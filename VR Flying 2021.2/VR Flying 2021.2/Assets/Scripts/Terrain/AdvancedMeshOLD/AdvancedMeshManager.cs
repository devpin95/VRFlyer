using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

// for animation curve in a job: 
//      https://forum.unity.com/threads/need-way-to-evaluate-animationcurve-in-the-job.532149/

public class AdvancedMeshManager : MonoBehaviour
{
    // LOD variables
    [Header("LOD variables")]
    private Transform playerTransform;
    private Vector3 previousPlayerPosition;
    public float distanceFromPlayer = 0;
    [Range(-1, 6)] public int LOD = 0;

    [Header("Grid Position")] 
    public Vector2 gridOffset;
    public Vector2 previousGridOffset;
    public Vector2 worldOffset;
    private Vector3 _repositionOffset = Vector3.zero;
    
    // Objects
    [Header("Object References")]
    private GameData _gameData;
    public TerrainInfo terrainInfo;
    
    // Material
    public Material material;
    
    // components
    private MeshFilter _meshFilter;
    private Renderer _meshRenderer;
    private MeshCollider _meshCollider;
    
    // mesh
    private Bounds _meshBounds;

    // lods
    private int LODcount = 1;
    private NativeArray<float> nativeMap;
    private List<LODMeshEntry> meshLods;
    private List<Mesh> lodMeshList;
    
    private class LODMeshEntry
    {
        public GameObject instance;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public MeshCollider meshCollider;
        public Mesh tempMesh;
    }
    
    // jobs
    private bool _jobsRunning = false;
    [NonSerialized] public JobHandle mapJobHandle;
    [NonSerialized] public BuildAdvancedMapJob mapJob;
    
    // test vertex job
    // [NonSerialized] public JobHandle vertexUVJobHandle;
    // [NonSerialized] public BuildAdvancedVertexUVJob vertexUVJob;
    [NonSerialized] public Mesh.MeshDataArray meshDataArray;
    private NativeArray<VertexAttributeDescriptor> meshVertexAttributes;
    
    //test index job
    // [NonSerialized] public JobHandle indexJobHandle;
    // [NonSerialized] public BuildAdvancedIndexJob indexJob;
    
    private class JobData
    {
        // vertex job
        public BuildAdvancedVertexJob vertexJob;
        public JobHandle vertexJobHandle;
        public uint vertexJobLength;
        
        // UV Job
        public BuildAdvancedUVJob uvJob;
        public JobHandle uvJobHandle;
        public uint uvJobLength;

        // index job
        public BuildAdvancedIndexJob indexJob;
        public JobHandle indexJobHandle;
        public uint indexJobLength;
        

        public void SetJobMap(NativeArray<float> map)
        {
            vertexJob.map = map;
        }
    }
    
    private List<JobData> jobCollection;

    // synchronization
    private bool _isBuilding = false;
    private DeferredBuildData deferredTerrainBuildData;
    private bool _buildDeferred = false;
    private bool _firstBuild = true;

    private struct DeferredBuildData
    {
        public Vector2 pos;
        public int lod;
    }

    void Awake()
    {
        _gameData = GameObject.Find("Game Manager").GetComponent<GameManager>().gameData;
        playerTransform = FindObjectOfType<WorldRepositionManager>().playerTransform;
        
        _meshBounds = new Bounds();

        // set up the objects that will take on the generated mesh
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer = GetComponent<Renderer>();
        
        meshLods = new List<LODMeshEntry>();
        lodMeshList = new List<Mesh>();
        jobCollection = new List<JobData>();
        
        for (int i = 0; i <= terrainInfo.lods.Count; ++i)
        {
            InitLodMeshObject(i);
            InitLodMeshJob(i);
        }
        
        // initialize jobs
        nativeMap = new NativeArray<float>(terrainInfo.meshVerts * terrainInfo.meshVerts, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        
        // set up the map job that will be used by all of the lod jobs
        InitMapJob();
        
        // set up the vertex attribute descriptors for allocating writeable mesh data
        meshVertexAttributes = new NativeArray<VertexAttributeDescriptor>();
        SetMeshVertexAttributes();

        deferredTerrainBuildData = new DeferredBuildData();
    }

    private void Start()
    {
        previousPlayerPosition = playerTransform.position;
        
        // nativeMap = new NativeArray<float>(terrainInfo.meshVerts * terrainInfo.meshVerts, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        // meshJob.map = nativeMap;
    }

    private void Update()
    {
        distanceFromPlayer = Mathf.Sqrt(_meshBounds.SqrDistance(playerTransform.position));

        // if (distanceFromPlayer > terrainInfo.cullDistance) _meshRenderer.enabled = false;
        // else _meshRenderer.enabled = true;

        // if (!_isBuilding && _buildDeferred)
        // {
        //     _buildDeferred = false;
        //     BuildTerrain(deferredTerrainBuildData.pos, deferredTerrainBuildData.lod, true);
        //     return;
        // }

        // only recalculate the LOD when the player has traveled a certain distance
        if (Vector3.Distance(previousPlayerPosition, playerTransform.position) > terrainInfo.recalculateLodDistance && !_firstBuild)
        {
            // loop through all of the LODs and determine which LOD we need to target
            // loop backwards and if the player distance is greater than any LOD distance, we know the mesh needs
            // to be at the LOD
            bool lodChange = false;
            int targetLOD = terrainInfo.lods[terrainInfo.lods.Count - 1].lod;
            for (int i = terrainInfo.lods.Count - 1; i >= -1; --i)
            {
                // if we loop through all of the LODs and havent found one, then we are close enough for the highest LOD
                if (i == -1)
                {
                    targetLOD = 0;
                    lodChange = true;
                    break;
                }
                
                // if we after further away that the LOD distance, we must need to use this LOD
                if (distanceFromPlayer > terrainInfo.lods[i].distance)
                {
                    targetLOD = terrainInfo.lods[i].lod;
                    lodChange = true;
                    break;
                }
            }
            
            // only swap our LOD if we actually need to swap to a different LOD
            if (targetLOD / 2 != LOD && lodChange)
            {
                if ( targetLOD < 0 ) Debug.Log("Hiding terrain chunk");
                
                Debug.Log(transform.name + " changing LOD from " + LOD + " to " + (targetLOD / 2) );
                LOD = targetLOD / 2;
                // TODO update the mesh with the new LOD
                BuildTerrain(gridOffset, targetLOD: (LOD == 0 ? 1 : LOD * 2), defer: true);
            }
        }
    }

    private void OnDestroy()
    {
        // mapJobHandle.Complete();
        // for (int i = 0; i < jobCollection.Count; ++i)
        // {
        //     jobCollection[i].indexJobHandle.Complete();
        //     jobCollection[i].uvJobHandle.Complete();
        //     jobCollection[i].vertexJobHandle.Complete();
        // }

        nativeMap.Dispose();
        mapJob.amplitudes.Dispose();
        mapJob.frequencies.Dispose();
        meshVertexAttributes.Dispose();
        // meshDataArray.Dispose();
    }

    public void BuildTerrain(Vector2 pos, int targetLOD = 1, bool defer = false)
    {
        if (_isBuilding)
        {
            if (defer)
            {
                Debug.Log("Deferring Terrain Build");
                _buildDeferred = true;
                deferredTerrainBuildData.pos = pos;
                deferredTerrainBuildData.lod = targetLOD;
            }
            
            return;
        }

        _isBuilding = true;
        
        // update the grid position with the new offset pos
        previousGridOffset = gridOffset;
        gridOffset = pos;
        
        // set the world offset to the gridoffset plus the perlin offset in _gameData
        worldOffset = new Vector2(gridOffset.x + _gameData.PerlinOffset.x, gridOffset.y + _gameData.PerlinOffset.y);
        
        // Debug.Log(transform.name + " Building a mesh muh boi");
        
        int newMapSquares = terrainInfo.meshSquares / targetLOD;
        int newMapVerts = newMapSquares + 1;
        int newMapSize = newMapVerts * newMapVerts;

        nativeMap.Dispose();
        nativeMap = new NativeArray<float>(newMapSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        
        mapJob.offset = new int2((int)pos.x, (int)pos.y);
        mapJob.map = nativeMap;
        mapJobHandle = mapJob.Schedule(nativeMap.Length, 1);

        SetupMeshDataArray();

        for (int i = 0; i < jobCollection.Count; ++i)
        {
            var job = jobCollection[i];
            job.vertexJob.map = nativeMap;
            
            job.vertexJobHandle = job.vertexJob.Schedule((int)job.vertexJobLength, 1, mapJobHandle);
            Debug.Log("Scheduled vertex Job");
            
            job.uvJobHandle = job.uvJob.Schedule((int)job.uvJobLength, 1, job.vertexJobHandle);
            Debug.Log("Scheduled uv Job");
            
            job.indexJobHandle = job.indexJob.Schedule((int)job.indexJobLength, 1,  job.uvJobHandle);
            Debug.Log("Scheduled index Job");

            jobCollection[i] = job;
        }
        
        Debug.Log("Starting poll");

        // start polling for the jobs' completion
        // StartCoroutine(PollJobs());
        _jobsRunning = true;
        
        // call this to make sure all of the jobs actually start executing
        JobHandle.ScheduleBatchedJobs();
        
        // set the position of the terrain chunk in the world
        float fullwidth = terrainInfo.meshSquares * terrainInfo.vertexScale; // the full width to move the block to it's correct position
        transform.position = new Vector3(gridOffset.x * fullwidth, 0, gridOffset.y * fullwidth) + _repositionOffset;
    }

    IEnumerator PollJobs()
    {
        // yield return new WaitUntil(() => !mapJobHandle.IsCompleted); // wait until the job is completed
        // mapJobHandle.Complete();
        
        for (int i = 0; i < jobCollection.Count; ++i)
        {
            yield return new WaitUntil(() => !jobCollection[i].indexJobHandle.IsCompleted); // wait until the job is completed
            // jobCollection[i].indexJobHandle.Complete();
            // jobCollection[i].uvJobHandle.Complete();
            // jobCollection[i].vertexJobHandle.Complete();
            
            Debug.Log(transform.name + " Finished map and mesh building job for LOD " + i);
        }

        yield return ApplyMeshData();
    }

    IEnumerator ApplyMeshData()
    {
        for (int i = 0; i < meshDataArray.Length; ++i)
        {
            int triangleIndexCount = terrainInfo.meshSquares * terrainInfo.meshSquares * 6;
            Mesh.MeshData tempData = meshDataArray[0];

            // we have to set the sub mesh after all the work has been done because unity will look at the values in
            // the index array and validate that values. If we do it before the indices have been set, we can get an error
            // saying the random values in the allocated index array are out of bounds (there will be numbers higher than
            // the number of vertices in the mesh)
            tempData.subMeshCount = 1;
            tempData.SetSubMesh(0, new SubMeshDescriptor(0, triangleIndexCount));

            yield return new WaitForSeconds(0.25f);
            
            _isBuilding = false;
            _firstBuild = false;
        }

        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, lodMeshList);
        // Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, meshLods[0].tempMesh);

        for (int i = 0; i < meshLods.Count; ++i)
        {
            meshLods[i].tempMesh.RecalculateNormals();
            yield return new WaitForSeconds(Random.Range(0, 0.3f));
            meshLods[i].tempMesh.RecalculateBounds();   
        }
    }

    private void LateUpdate()
    {
        if (_jobsRunning)
        {
            mapJobHandle.Complete();
        
            for (int i = 0; i < jobCollection.Count; ++i)
            {
                if (jobCollection[i].indexJobHandle.IsCompleted)
                {
                    jobCollection[i].indexJobHandle.Complete();
                    jobCollection[i].uvJobHandle.Complete();
                    jobCollection[i].vertexJobHandle.Complete();
                }

                Debug.Log(transform.name + " Finished map and mesh building job for LOD " + i);
            }
            
            _jobsRunning = false;
        }
    }

    public void Reposition(Vector3 offset)
    {
        _repositionOffset += offset;
        transform.position += offset;

        CalculateBounds();
    }

    public void SetRepositionOffset(Vector3 offset)
    {
        _repositionOffset = offset;
    }

    public void CalculateBounds()
    {
        float width = terrainInfo.meshVerts * terrainInfo.vertexScale;
        float miny = terrainInfo.remapMin;
        float maxy = terrainInfo.remapMax;
        float ydelta = maxy - miny;
        float verticalCenter = miny + (maxy - miny) / 2;
        Vector3 center = new Vector3(transform.position.x + width / 2, verticalCenter / 2, transform.position.z + width / 2);
        Vector3 size = new Vector3(width, ydelta, width);
        _meshBounds.center = center;
        _meshBounds.size = size;
    }

    private void SetupMeshDataArray()
    {
        meshDataArray = Mesh.AllocateWritableMeshData(terrainInfo.lods.Count);

        // loop through the lods
        for (int i = 0; i < terrainInfo.lods.Count; ++i )
        {
            Mesh.MeshData tempData = meshDataArray[i];

            // initialize the array of mesh data with the vertex attributes
            tempData.SetVertexBufferParams(nativeMap.Length, meshVertexAttributes);

            // the number of squares times 6 for the number of points for each triangle in the square
            int triangleIndexCount = terrainInfo.meshSquares * terrainInfo.meshSquares * 6;

            // set up the array for indices as the number of triangle index count
            // then get a reference to the native array for the indices
            tempData.SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);
            
            jobCollection[i].vertexJob.verts = tempData.GetVertexData<float3>();
            jobCollection[i].uvJob.uvs = tempData.GetVertexData<float2>(1);
            jobCollection[i].indexJob.indices = tempData.GetIndexData<uint>();
        }
    }

    private void InitMapJob()
    {
        mapJob = new BuildAdvancedMapJob();
        mapJob.map = nativeMap;
        mapJob.dim = terrainInfo.meshVerts;
        mapJob.chunkSize = terrainInfo.chunkSize;
        mapJob.lod = 1;
        
        mapJob.amplitudes = new NativeArray<float>(5, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        mapJob.amplitudes[0] = 1f;
        mapJob.amplitudes[1] = 1f / 2f;
        mapJob.amplitudes[2] = 1f / 4f;
        mapJob.amplitudes[3] = 1f / 8f;
        mapJob.amplitudes[4] = 1f / 16f;

        mapJob.frequencies = new NativeArray<float>(5, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        mapJob.frequencies[0] = 1f;
        mapJob.frequencies[1] = 2f;
        mapJob.frequencies[2] = 4f;
        mapJob.frequencies[3] = 8f;
        mapJob.frequencies[4] = 16f;

        mapJob.octaveMax = 1.9375f;
    }
    
    private void InitLodMeshObject(int i)
    {
        // make a new entry with a new gameobject that has mesh filter and renderer components
        LODMeshEntry entry = new LODMeshEntry();
            
        entry.instance = new GameObject();
        // entry.instance.SetActive(false);
        entry.instance.transform.name = "LOD " + (i == 0 ? 1 : i * 2);
        entry.instance.transform.SetParent(transform); // make sure that the new object is a child of this object
            
        entry.meshFilter = entry.instance.AddComponent<MeshFilter>();
        entry.meshCollider = entry.instance.AddComponent<MeshCollider>();
            
        entry.meshRenderer = entry.instance.AddComponent<MeshRenderer>();
        entry.meshRenderer.material = material;

        entry.tempMesh = new Mesh();
        entry.tempMesh.MarkDynamic();

        entry.meshFilter.sharedMesh = entry.tempMesh;
        entry.meshCollider.sharedMesh = entry.tempMesh;

        // add it to the list so we can reference it later
        meshLods.Add(entry);
        lodMeshList.Add(entry.tempMesh);
    }

    private void InitLodMeshJob(int i)
    {
        int targetLod = i == 0 ? 1 : i * 2;
        
        // set up vertex job
        BuildAdvancedVertexJob vertexJob = new BuildAdvancedVertexJob();
        vertexJob.map = nativeMap;
        vertexJob.dim = terrainInfo.meshVerts;
        vertexJob.vertexScale = terrainInfo.vertexScale;
        vertexJob.LOD = targetLod;
        vertexJob.remap = new float2(terrainInfo.remapMin, terrainInfo.remapMax);

        // set up UV job
        BuildAdvancedUVJob uvJob = new BuildAdvancedUVJob();
        uvJob.dim = terrainInfo.meshVerts;
        uvJob.vertexScale = terrainInfo.vertexScale;
        uvJob.LOD = targetLod;

        // set up index job
        BuildAdvancedIndexJob indexJob = new BuildAdvancedIndexJob();
        // make sure the meshSquares and meshVerts match the size of the target LOD
        indexJob.meshSquares = (uint) terrainInfo.meshSquares / (uint) vertexJob.LOD;
        indexJob.meshVerts = indexJob.meshSquares + 1;
        
        // create a new job pipeline for the given LOD i
        JobData meshPipeline = new JobData();
        
        // add vertex job to pipeline
        meshPipeline.vertexJob = vertexJob;
        meshPipeline.vertexJobLength = indexJob.meshVerts * indexJob.meshVerts;

        // add UV job to pipeline
        meshPipeline.uvJob = uvJob;
        meshPipeline.uvJobLength = indexJob.meshVerts * indexJob.meshVerts;
        
        // add index job to pipeline
        meshPipeline.indexJob = indexJob;
        meshPipeline.indexJobLength = indexJob.meshSquares * indexJob.meshSquares;

        // add the mesh pipeline to the job list
        jobCollection.Add(meshPipeline);
    }
    
    private void SetMeshVertexAttributes()
    {
        // make temp vertex attribute descriptors for vertex position and UV streams
        int vertexAttributeCount = 2;
        VertexAttributeDescriptor vertexPositionStream = new VertexAttributeDescriptor(VertexAttribute.Position, dimension: 3); // stream 0
        VertexAttributeDescriptor vertexUvStream = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2, stream: 1); // stream 1
        
        // set up the mesh vertex attributes position stream and UV stream
        meshVertexAttributes = new NativeArray<VertexAttributeDescriptor>(vertexAttributeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        meshVertexAttributes[0] = vertexPositionStream;
        meshVertexAttributes[1] = vertexUvStream;
    }
}
