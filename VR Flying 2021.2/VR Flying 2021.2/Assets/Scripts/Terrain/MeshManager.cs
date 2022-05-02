using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MeshManager : MonoBehaviour
{
    public TerrainInfo terrainInfo;
    
    private Mesh _mesh;
    private Vector3[] _vertices;
    private int[] _triangles;
    private Vector3[] _normals;
    private Vector2[] _uvs;

    [Header("Mesh")]
    // public int meshSquares = 239;
    // public int meshVerts = 240;
    public Vector2 previousGridOffset;
    public Vector2 gridOffset;
    public Vector2 worldOffset;
    // [FormerlySerializedAs("offsetScale")] public float chunkSize = 5;
    // public float vertexScale = 1;
    // public AnimationCurve terrainCurve;
    [Range(-1, 6)]
    public int LOD;

    [Header("Terrain")] 
    private GameObject _structureContainer;
    // public int noiseOctaves = 5;
    // public float remapMin = -500;
    // public float remapMax = 500;

    [Header("LOD Distances")] 
    private Transform playerTransform;
    private Vector3 previousPlayerPosition;
    public float recalculateLodDistance;
    public float distanceFromPlayer = 0;
    // public List<LODInfo> lods = new List<LODInfo>();
    private WorldRepositionManager _worldRepositionManager;

    [Header("Maps")]
    [Range(0, 1)] public float altitudeVal;

    private MeshFilter _meshFilter;
    private Renderer _meshRenderer;
    private MeshCollider _meshCollider;

    private TerrainMap _terrainMap = new TerrainMap();
    public Texture2D heightMapTex;
    public Texture2D altitudeMapTex;
    [HideInInspector] public Texture2D normalMapTex;

    private Material _terrainMaterial;
    private GameData _gameData;
    
    private Vector3 _repositionOffset = Vector3.zero;
    private Bounds _meshBounds;

    private Queue<TerrainMap.MapGenerationOutput> _mapThreadResultQueue;
    private Queue<MeshGenerationOutput> _meshThreadResultQueue;

    private bool startup = true;

    private bool[] _initializedMeshes;
    private LODMesh[] _meshLODs;
    private Mesh.MeshDataArray _meshDataArray;
    
    [Header("Events")]
    public CEvent initialGenerationEvent;
    public CEvent_Vector3 helipadSpawnPointNotification;

    [Serializable]
    public struct LODInfo
    {
        public int lod;
        public float distance;

        public LODInfo(int lod, float distance)
        {
            this.lod = lod;
            this.distance = distance;
        }
    }

    private struct LODMesh
    {
        public GameObject instance;
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;
        public Mesh mesh;
    }
    
    [Header("DEBUG")] 
    public Image img;

    public bool atCullDistance;
    public bool beneathCullHeight;
    public bool culled = false;

    // Start is called before the first frame update
    void Awake()
    {
        _terrainMaterial = GetComponent<MeshRenderer>().material;
        _gameData = GameObject.Find("Game Manager").GetComponent<GameManager>().gameData;

        _mapThreadResultQueue = new Queue<TerrainMap.MapGenerationOutput>();
        _meshThreadResultQueue = new Queue<MeshGenerationOutput>();

        playerTransform = FindObjectOfType<WorldRepositionManager>().playerTransform;

        _terrainMap = new TerrainMap();
        _terrainMap.InitMap(terrainInfo.meshVerts);
        _meshBounds = new Bounds();

        // _meshFilter = GetComponent<MeshFilter>();
        // _meshCollider = GetComponent<MeshCollider>();
        // _meshRenderer = GetComponent<Renderer>();
        //
        // _mesh = new Mesh();
        // _mesh.MarkDynamic();

        _worldRepositionManager = FindObjectOfType<WorldRepositionManager>().GetComponent<WorldRepositionManager>();

        LOD = LODUtility.LowestLOD(terrainInfo).lod;
        _initializedMeshes = new bool[LODUtility.MaxLODCount()]; // initialize them all to false
        _meshLODs = new LODMesh[LODUtility.MaxLODCount()];
        _meshDataArray = Mesh.AllocateWritableMeshData(LODUtility.MaxLODCount());
        
        // create an empty gameobject to hold all of the structures for a given terrain
        _structureContainer = new GameObject();
        _structureContainer.transform.position = Vector3.zero;
        _structureContainer.transform.SetParent(transform, worldPositionStays: false);
        _structureContainer.transform.name = "Structures";
    }

    private void Start()
    {
        previousPlayerPosition = playerTransform.position;
    }

    private void OnDestroy()
    {
        _meshDataArray.Dispose();
    }

    private void Update()
    {
        // check if there is a map generation result in the queue
        if (_mapThreadResultQueue.Count > 0)
        {
            // if there is, dequeue the data and invoke it's callback with the data
            TerrainMap.MapGenerationOutput output = _mapThreadResultQueue.Dequeue();
            output.callback(output, false);
        }
        if (_meshThreadResultQueue.Count > 0)
        {
            MeshGenerationOutput output = _meshThreadResultQueue.Dequeue();
            output.callback(output, true);
        }

        distanceFromPlayer = Mathf.Sqrt(_meshBounds.SqrDistance(playerTransform.position));

        // if (distanceFromPlayer > terrainInfo.cullDistance) _meshRenderer.enabled = false;
        // else _meshRenderer.enabled = true;
        
        // cull chunks that are too far away from the player or if the player is beneath the cull height
        atCullDistance = distanceFromPlayer > terrainInfo.cullDistance; // too far away
        beneathCullHeight = _worldRepositionManager.playerWorldPos.y < terrainInfo.heightCullDistance; // too low
        beneathCullHeight = beneathCullHeight && LOD != 0;
        if (atCullDistance || beneathCullHeight) _meshLODs[LOD].instance?.SetActive(false);
        else _meshLODs[LOD].instance?.SetActive(true);

        // only recalculate the LOD when the player has traveled a certain distance
        if (Vector3.Distance(previousPlayerPosition, playerTransform.position) > terrainInfo.recalculateLodDistance)
        {
            FindNewLOD();
        }
    }

    private void FindNewLOD()
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
            if (targetLOD != LOD && lodChange)
            {
                int oldLOD = LOD;
                LOD = targetLOD;

                if (_initializedMeshes[LOD])
                {
                    // the mesh at the target LOD has already been generated, so we dont need to regenerate it
                    SwapLODMesh(oldLOD, LOD);
                }
                else
                {
                    //disable the old mesh
                    if ( _meshLODs[oldLOD].instance ) _meshLODs[oldLOD].instance?.SetActive(false);

                    // set up a stub of the information we would have gotten from the map generation thread
                    TerrainMap.MapGenerationOutput meshGenerationInfoStub = new TerrainMap.MapGenerationOutput();
                    meshGenerationInfoStub.map = _terrainMap.Get2DHeightMap();
                    meshGenerationInfoStub.min = _terrainMap.MapMin();
                    meshGenerationInfoStub.max = _terrainMap.MapMax();

                    // call the function to start the mesh generation thread
                    MapGenerationReceived(meshGenerationInfoStub, true);   
                }
            }
    }

    public void ForceLODCheck()
    {
        FindNewLOD();
    }

    private void CreateMeshLODGameObject(int targetLOD)
    {
        _meshLODs[targetLOD].instance = new GameObject();
        _meshLODs[targetLOD].instance.transform.position = Vector3.zero;
        _meshLODs[targetLOD].instance.transform.SetParent(transform, worldPositionStays: false);
        _meshLODs[targetLOD].instance.name = "LOD " + targetLOD;

        _meshLODs[targetLOD].meshFilter = _meshLODs[targetLOD].instance.AddComponent<MeshFilter>();
        _meshLODs[targetLOD].meshRenderer = _meshLODs[targetLOD].instance.AddComponent<MeshRenderer>();
        _meshLODs[targetLOD].meshCollider = _meshLODs[targetLOD].instance.AddComponent<MeshCollider>();

        _meshLODs[targetLOD].meshRenderer.material = _terrainMaterial;

        _meshLODs[targetLOD].mesh = new Mesh();
        _meshLODs[targetLOD].mesh.MarkDynamic();

        // if the target LOD is NOT the highest LOD, then disable it's shadows for better performance
        if (targetLOD != 0)
        {
            _meshLODs[targetLOD].meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }
    
    public void BuildTerrain()
    {
        _terrainMap.InitMap(terrainInfo.meshVerts);
        
        Vector2 nworldOffset = new Vector2(gridOffset.x, gridOffset.y);

        if (_gameData)
            nworldOffset = new Vector2(gridOffset.x + _gameData.PerlinOffset.x, gridOffset.y + _gameData.PerlinOffset.y);
        
        worldOffset = nworldOffset;

        _terrainMap.RequestMap(nworldOffset, terrainInfo.chunkSize, callback: MapGenerationReceived, _mapThreadResultQueue, octaves: terrainInfo.noiseOctaves);
    }

    private void MapGenerationReceived(TerrainMap.MapGenerationOutput output, bool stub = false)
    {
        StartCoroutine(MapGenerationCoroutine(output, stub));
    }

    IEnumerator MapGenerationCoroutine(TerrainMap.MapGenerationOutput output, bool stub = false)
    {
        if (!stub)
        {
            _terrainMap.SetMap(output.map, output.min, output.max, output.minpos, output.maxpos);
            yield return null;
            heightMapTex = _terrainMap.GetHeightMapTexture2D();
            yield return null;
            altitudeMapTex = _terrainMap.GetAltitudeMap(altitudeVal, TerrainMap.ALTITUDE_BELOW);
            yield return null;
            normalMapTex = _terrainMap.GetNormalMapTex2D(terrainInfo.remapMin, terrainInfo.remapMax);
        }

        yield return null;
        
        // clear the mesh arrays before we start making new ones
        // if (_uvs != null) Array.Clear(_uvs, 0, _vertices.Length); // do this one first because we dont want to clear _vertices before we get it's length here
        // yield return null;
        // if (_vertices != null) Array.Clear(_vertices, 0, _vertices.Length);
        // yield return null;
        // if (_triangles != null) Array.Clear(_triangles, 0, _triangles.Length);
        // yield return null;

        // set up the mesh generation input
        // int lodMeshVerts = meshVerts / (LOD == 0 ? 1 : LOD * 2);
        // int lodMeshSquares = lodMeshVerts - 1;
        int meshDim = (terrainInfo.meshVerts - 1) / LODUtility.LODToMeshResolution(LOD) + 1;
        MeshGenerationInput meshGenerationInput = new MeshGenerationInput(
            _terrainMap, 
            meshDim, 
            terrainInfo.vertexScale, 
            terrainInfo.remapMin, 
            terrainInfo.remapMax, 
            terrainInfo.terrainCurve, 
            LODUtility.LODToMeshResolution(LOD),
            ref _meshDataArray);

        yield return null;

        // ** IJob **
        // -------------------------------------------------------------------------------------------------------------
        // MeshBuilderIJob builderIJob = new MeshBuilderIJob();
        // builderIJob.meshInfo = meshGenerationInput;
        // builderIJob.callback = MeshGenerationReceived;
        // builderIJob.resultQ = _meshThreadResultQueue;
        //
        // JobHandle jobHandle = builderIJob.Schedule();
        // jobHandle.Complete();
        
        // ** thread pool **
        // -------------------------------------------------------------------------------------------------------------
        // // make the new builder thread object with the input, the output queue, and the callback we need to invoke
        MeshBuilderThread builderThread = new MeshBuilderThread(ref meshGenerationInput, _meshThreadResultQueue, MeshGenerationReceived);
        
        yield return null;
        
        ThreadPool.QueueUserWorkItem(delegate { builderThread.ThreadProc(); });

        // // make a ThreadStart that will call the MeshBuilderThread.ThreadProc and to run the mesh generation on a thread
        // ThreadStart threadStart = delegate { builderThread.ThreadProc(); };
        //
        // Thread meshThread = new Thread(threadStart);
        // meshThread.Start();
        // meshThread.Join();
        
        yield return null;
    }
    
    private void MeshGenerationReceived(MeshGenerationOutput generationOutput, bool reposition = true)
    {
        StartCoroutine(MeshGenerationCoroutine(generationOutput));
    }

    IEnumerator MeshGenerationCoroutine(MeshGenerationOutput generationOutput)
    {
        // _mesh.Clear();
        // ----- >
        int targetLod = generationOutput.targetLod; // make a copy of this so everything is easier to read later
        
        // if the mesh hasnt been created, create it
        // if the mesh is already created, then clear it so we can update it with the new heightmap
        if (_meshLODs[targetLod].instance == null) CreateMeshLODGameObject(targetLod);
        else _meshLODs[targetLod].mesh.Clear();

        yield return null;
        
        // Mesh.ApplyAndDisposeWritableMeshData(generationOutput.meshDataArray, _mesh);
        // ----- >
        // Mesh.ApplyAndDisposeWritableMeshData(generationOutput.meshDataArray, _meshLODs[generationOutput.targetLod].mesh);
        NativeArray<float3> targetVerts = generationOutput.meshDataArray[targetLod].GetVertexData<float3>((int)MeshBuilderThread.MeshDataStreams.Vertices);
        NativeArray<float2> targetUVs = generationOutput.meshDataArray[targetLod].GetVertexData<float2>((int)MeshBuilderThread.MeshDataStreams.UVs);
        NativeArray<uint> targetIndices = generationOutput.meshDataArray[targetLod].GetIndexData<uint>();
        
        _meshLODs[targetLod].mesh.SetVertices(targetVerts, 0, targetVerts.Length, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
        yield return null;
        _meshLODs[targetLod].mesh.SetUVs(0, targetUVs);
        yield return null;
        _meshLODs[targetLod].mesh.SetIndices(targetIndices, MeshTopology.Triangles, 0, calculateBounds: false);
        yield return null;
        
        // recalculate the normals so that everything looks right
        //_mesh.RecalculateNormals();
        // ----- >
        _meshLODs[targetLod].mesh.RecalculateNormals();
        
        yield return null;
        
        // _mesh.RecalculateBounds();
        // ----- >
        _meshLODs[targetLod].mesh.RecalculateBounds();
        
        yield return null;
        
        // _mesh.MarkModified();
        // ----- >
        _meshLODs[targetLod].mesh.MarkModified();
        
        yield return null;
        
        // updated the mesh on the filter and collider
        // _meshFilter.sharedMesh = _mesh;
        // ----- >
        // _meshFilter.sharedMesh = _meshLODs[generationOutput.targetLod].mesh;
        _meshLODs[targetLod].meshFilter.sharedMesh = _meshLODs[targetLod].mesh;
            
        yield return null;
        
        // _meshCollider.sharedMesh = _mesh;
        // ----- >
        // _meshCollider.sharedMesh = _meshLODs[generationOutput.targetLod].mesh;
        _meshLODs[targetLod].meshCollider.sharedMesh = _meshLODs[targetLod].mesh;
        
        yield return null;
        
        // position the mesh where it needs to go in grid and world space
        float fullwidth = terrainInfo.meshSquares * terrainInfo.vertexScale; // the full width to move the block to it's correct position

        if (gridOffset != previousGridOffset)
        {
            transform.position = new Vector3(gridOffset.x * fullwidth, 0, gridOffset.y * fullwidth) + _repositionOffset;
        }

        yield return null;
        
        CalculateBounds();

        // set the flag for this lod to true to indicate that this LOD has been generated
        _initializedMeshes[generationOutput.targetLod] = true;

        BuildStructures();

        if (startup)
        {
            initialGenerationEvent.Raise();
            startup = false;
        }
    }

    private void SwapLODMesh(int oldLod, int targetLod)
    {
        _meshLODs[oldLod].instance?.SetActive(false);
        _meshLODs[targetLod].instance.SetActive(true);
        // _meshFilter.sharedMesh = _meshLODs[targetLod].mesh;
        // _meshCollider.sharedMesh = _meshLODs[targetLod].mesh;
    }

    private void CalculateBounds()
    {
        float width = terrainInfo.meshVerts * terrainInfo.vertexScale;
        float miny = _terrainMap.MapMinRemapped(terrainInfo.remapMin, terrainInfo.remapMax, terrainInfo.terrainCurve);
        float maxy = _terrainMap.MapMaxRemapped(terrainInfo.remapMin, terrainInfo.remapMax, terrainInfo.terrainCurve);
        float ydelta = maxy - miny;
        float verticalCenter = miny + (maxy - miny) / 2;
        Vector3 center = new Vector3(transform.position.x + width / 2, verticalCenter / 2, transform.position.z + width / 2);
        Vector3 size = new Vector3(width, ydelta, width);
        _meshBounds.center = center;
        _meshBounds.size = size;
    }
    
    private void BuildStructures()
    {
        GridOriginStructures();
        
        // float fullwidth = terrainInfo.meshSquares * terrainInfo.vertexScale;
        //
        // int numStructures = Random.Range(0, Random.Range(0, 10));
        //
        // Debug.Log(transform.name + " generating " + numStructures + " structures");
        //
        // for (int i = 0; i < numStructures; ++i)
        // {
        //     GameObject prefab = _gameData.structureList[Random.Range(0, _gameData.structureList.Count)];
        //     GameObject structure = Instantiate(prefab, transform);
        //     structure.transform.SetParent(transform); // make sure that the terrain is the structures parent
        //     structure.transform.localPosition = Vector3.zero;
        //
        //     // set a random position for the structure
        //     float randx = Random.Range(0, fullwidth);
        //     float randz = Random.Range(0, fullwidth);
        //     Vector3 structurePos = new Vector3(randx, 5000, randz);
        //     structure.transform.localPosition = structurePos;
        //     
        //     structure.GetComponent<Structure>().PlantStructure();
        // }
    }

    private void GridOriginStructures()
    {
        // this function controls the structures that are required at the origin of the grid
        if (gridOffset != Vector2.zero) return;
        
        // plant a guaranteed helipad at the lowest point of the 0x0 grid chunk
        Vector3 lowestPoint = _terrainMap.MapMinPosition();
        Vector3 chunkPosition = TerrainMap.ScaleMapPosition(lowestPoint, terrainInfo.vertexScale, terrainInfo.terrainCurve, terrainInfo.remapMin, terrainInfo.remapMax);

        // get an instance of a helipad
        GameObject helipadInstance = Instantiate(_gameData.helipadPrefab, chunkPosition, Quaternion.identity);
        helipadInstance.transform.SetParent(_structureContainer.transform, false);
        
        // reposition the helipad so that the point it touches the ground matches the legs of the helipad instead
        // of the pivot of the helipad object
        Vector3 helipadAttachPoint = helipadInstance.transform.Find("Attach Point").position;
        Vector3 helipadAttachPointToChunkPoint = chunkPosition - helipadAttachPoint;
        helipadInstance.transform.position += helipadAttachPointToChunkPoint;

        // get the point of the helipad where we want to attach the helicopter to so that it is "landed" on the pad
        Vector3 landingPoint = helipadInstance.transform.Find("Landing Point").transform.position;

        // if we're at startup, we need to announce where this helipad will be placed so that the helicopter can
        // be spawned at it instead of in the air
        if (startup)
        {
            helipadSpawnPointNotification.Raise(landingPoint);
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

    private void ManualReposition(Vector3 offset)
    {
        transform.position += offset;
    }

    public void SetLod(int lod)
    {
        if (lod < 0 || lod > 12)
        {
            Debug.LogError("Mesh LOD must be an integer 0 <= lod <= 6");
            return;
        }

        LOD = lod;
    }

    public void ButtonTest()
    {
        Debug.Log("Building terrain");
        BuildTerrain();
    }

    public void SetGridPosition(Vector2 pos)
    {
        previousGridOffset = gridOffset;
        gridOffset = pos;
        _initializedMeshes = new bool[LODUtility.MaxLODCount()]; // reset the array to all false

        // destroy them for now, in the future we need to do something with pools to make this faster
        int structureCount = _structureContainer.transform.childCount;
        for (int i = structureCount - 1; i >= 0; --i)
        {
            Destroy(_structureContainer.transform.GetChild(i).gameObject);
        }
    }

    public struct MeshGenerationOutput
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
        public Mesh.MeshDataArray meshDataArray;
        public int targetLod;

        public Action<MeshGenerationOutput, bool> callback;

        public void SetMeshDataArray(ref Mesh.MeshDataArray meshDataArray)
        {
            this.meshDataArray = meshDataArray;
        }
    }

    public struct MeshGenerationInput
    {
        public TerrainMap map;
        public int dim;
        public float vertexScale;
        public float remapMin;
        public float remapMax;
        public AnimationCurve terrainCurve;
        public int resolution;
        public Mesh.MeshDataArray meshDataArray;

        public MeshGenerationInput(TerrainMap map, int dim, float vertexScale, float remapMin, float remapMax, AnimationCurve terrainCurve, int resolution, ref Mesh.MeshDataArray meshDataArray)
        {
            this.map = map;
            this.dim = dim;
            this.vertexScale = vertexScale;
            this.remapMin = remapMin;
            this.remapMax = remapMax;
            this.terrainCurve = terrainCurve;
            this.resolution = resolution;
            this.meshDataArray = meshDataArray;
        }
    }
    
    private class MeshBuilderThread
    {
        public enum MeshDataStreams : int
        {
            Vertices = 0,
            UVs = 1
        }
        
        private MeshGenerationInput meshInfo;
        private Queue<MeshGenerationOutput> resultQ;
        private Action<MeshGenerationOutput, bool> callback;
        private MeshGenerationOutput meshGenerationOutput;
        private NativeArray<VertexAttributeDescriptor> meshVertexAttributes; 
        
        public MeshBuilderThread(ref MeshGenerationInput meshInfo, Queue<MeshGenerationOutput> resultQ, Action<MeshGenerationOutput, bool> callback)
        {
            this.meshInfo = meshInfo;
            this.resultQ = resultQ;
            this.callback = callback;
        }

        public void ThreadProc()
        {
            meshGenerationOutput = new MeshGenerationOutput();
            meshGenerationOutput.callback = callback;
            meshGenerationOutput.meshDataArray = meshInfo.meshDataArray;
            meshGenerationOutput.targetLod = LODUtility.MeshResolutionToLOD(meshInfo.resolution);
            // meshGenerationOutput.SetMeshDataArray(ref meshDataArray);

            Debug.Log("Mesh Done");
            
            SetVertexDescriptors();
            int targetLOD = LODUtility.MeshResolutionToLOD(meshInfo.resolution);
            Mesh.MeshData data = meshInfo.meshDataArray[targetLOD];
            data.SetVertexBufferParams(meshInfo.map.GetVertexArraySizeAtLOD(meshInfo.resolution), meshVertexAttributes);
            data.SetIndexBufferParams(meshInfo.map.GetIndexArraySizeAtLOD(meshInfo.resolution), IndexFormat.UInt32);
            meshVertexAttributes.Dispose();
            
            
            // CreateVerts();
            CreateVertsNative(data);
            // CreateTris();
            CreateTrisNative(data);
            // CreateUVs();
            CreateUVsNative(data);

            // Debug.Log("Mesh will have verts[" + meshInfo.dim * meshInfo.dim + "] tris[" + (meshInfo.dim - 1) * (meshInfo.dim - 1) + "] uvs[" + meshGenerationOutput.uvs.Length + "]");

            data.subMeshCount = 1;
            MeshUpdateFlags smflags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;
            SubMeshDescriptor smdes = new SubMeshDescriptor(0, meshInfo.map.GetIndexArraySizeAtLOD(meshInfo.resolution));
            data.SetSubMesh(0, smdes, smflags);

            lock (resultQ) resultQ.Enqueue(meshGenerationOutput);
        }
        
        private void CreateVerts()
        {
            Debug.Log(meshInfo.map);
            meshGenerationOutput.vertices = meshInfo.map.GetRemappedFlattenedVectorMap(
                    vertScale: meshInfo.vertexScale, 
                    min: meshInfo.remapMin, 
                    max: meshInfo.remapMax, 
                    curve: meshInfo.terrainCurve, 
                    lod: meshInfo.resolution
                );
        }

        private void CreateVertsNative(Mesh.MeshData data)
        {
            var vertdata = data.GetVertexData<float3>();
            meshInfo.map.GetVectorMapNative(
                nativeMap: ref vertdata,
                vertScale: meshInfo.vertexScale, 
                min: meshInfo.remapMin, 
                max: meshInfo.remapMax, 
                curve: meshInfo.terrainCurve, 
                lod: meshInfo.resolution
            );
        }
    
        private void CreateTris()
        {
            List<int> trilist = new List<int>();
            
            int width = meshInfo.dim - 1;
            
            for( int z = 0; z < width; ++z )
            {
                int offset = z * (width + 1); // offset
                for (int x = 0; x < width; ++x)
                {
                    int bl = x + offset;
                    int tl = x + width + offset + 1;
                    int tr = x + width + offset + 2;
                    int br = x + offset + 1;
                
                    // left tri
                    trilist.Add(tl);
                    trilist.Add(br);
                    trilist.Add(bl);

                    // right tri
                    trilist.Add(tl);
                    trilist.Add(tr);
                    trilist.Add(br);
                }
            }
        
            // Debug.Log("Mesh has " + trilist.Count / 3 + " triangles");
            meshGenerationOutput.triangles = trilist.ToArray();
        }

        private void CreateTrisNative(Mesh.MeshData data)
        {
            var trilist = data.GetIndexData<uint>();
            uint width = (uint)meshInfo.dim - 1;
            int index = 0;
            
            for( uint z = 0; z < width; ++z )
            {
                uint offset = z * (width + 1); // offset
                for (uint x = 0; x < width; ++x)
                {
                    uint bl = x + offset;
                    uint tl = x + width + offset + 1;
                    uint tr = x + width + offset + 2;
                    uint br = x + offset + 1;
                
                    // left tri
                    trilist[index++] = tl;
                    trilist[index++] = br;
                    trilist[index++] = bl;

                    // right tri
                    trilist[index++] = tl;
                    trilist[index++] = tr;
                    trilist[index++] = br;
                }
            }
        }
        
        private void CreateUVs( )
        {
            meshGenerationOutput.uvs = new Vector2[meshGenerationOutput.vertices.Length];

            for (int i = 0; i < meshGenerationOutput.vertices.Length; ++i)
            {
                meshGenerationOutput.uvs[i] = new Vector2(meshGenerationOutput.vertices[i].x, meshGenerationOutput.vertices[i].z);
            }
        }

        private void CreateUVsNative(Mesh.MeshData data)
        {
            var uvs = data.GetVertexData<float2>(1);
            int dim = meshInfo.map.GetMeshDimAtLOD(meshInfo.resolution);
            int i = 0;

            for (int z = 0; z < dim; ++z)
            {
                for (int x = 0; x < dim; ++x)
                {
                    uvs[i] = new float2(x * meshInfo.vertexScale, z * meshInfo.vertexScale);
                    ++i;
                }
            }
        }

        private void SetVertexDescriptors()
        {
            // make temp vertex attribute descriptors for vertex position and UV streams
            VertexAttributeDescriptor vertexPositionStream = new VertexAttributeDescriptor(VertexAttribute.Position, dimension: 3); // stream 0
            VertexAttributeDescriptor vertexUvStream = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2, stream: 1); // stream 1
            int vertexAttributeCount = 2;
        
            // set up the mesh vertex attributes position stream and UV stream
            meshVertexAttributes = new NativeArray<VertexAttributeDescriptor>(vertexAttributeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            meshVertexAttributes[0] = vertexPositionStream;
            meshVertexAttributes[1] = vertexUvStream;
        }
    }

    public struct MeshBuilderIJob : IJob
    {
        public MeshGenerationInput meshInfo;
        public Queue<MeshGenerationOutput> resultQ;
        public Action<MeshGenerationOutput, bool> callback;
        public MeshGenerationOutput meshGenerationOutput;
        
        public void Execute()
        {
            meshGenerationOutput = new MeshGenerationOutput();
            meshGenerationOutput.callback = callback;

            CreateVerts();
            CreateTris();
            CreateUVs();
            
            lock (resultQ) resultQ.Enqueue(meshGenerationOutput);
        }
        
        private void CreateVerts()
        {
            Debug.Log(meshInfo.map);
            meshGenerationOutput.vertices = meshInfo.map.GetRemappedFlattenedVectorMap(
                vertScale: meshInfo.vertexScale, 
                min: meshInfo.remapMin, 
                max: meshInfo.remapMax, 
                curve: meshInfo.terrainCurve, 
                lod: meshInfo.resolution
            );
        }
    
        private void CreateTris()
        {
            List<int> trilist = new List<int>();
            
            int width = meshInfo.dim - 1;
            
            for( int z = 0; z < width; ++z )
            {
                int offset = z * (width + 1); // offset
                for (int x = 0; x < width; ++x)
                {
                    int bl = x + offset;
                    int tl = x + width + offset + 1;
                    int tr = x + width + offset + 2;
                    int br = x + offset + 1;
                
                    // left tri
                    trilist.Add(tl);
                    trilist.Add(br);
                    trilist.Add(bl);

                    // right tri
                    trilist.Add(tl);
                    trilist.Add(tr);
                    trilist.Add(br);
                }
            }
        
            // Debug.Log("Mesh has " + trilist.Count / 3 + " triangles");
            meshGenerationOutput.triangles = trilist.ToArray();
        }

        private void CreateUVs( )
        {
            meshGenerationOutput.uvs = new Vector2[meshGenerationOutput.vertices.Length];

            for (int i = 0; i < meshGenerationOutput.vertices.Length; ++i)
            {
                meshGenerationOutput.uvs[i] = new Vector2(meshGenerationOutput.vertices[i].x, meshGenerationOutput.vertices[i].z);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // float width = meshSquares * vertexScale;
        // Vector3 center = new Vector3(transform.position.x + width / 2, transform.position.y, transform.position.z + width / 2);
        // Vector3 size = new Vector3(width, 1, width);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_meshBounds.center, _meshBounds.size);
    }
}

[CustomEditor(typeof(MeshManager))]
public class SomeScriptEditor : Editor 
{
    MeshManager manager;
    private SerializedProperty gridOffset;
    private SerializedProperty heightMapSprite;
    private SerializedProperty debugImg;
    private SerializedProperty heightMapTex;
    private SerializedProperty altitudeTex;
    private SerializedProperty normalTex;

    void OnEnable()
    {
        gridOffset = serializedObject.FindProperty("gridOffset");
        heightMapSprite = serializedObject.FindProperty("_heightMapSprite");
        debugImg = serializedObject.FindProperty("img");
        heightMapTex = serializedObject.FindProperty("heightMapTex");
        altitudeTex = serializedObject.FindProperty("altitudeMapTex");
        normalTex = serializedObject.FindProperty("normalMapTex");
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        MeshManager script = (MeshManager) target;
        if (GUILayout.Button("Preview Maps"))
        {
            script.ButtonTest();
            EditorUtility.SetDirty(script);
            serializedObject.Update();
        }

        // EditorGUILayout.ObjectField("Height Map", heightMapSprite.objectReferenceValue, typeof(Sprite), false);
        
        if ( heightMapTex.objectReferenceValue ) EditorGUI.DrawPreviewTexture(new Rect(20, 30, 150, 150), (Texture2D) heightMapTex.objectReferenceValue);
        if ( altitudeTex.objectReferenceValue ) EditorGUI.DrawPreviewTexture(new Rect(20 + 150 + 5, 30, 150, 150), (Texture2D) altitudeTex.objectReferenceValue);
        if ( normalTex.objectReferenceValue ) EditorGUI.DrawPreviewTexture(new Rect(20 + 150 + 5 + 150 + 5, 30, 150, 150), (Texture2D) normalTex.objectReferenceValue);
        // float val = EditorGUIUtility.currentViewWidth / 150;
        // EditorGUI.PrefixLabel(new Rect(25, 180, 100, 15), 0, new GUIContent(val.ToString("n2")));
            
        // EditorGUI.DrawPreviewTexture(new Rect(20 + 150 + 5, 30, 150, 150), (Texture2D) heightMapTex.objectReferenceValue);
        // EditorGUI.DrawPreviewTexture(new Rect(20 + 150 + 5 + 150 + 5, 30, 150, 150), (Texture2D) heightMapTex.objectReferenceValue);

        EditorGUILayout.Space(170);

        DrawDefaultInspector();
            
        serializedObject.ApplyModifiedProperties();
        
        // DrawDefaultInspector();
    }
}
