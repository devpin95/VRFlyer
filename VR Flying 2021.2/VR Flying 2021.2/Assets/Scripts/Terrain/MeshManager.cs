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
    public GPSInfo gpsInfo;
    private VegetationManager _vegetationManager;
    private ProceduralSeed _procSeed;
    
    
    private Mesh _mesh;
    private Vector3[] _vertices;
    private int[] _triangles;
    private Vector3[] _normals;
    private Vector2[] _uvs;

    [Header("Mesh")]
    public IntVector2 previousGridOffset;
    public IntVector2 gridOffset;
    public IntVector2 worldOffset;
    [Range(-1, 6)]
    public int LOD;

    [Header("Terrain")] 
    private GameObject _structureContainer;
    private GameObject _waterBodyContainer;

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

    private Queue<Terrain.Threading.TerrainMapGeneratorThread.MapGenerationOutput> _mapThreadResultQueue;
    private Queue<Terrain.Threading.MeshBuilderThread.MeshGenerationOutput> _meshThreadResultQueue;
    private Queue<Terrain.Threading.WaterBodyBuilderThread.WaterBuilderOutput> _waterThreadResultQueue;

    // flags
    private bool startup = true;
    private bool newGridPos = true;

    private bool[] _initializedMeshes;
    private LODMesh[] _meshLODs;
    private Mesh.MeshDataArray _meshDataArray;
    
    [Header("Events")]
    public CEvent initialGenerationEvent;
    public CEvent_Vector3 helipadSpawnPointNotification;
    public CEvent_Texture2D_IntVector2 gpsImageNotification;
    public CEvent_Vector2_IconType gpsIconNotification;
    public CEvent_GPSDestination gpsDestinationNotification;

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

    private struct HelipadConstructionData
    {
        public GameObject instance;
        public Vector3 landingPoint;
    }
    
    [Header("DEBUG")] 
    public Image img;

    public bool atCullDistance;
    public bool beneathCullHeight;
    public bool culled = false;
    private bool _switchingLODs = false;
    private IEnumerator _delayedLODDisable;

    // Start is called before the first frame update
    void Awake()
    {
        _terrainMaterial = GetComponent<MeshRenderer>().material;
        _gameData = GameObject.Find("Game Manager").GetComponent<GameManager>().gameData;

        _mapThreadResultQueue = new Queue<Terrain.Threading.TerrainMapGeneratorThread.MapGenerationOutput>();
        _meshThreadResultQueue = new Queue<Terrain.Threading.MeshBuilderThread.MeshGenerationOutput>();
        _waterThreadResultQueue = new Queue<Terrain.Threading.WaterBodyBuilderThread.WaterBuilderOutput>();

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
        _meshLODs = new LODMesh[LODUtility.MaxLODCount() + 1]; // add 1 mesh for bodies of water
        _meshDataArray = Mesh.AllocateWritableMeshData(LODUtility.MaxLODCount() + 1);
        
        // create an empty gameobject to hold all of the structures for a given terrain
        _structureContainer = new GameObject();
        _structureContainer.transform.position = Vector3.zero;
        _structureContainer.transform.SetParent(transform, worldPositionStays: false);
        _structureContainer.transform.name = "Structures";
        
        // create an empty gameobject to hold all of the structures for a given terrain
        _waterBodyContainer = new GameObject();
        _waterBodyContainer.transform.position = Vector3.zero;
        _waterBodyContainer.transform.SetParent(transform, worldPositionStays: false);
        _waterBodyContainer.transform.name = "Bodies of Water";

        _vegetationManager = GetComponent<VegetationManager>();
        
        _delayedLODDisable = DelayedLODDisable(_meshLODs[LOD].instance);
        _procSeed = GetComponent<ProceduralSeed>();
    }

    private void Start()
    {
        previousPlayerPosition = _worldRepositionManager.playerWorldPos;
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
            Terrain.Threading.TerrainMapGeneratorThread.MapGenerationOutput output = _mapThreadResultQueue.Dequeue();
            output.callback(output, false);
        }
        if (_meshThreadResultQueue.Count > 0)
        {
            Terrain.Threading.MeshBuilderThread.MeshGenerationOutput output = _meshThreadResultQueue.Dequeue();
            output.callback(output, true);
        }

        if (_waterThreadResultQueue.Count > 0)
        {
            Terrain.Threading.WaterBodyBuilderThread.WaterBuilderOutput output = _waterThreadResultQueue.Dequeue();
            output.callback(output);
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
        if (Vector3.Distance(previousPlayerPosition, _worldRepositionManager.playerWorldPos) > terrainInfo.recalculateLodDistance)
        {
            previousPlayerPosition = _worldRepositionManager.playerWorldPos;
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
                // set up a stub of the information we would have gotten from the map generation thread
                Terrain.Threading.TerrainMapGeneratorThread.MapGenerationOutput meshGenerationInfoStub = new Terrain.Threading.TerrainMapGeneratorThread.MapGenerationOutput();
                meshGenerationInfoStub.map = _terrainMap.Get2DHeightMap();
                meshGenerationInfoStub.min = _terrainMap.MapMin();
                meshGenerationInfoStub.max = _terrainMap.MapMax();
                meshGenerationInfoStub.attributes = _terrainMap.generatedAttributes;

                // call the function to start the mesh generation thread
                MapGenerationReceived(meshGenerationInfoStub, true);   
                
                //disable the old mesh
                // if ( _meshLODs[oldLOD].instance ) _meshLODs[oldLOD].instance?.SetActive(false);
                
                _switchingLODs = true;
                _delayedLODDisable = DelayedLODDisable(_meshLODs[oldLOD].instance);
                // if (startup)
                // {
                //     _meshLODs[oldLOD].instance?.SetActive(false);
                // }
                // else
                // {
                //     _delayedLODDisable = DelayedLODDisable(_meshLODs[oldLOD].instance);
                //     // StartCoroutine(_delayedLODDisable);
                // }
            }
        }
    }

    private IEnumerator DelayedLODDisable(GameObject obj)
    {
        if ( obj == null ) yield break;
        
        // Debug.Log("Disabling old LOD");
        
        // yield return new WaitUntil(() => !_switchingLODs);
        obj.SetActive(false);
        _switchingLODs = false; // make sure it is false
        _delayedLODDisable = null;
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
    
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    // Mesh Generation Pipeline ----------------------------------------------------------------------------------------
    public void BuildTerrain()
    {
        _terrainMap.InitMap(terrainInfo.meshVerts);
        
        IntVector2 nworldOffset = new IntVector2(gridOffset.x, gridOffset.y);

        if (_gameData)
            nworldOffset = new IntVector2(gridOffset.x + _gameData.PerlinOffset.x, gridOffset.y + _gameData.PerlinOffset.y);
        
        worldOffset = nworldOffset;

        _procSeed.InitState(RNGSalt());
        _terrainMap.SetTerrainAttribute(TerrainMap.Attributes.HAS_LAKE, _procSeed.Range(0f, 1f) < terrainInfo.pLakeSpawn);
        _terrainMap.SetTerrainAttribute(TerrainMap.Attributes.HAS_SPLIT_LAKE, false);
        _terrainMap.SetTerrainAttribute(TerrainMap.Attributes.HAS_TOWN, false);
        _terrainMap.SetTerrainAttribute(TerrainMap.Attributes.LAKE_TYPES, TerrainMap.TerrainAttributes.defaultLakeTypes);

        _terrainMap.nonContributingBiomes = terrainInfo.GetNonContributingBiomes();
        _terrainMap.contributingBiomes = terrainInfo.GetContributingBiomes();
        _terrainMap.offset = nworldOffset;
        _terrainMap.maxHeight = terrainInfo.MaxTerrainHeight();
        
        _terrainMap.RequestMap( callback: MapGenerationReceived, _mapThreadResultQueue);
    }

    private void MapGenerationReceived(Terrain.Threading.TerrainMapGeneratorThread.MapGenerationOutput output, bool stub = false)
    {
        StartCoroutine(MapGenerationCoroutine(output, stub));
    }

    IEnumerator MapGenerationCoroutine(Terrain.Threading.TerrainMapGeneratorThread.MapGenerationOutput mapGenerationOutput, bool stub = false)
    {
        if (!stub)
        {
            // if we get in here, then the caller of this function was the thread that generated the map
            // NOT the manager wanting a new LOD of the same map
            _terrainMap.SetMap(mapGenerationOutput.map, mapGenerationOutput.min, mapGenerationOutput.max, mapGenerationOutput.minpos, mapGenerationOutput.maxpos);
            _terrainMap.SetWaterMap(mapGenerationOutput.water);
            _terrainMap.mean = mapGenerationOutput.mean;
            _terrainMap.variance = mapGenerationOutput.variance;
            _terrainMap.stdDev = mapGenerationOutput.stdDev;
            _terrainMap.generatedAttributes = mapGenerationOutput.attributes;
         
            // check if the terrain generated a body of water
            if (mapGenerationOutput.attributes.conditions[(int)TerrainMap.Attributes.HAS_LAKE])
            {
                // if the terrain has a body of water, we need to start another thread that will generate the vertices
                // we need to build a water mesh
                Debug.Log(transform.name + " has generated a lake");
                Terrain.Threading.WaterBodyBuilderThread.WaterBuilderInput waterBuilderInput = new Terrain.Threading.WaterBodyBuilderThread.WaterBuilderInput(
                    mapGenerationOutput.water,
                    mapGenerationOutput.waterElevation * terrainInfo.MaxTerrainHeight(),
                    terrainInfo.meshVerts,
                    vertScale: terrainInfo.vertexScale,
                    ref _meshDataArray
                );
                Terrain.Threading.WaterBodyBuilderThread waterBuilderThread = new Terrain.Threading.WaterBodyBuilderThread(ref waterBuilderInput, _waterThreadResultQueue, WaterGenerationReceived);
        
                yield return null;
        
                ThreadPool.QueueUserWorkItem(delegate { waterBuilderThread.ThreadProc(); });
                
            }
        }

        yield return null;
        
        // Debug.Log(transform.name + " average height: " + output.mean);
        
        // set up the mesh generation input
        int meshDim = (terrainInfo.meshVerts - 1) / LODUtility.LODToMeshResolution(LOD) + 1;
        Terrain.Threading.MeshBuilderThread.MeshGenerationInput meshGenerationInput = new Terrain.Threading.MeshBuilderThread.MeshGenerationInput(
            _terrainMap, 
            meshDim, 
            terrainInfo.vertexScale, 
            0, 
            terrainInfo.MaxTerrainHeight(), 
            terrainInfo.terrainCurve, 
            LODUtility.LODToMeshResolution(LOD),
            ref _meshDataArray);

        yield return null;

        // ** thread pool **
        // -------------------------------------------------------------------------------------------------------------
        // // make the new builder thread object with the input, the output queue, and the callback we need to invoke
        Terrain.Threading.MeshBuilderThread builderThread = new Terrain.Threading.MeshBuilderThread(ref meshGenerationInput, _meshThreadResultQueue, MeshGenerationReceived);
        
        yield return null;
        
        ThreadPool.QueueUserWorkItem(delegate { builderThread.ThreadProc(); });

        yield return null;
    }
    
    private void MeshGenerationReceived(Terrain.Threading.MeshBuilderThread.MeshGenerationOutput meshGenerationOutput, bool reposition = true)
    {
        StartCoroutine(MeshGenerationCoroutine(meshGenerationOutput));
    }

    IEnumerator MeshGenerationCoroutine(Terrain.Threading.MeshBuilderThread.MeshGenerationOutput generationOutput)
    {
        int targetLod = generationOutput.targetLod; // make a copy of this so everything is easier to read later

        // if the mesh hasnt been created, create it
        // if the mesh is already created, then clear it so we can update it with the new heightmap
        if (_meshLODs[targetLod].instance == null) CreateMeshLODGameObject(targetLod);
        else _meshLODs[targetLod].mesh.Clear();

        yield return null;
        
        NativeArray<float3> targetVerts = generationOutput.meshDataArray[targetLod].GetVertexData<float3>((int)Terrain.Threading.MeshBuilderThread.MeshDataStreams.Vertices);
        NativeArray<float2> targetUVs = generationOutput.meshDataArray[targetLod].GetVertexData<float2>((int)Terrain.Threading.MeshBuilderThread.MeshDataStreams.UVs);
        NativeArray<uint> targetIndices = generationOutput.meshDataArray[targetLod].GetIndexData<uint>();
        
        _meshLODs[targetLod].mesh.SetVertices(targetVerts, 0, targetVerts.Length, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
        yield return null;
        _meshLODs[targetLod].mesh.SetUVs(0, targetUVs);
        yield return null;
        _meshLODs[targetLod].mesh.SetIndices(targetIndices, MeshTopology.Triangles, 0, calculateBounds: false);
        yield return null;
        
        _meshLODs[targetLod].mesh.RecalculateNormals();
        
        yield return null;
        
        _meshLODs[targetLod].mesh.RecalculateBounds();
        
        yield return null;
        
        _meshLODs[targetLod].mesh.MarkModified();
        
        yield return null;
        
        // updated the mesh on the filter and collider
        _meshLODs[targetLod].meshFilter.sharedMesh = _meshLODs[targetLod].mesh;
            
        yield return null;

        // only set the mesh collider for the highest LOD
        if (targetLod == 0)
        {
            _meshLODs[targetLod].meshCollider.sharedMesh = _meshLODs[targetLod].mesh;
        }

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

        _switchingLODs = false;
        if (!startup && _delayedLODDisable != null)
        {
            yield return _delayedLODDisable;
        }
        
        // if we built at a new grid position, do these things once instead of for each LOD
        if (newGridPos)
        {
            BuildStructures();
            BuildImages();
            newGridPos = false;
        }

        if (LOD == 0)
        {
            yield return _vegetationManager.GenerateVegetation(_terrainMap);
        }

        if (startup)
        {
            initialGenerationEvent.Raise();
            startup = false;
        }
    }

    private void WaterGenerationReceived(Terrain.Threading.WaterBodyBuilderThread.WaterBuilderOutput waterBuilderOutput)
    {
        Debug.Log("Water body builder output received...");
        StartCoroutine(WaterMeshGenerationCoroutine(waterBuilderOutput));
    }

    IEnumerator WaterMeshGenerationCoroutine(Terrain.Threading.WaterBodyBuilderThread.WaterBuilderOutput waterBuilderOutput)
    {
        var waterBody = WaterBodyPool.Instance.RequestWaterBodyInstance(_waterBodyContainer.transform);

        // if we couldnt get a water body object, then just exit
        if (waterBody == null) yield break;
        
        waterBody.transform.localPosition = Vector3.zero;

        int targetLod = _meshLODs.Length - 1;
        
        if (_meshLODs[targetLod].instance == null) CreateMeshLODGameObject(targetLod);
        else _meshLODs[targetLod].mesh.Clear();

        yield return null;
        
        NativeArray<float3> targetVerts = waterBuilderOutput.meshDataArray[targetLod].GetVertexData<float3>((int)Terrain.Threading.WaterBodyBuilderThread.MeshDataStreams.Vertices);
        NativeArray<float2> targetUVs = waterBuilderOutput.meshDataArray[targetLod].GetVertexData<float2>((int)Terrain.Threading.WaterBodyBuilderThread.MeshDataStreams.UVs);
        NativeArray<uint> targetIndices = waterBuilderOutput.meshDataArray[targetLod].GetIndexData<uint>();
        
        Debug.Log("Building water body mesh with " + targetVerts.Length + " verts, " + targetIndices.Length + " tris...");
        
        _meshLODs[targetLod].mesh.SetVertices(targetVerts, 0, targetVerts.Length, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
        yield return null;
        _meshLODs[targetLod].mesh.SetUVs(0, targetUVs);
        yield return null;
        _meshLODs[targetLod].mesh.SetIndices(targetIndices, MeshTopology.Triangles, 0, calculateBounds: false);
        yield return null;
        _meshLODs[targetLod].mesh.RecalculateBounds();
        yield return null;
        _meshLODs[targetLod].mesh.RecalculateNormals();

        waterBody.GetComponent<MeshFilter>().sharedMesh = _meshLODs[targetLod].mesh;
        yield return null;
        waterBody.GetComponent<MeshCollider>().sharedMesh = _meshLODs[targetLod].mesh;
    }
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------

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
        float miny = 0;
        float maxy = terrainInfo.MaxTerrainHeight();
        // float miny = _terrainMap.MapMinRemapped(terrainInfo.remapMin, terrainInfo.remapMax, terrainInfo.terrainCurve);
        // float maxy = _terrainMap.MapMaxRemapped(terrainInfo.remapMin, terrainInfo.remapMax, terrainInfo.terrainCurve);
        float ydelta = maxy - miny;
        float verticalCenter = miny + (maxy - miny) / 2;
        Vector3 center = new Vector3(transform.position.x + width / 2, verticalCenter / 2, transform.position.z + width / 2);
        Vector3 size = new Vector3(width, ydelta, width);
        _meshBounds.center = center;
        _meshBounds.size = size;
    }
    
    private void BuildStructures()
    {
        if (gridOffset == IntVector2.zero) GridOriginStructures();
        else
        {
            // if within helipad spawn probability, spawn a helipad at the lowest point of the chunk
            if (_procSeed.RangeSingle(0f, 1f, RNGSalt()) < terrainInfo.pHelipadSpawn) BuildHelipad();
        }
    }

    private void BuildImages()
    {
        // Texture2D map = _terrainMap.GetAltitudeMap(terrainInfo.altitudeCutoff, TerrainMap.ALTITUDE_BELOW, terrainInfo.lowAltitudeColor, terrainInfo.highAltitudeColor);
        Texture2D map = _terrainMap.GetAltitudeMap(gpsInfo.gpsLevels);
        gpsImageNotification.Raise(map, gridOffset);
        _terrainMap.GetDebugBiomeMaps(terrainInfo.biomes, gridOffset);
    }

    private void GridOriginStructures()
    {
        HelipadConstructionData data = BuildHelipad();
        
        // if we're at startup, we need to announce where this helipad will be placed so that the helicopter can
        // be spawned at it instead of in the air
        if (startup)
        {
            Debug.Log("SPAWN POINT: " + data.landingPoint);
            // notify everyone about the spawn point location
            helipadSpawnPointNotification.Raise(data.landingPoint);
        }
    }

    private HelipadConstructionData BuildHelipad()
    {
        HelipadConstructionData data;
        
        Vector3 lowestPoint = _terrainMap.MapMinPosition();
        Vector3 localPosition = TerrainMap.ScaleMapPosition(lowestPoint, terrainInfo.vertexScale, terrainInfo.MaxTerrainHeight());
        // Vector3 localPosition = TerrainMap.ScaleMapPosition(lowestPoint, terrainInfo.meshVerts, terrainInfo.vertexScale, terrainInfo.terrainCurve, terrainInfo.remapMin, terrainInfo.remapMax, gridOffset);

        // Debug.Log(transform.name + " helipad at " + lowestPoint + " has local position " + localPosition);
        
        // get an instance of a helipad
        GameObject helipadInstance = Instantiate(_gameData.helipadPrefab, Vector3.zero, Quaternion.identity, parent: _structureContainer.transform);
        helipadInstance.transform.localPosition = localPosition;
        
        // reposition the helipad so that the point it touches the ground matches the legs of the helipad instead
        // of the pivot of the helipad object
        Vector3 helipadAttachPoint = helipadInstance.transform.Find("Attach Point").localPosition;
        // Vector3 helipadAttachPointToChunkPoint = localPosition - helipadAttachPoint;
        helipadInstance.transform.localPosition -= helipadAttachPoint;

        // get the point of the helipad where we want to attach the helicopter to so that it is "landed" on the pad
        Vector3 landingPoint = helipadInstance.transform.Find("Landing Point").transform.position;
        
        gpsIconNotification.Raise(new Vector2(helipadInstance.transform.position.x, helipadInstance.transform.position.z), GPSGUI.IconTypes.Helipad);

        data.instance = helipadInstance;
        data.landingPoint = landingPoint;
        
        // create a gps destination and notify the GPS
        GPSGUI.GPSDestination destination = new GPSGUI.GPSDestination();
        destination.icon = GPSGUI.IconTypes.Helipad;
        destination.name = (gridOffset == IntVector2.zero ? "Home" : "Pad " + _procSeed.RangeSingle(0, 1000,RNGSalt()));
        destination.preset = gridOffset == IntVector2.zero; // if at the origin, make this helipad a preset
        destination.worldPos = _worldRepositionManager.UnitySpaceToWorldSpace(helipadInstance.transform.position);
        gpsDestinationNotification.Raise(destination);

        return data;
    }

    private int RNGSalt()
    {
        return (gridOffset.x + gridOffset.y + gridOffset.x % 897 + gridOffset.y % 315);
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

    public void SetGridPosition(IntVector2 pos)
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
        
        // relinquish all of the bodies of water that were allocated to the previous terrain chunk
        int waterBodyCount = _waterBodyContainer.transform.childCount;
        for (int i = waterBodyCount - 1; i >= 0; --i)
        {
            WaterBodyPool.Instance.RelinquishWaterBodyInstance(_waterBodyContainer.transform.GetChild(i).gameObject);
        }

        newGridPos = true;
    }

    private void OnDrawGizmosSelected()
    {
        // float width = meshSquares * vertexScale;
        // Vector3 center = new Vector3(transform.position.x + width / 2, transform.position.y, transform.position.z + width / 2);
        // Vector3 size = new Vector3(width, 1, width);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_meshBounds.center, _meshBounds.size);

        if (FindObjectOfType<GameManager>().gameDebug)
        {
            int z = 0;
            for (int i = 0; i < _terrainMap.biomeMaps.Count; i += 2)
            {
                Gizmos.DrawGUITexture(new Rect(1000, (z / 2) * -1000, 1000, 1000), _terrainMap.biomeMaps[i]);
                Gizmos.DrawGUITexture(new Rect(2000, (z / 2) * -1000, 1000, 1000), _terrainMap.biomeMaps[i + 1]);
                z += 2;
            }
        }
        
        Handles.Label(new Vector3(750, 1000, 0), transform.name);
        Handles.Label(new Vector3(750, 975, 0), "min: " + _terrainMap.MapMin().ToString("n4"));
        Handles.Label(new Vector3(750, 950, 0), "max: " + _terrainMap.MapMax().ToString("n4"));
        Handles.Label(new Vector3(750, 925, 0), "mean: " + _terrainMap.mean.ToString("n4"));
        Handles.Label(new Vector3(750, 900, 0), "variance: " + _terrainMap.variance.ToString("n4"));
        Handles.Label(new Vector3(750, 875, 0), "standard dev.: " + _terrainMap.stdDev.ToString("n4"));
        Handles.Label(new Vector3(750, 850, 0), "min: " + _terrainMap.MapMin().ToString("n4"));
        Handles.Label(new Vector3(750, 825, 0), "max: " + _terrainMap.MapMax().ToString("n4"));
    }
}
