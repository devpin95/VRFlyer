using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

// for animation curve in a job: 
//      https://forum.unity.com/threads/need-way-to-evaluate-animationcurve-in-the-job.532149/

public class AdvancedMeshManager : MonoBehaviour
{
    // LOD variables
    [Header("LOD variables")]
    private Transform playerTransform;
    private Vector3 previousPlayerPosition;
    public float recalculateLodDistance;
    public float distanceFromPlayer = 0;
    [Range(-1, 6)] public int LOD = 6;

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
    
    private class LODMeshEntry
    {
        public GameObject instance;
        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public MeshCollider meshCollider;
        public Mesh tempMesh;
    }
    
    // jobs
    [NonSerialized] public JobHandle mapJobHandle;
    [NonSerialized] public BuildAdvancedMapJob mapJob;
    
    // test vertex job
    [NonSerialized] public JobHandle vertexUVJobHandle;
    [NonSerialized] public BuildAdvancedVertexUVJob vertexUVJob;
    [NonSerialized] public Mesh.MeshDataArray meshDataArray;
    
    //test index job
    [NonSerialized] public JobHandle indexJobHandle;
    [NonSerialized] public BuildAdvancedIndexJob indexJob;
    
    void Awake()
    {
        _gameData = GameObject.Find("Game Manager").GetComponent<GameManager>().gameData;

        playerTransform = FindObjectOfType<WorldRepositionManager>().playerTransform;
        
        _meshBounds = new Bounds();

        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();
        _meshRenderer = GetComponent<Renderer>();
        
        meshLods = new List<LODMeshEntry>();
        for (int i = 0; i < LODcount; ++i)
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
        }
        
        // initialize jobs
        nativeMap = new NativeArray<float>(terrainInfo.meshVerts * terrainInfo.meshVerts, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        
        mapJob = new BuildAdvancedMapJob();
        mapJob.map = nativeMap;
        mapJob.dim = terrainInfo.meshVerts;
        mapJob.chunkSize = terrainInfo.chunkSize;
        
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

        vertexUVJob = new BuildAdvancedVertexUVJob();
        vertexUVJob.map = nativeMap;
        vertexUVJob.dim = terrainInfo.meshVerts;
        vertexUVJob.vertexScale = terrainInfo.vertexScale;
        vertexUVJob.LOD = 1;

        indexJob = new BuildAdvancedIndexJob();
        indexJob.meshVerts = (uint) terrainInfo.meshVerts;
        indexJob.meshSquares = (uint)terrainInfo.meshSquares;
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

        // only recalculate the LOD when the player has traveled a certain distance
        if (Vector3.Distance(previousPlayerPosition, playerTransform.position) > terrainInfo.recalculateLodDistance)
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
                LOD = targetLOD / 2;
                // TODO update the mesh with the new LOD
            }
        }
    }

    private void OnDestroy()
    {
        nativeMap.Dispose();
        mapJob.amplitudes.Dispose();
        mapJob.frequencies.Dispose();
        // meshDataArray.Dispose();
    }

    public void BuildTerrain(Vector2 pos)
    {
        // update the grid position with the new offset pos
        previousGridOffset = gridOffset;
        gridOffset = pos;
        
        // set the world offset to the gridoffset plus the perlin offset in _gameData
        worldOffset = new Vector2(gridOffset.x + _gameData.PerlinOffset.x, gridOffset.y + _gameData.PerlinOffset.y);
        
        // Debug.Log(transform.name + " Building a mesh muh boi");
        
        mapJob.offset = new int2((int)pos.x, (int)pos.y);
        mapJobHandle = mapJob.Schedule(nativeMap.Length, 1);
        
        meshDataArray = Mesh.AllocateWritableMeshData(1);
        
        // make temp vertex attribute descriptors
        int vertexAttributeCount = 2;
        VertexAttributeDescriptor vertexPositionStream = new VertexAttributeDescriptor(VertexAttribute.Position, dimension: 3); // stream 0
        VertexAttributeDescriptor vertexUvStream = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2, stream: 1); // stream 1
        
        var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
        vertexAttributes[0] = vertexPositionStream;
        vertexAttributes[1] = vertexUvStream;

        for (int i = 0; i < meshDataArray.Length; ++i)
        {
            meshDataArray[i].SetVertexBufferParams(nativeMap.Length, vertexAttributes);
        }

        vertexAttributes.Dispose(); // make sure to dispose the attributes array in the same frame
        
        // vertexUVJob.vertices = meshDataArray[0].GetVertexData<float3>();
        // vertexUVJob.uvs = meshDataArray[0].GetVertexData<float2>(stream: 1);
        vertexUVJob.meshData = meshDataArray[0];
        vertexUVJobHandle = vertexUVJob.Schedule(nativeMap.Length, 1, dependsOn: mapJobHandle);
        
        int triangleIndexCount = (terrainInfo.meshSquares * 2) * terrainInfo.meshSquares * 3; // the number of triangles in the first row * number of rows
        
        Debug.Log(transform.name + " will have a traingle array of " + triangleIndexCount);
        
        meshDataArray[0].SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);
        indexJob.indices = meshDataArray[0].GetIndexData<uint>();

        indexJobHandle = indexJob.Schedule(terrainInfo.meshSquares * terrainInfo.meshSquares, 1);

        StartCoroutine(PollJobs());
    }

    IEnumerator PollJobs()
    {
        // yield return new WaitUntil(() => !mapJobHandle.IsCompleted); // wait until the job is completed
        // mapJobHandle.Complete();
        

        yield return new WaitUntil(() => !vertexUVJobHandle.IsCompleted); // wait until the job is completed
        mapJobHandle.Complete();
        vertexUVJobHandle.Complete();
        Debug.Log(transform.name + " Finished map and mesh building job");
        
        indexJobHandle.Complete();
        
        Mesh.ApplyAndDisposeWritableMeshData(meshDataArray, meshLods[0].tempMesh);

        meshLods[0].meshFilter.sharedMesh = meshLods[0].tempMesh;
        meshLods[0].meshCollider.sharedMesh = meshLods[0].tempMesh;

        // meshDataArray.Dispose(); // make sure to get rid of the meshDataArray that we allocated before the job started


        // for (int i = 0; i < LODcount; ++i)
        // {
        //     meshLods[i].tempMesh.Clear();
        // }
        //
        // Mesh.ApplyAndDisposeWritableMeshData(mapJob.meshDataArray, meshLods[0].tempMesh);
        //
        // for (int i = 0; i < LODcount; ++i)
        // {
        //     meshLods[i].tempMesh.RecalculateNormals();
        //     meshLods[i].tempMesh.RecalculateBounds();
        // }

        // TODO test if we need to apply the new mesh to the shared meshes of the filter, renderer, and collider
    }

    private void LateUpdate()
    {
        return;
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
        //TODO calculate bounds based on the min and max of the heightmap
    }
}
