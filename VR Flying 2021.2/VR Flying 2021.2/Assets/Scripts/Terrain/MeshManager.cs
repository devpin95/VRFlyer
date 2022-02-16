using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MeshManager : MonoBehaviour
{
    private Mesh _mesh;
    private Vector3[] _vertices;
    private int[] _triangles;
    private Vector3[] _normals;
    private Vector2[] _uvs;

    [Header("Mesh")]
    public int meshSquares = 239;
    public int meshVerts = 240;
    public Vector2 gridOffset;
    public Vector2 worldOffset;
    [FormerlySerializedAs("offsetScale")] public float chunkSize = 5;
    public float vertexScale = 1;
    public AnimationCurve terrainCurve;
    [Range(0, 6)]
    public int LOD = 6;
    
    [Header("Terrain")]
    public int noiseOctaves = 5;
    public float remapMin = -500;
    public float remapMax = 500;

    [Header("LOD Distances")] 
    private Transform playerTransform;
    private Vector3 previousPlayerPosition;
    public float recalculateLodDistance;
    public float distanceFromPlayer = 0;
    public List<LODInfo> lods = new List<LODInfo>();

    [Header("Maps")]
    [Range(0, 1)] public float altitudeVal;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
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
    
    [Header("DEBUG")] 
    public Image img;

    // Start is called before the first frame update
    void Awake()
    {
        _terrainMaterial = GetComponent<MeshRenderer>().material;
        _gameData = GameObject.Find("Game Manager").GetComponent<GameManager>().gameData;

        _mapThreadResultQueue = new Queue<TerrainMap.MapGenerationOutput>();
        _meshThreadResultQueue = new Queue<MeshGenerationOutput>();

        playerTransform = FindObjectOfType<WorldRepositionManager>().playerTransform;

        _terrainMap = new TerrainMap();
        _terrainMap.InitMap(meshVerts);
        _meshBounds = new Bounds();

        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();

        _mesh = new Mesh();
        _mesh.MarkDynamic();
    }

    private void Start()
    {
        previousPlayerPosition = playerTransform.position;
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

        // only recalculate the LOD when the player has traveled a certain distance
        if (Vector3.Distance(previousPlayerPosition, playerTransform.position) > recalculateLodDistance)
        {
            startup = false; // make sure to set this to false so we only force this loop once at the start
            
            // loop through all of the LODs and determine which LOD we need to target
            // loop backwards and if the player distance is greater than any LOD distance, we know the mesh needs
            // to be at the LOD
            bool lodChange = false;
            int targetLOD = lods[lods.Count - 1].lod;
            for (int i = lods.Count - 1; i >= -1; --i)
            {
                // if we loop through all of the LODs and havent found one, then we are close enough for the highest LOD
                if (i == -1)
                {
                    targetLOD = 0;
                    lodChange = true;
                    break;
                }
                
                // if we after further away that the LOD distance, we must need to use this LOD
                if (distanceFromPlayer > lods[i].distance)
                {
                    targetLOD = lods[i].lod;
                    lodChange = true;
                    break;
                }
            }

            // only swap our LOD if we actually need to swap to a different LOD
            if (targetLOD / 2 != LOD && lodChange)
            {
                LOD = targetLOD / 2;
                
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
    
    public void BuildTerrain()
    {
        _terrainMap.InitMap(meshVerts);
        
        Vector2 nworldOffset = new Vector2(gridOffset.x, gridOffset.y);

        if (_gameData)
            nworldOffset = new Vector2(gridOffset.x + _gameData.PerlinOffset.x, gridOffset.y + _gameData.PerlinOffset.y);
        
        worldOffset = nworldOffset;

        _terrainMap.RequestMap(nworldOffset, chunkSize, callback: MapGenerationReceived, _mapThreadResultQueue, octaves: noiseOctaves);
    }

    private void MapGenerationReceived(TerrainMap.MapGenerationOutput output, bool stub = false)
    {
        StartCoroutine(MapGenerationCoroutine(output, stub));
    }

    IEnumerator MapGenerationCoroutine(TerrainMap.MapGenerationOutput output, bool stub = false)
    {
        if (!stub)
        {
            _terrainMap.SetMap(output.map, output.min, output.max);
            yield return null;
            heightMapTex = _terrainMap.GetHeightMapTexture2D();
            yield return null;
            altitudeMapTex = _terrainMap.GetAltitudeMap(altitudeVal, TerrainMap.ALTITUDE_BELOW);
            yield return null;
            normalMapTex = _terrainMap.GetNormalMapTex2D(remapMin, remapMax);
        }

        yield return null;
        
        // clear the mesh arrays before we start making new ones
        if (_uvs != null) Array.Clear(_uvs, 0, _vertices.Length); // do this one first because we dont want to clear _vertices before we get it's length here
        yield return null;
        if (_vertices != null) Array.Clear(_vertices, 0, _vertices.Length);
        yield return null;
        if (_triangles != null) Array.Clear(_triangles, 0, _triangles.Length);
        yield return null;

        // set up the mesh generation input
        // int lodMeshVerts = meshVerts / (LOD == 0 ? 1 : LOD * 2);
        // int lodMeshSquares = lodMeshVerts - 1;
        int meshDim = (meshVerts - 1) / (LOD == 0 ? 1 : LOD * 2) + 1;
        MeshGenerationInput meshGenerationInput = new MeshGenerationInput(_terrainMap, meshDim, vertexScale, remapMin, remapMax, terrainCurve, (LOD == 0 ? 1 : LOD * 2), stub);
        
        yield return null;
        
        // make the new builder thread object with the input, the output queue, and the callback we need to invoke
        MeshBuilderThread builderThread = new MeshBuilderThread(meshGenerationInput, _meshThreadResultQueue, MeshGenerationReceived);
        
        yield return null;

        // make a ThreadStart that will call the MeshBuilderThread.ThreadProc and to run the mesh generation on a thread
        ThreadStart threadStart = delegate { builderThread.ThreadProc(); };

        Thread meshThread = new Thread(threadStart);
        meshThread.Start();
        meshThread.Join();
        
        yield return null;
    }
    
    private void MeshGenerationReceived(MeshGenerationOutput generationOutput, bool reposition = true)
    {
        StartCoroutine(MeshGenerationCoroutine(generationOutput));
    }

    IEnumerator MeshGenerationCoroutine(MeshGenerationOutput generationOutput)
    {
        Debug.Log("Building mesh in Coroutine");
        
        _mesh.Clear();
        
        yield return null;
        
        // reset the mesh with the verts, triangles, and uvs that were generated in the thread
        _mesh.vertices = generationOutput.vertices;

        yield return null;
        
        _mesh.triangles = generationOutput.triangles;
        
        yield return null;
        
        _mesh.uv = generationOutput.uvs;
        
        yield return null;
        
        // recalculate the normals so that everything looks right
        _mesh.RecalculateNormals();
        
        yield return null;
        
        _mesh.MarkModified();
        
        // updated the mesh on the filter and collider
        _meshFilter.sharedMesh = _mesh;
        _meshCollider.sharedMesh = _mesh;
        
        yield return null;
        
        // position the mesh where it needs to go in grid and world space
        float fullwidth = meshSquares * vertexScale; // the full width to move the block to it's correct position
        transform.position = new Vector3(gridOffset.x * fullwidth, 0, gridOffset.y * fullwidth) + (generationOutput.reposition ? _repositionOffset : Vector3.zero);
        
        yield return null;
        
        CalculateBounds();
    }

    private void CalculateBounds()
    {
        float width = meshVerts * vertexScale;
        float miny = _terrainMap.MapMinRemapped(remapMin, remapMax, terrainCurve);
        float maxy = _terrainMap.MapMaxRemapped(remapMin, remapMax, terrainCurve);
        float ydelta = maxy - miny;
        float verticalCenter = miny + (maxy - miny) / 2;
        Vector3 center = new Vector3(transform.position.x + width / 2, verticalCenter / 2, transform.position.z + width / 2);
        Vector3 size = new Vector3(width, ydelta, width);
        _meshBounds.center = center;
        _meshBounds.size = size;
    }
    
    private void BuildStructures()
    {
        float fullwidth = meshSquares * vertexScale;
        
        int numStructures = Random.Range(0, Random.Range(0, 10));
        
        Debug.Log(transform.name + " generating " + numStructures + " structures");

        for (int i = 0; i < numStructures; ++i)
        {
            GameObject prefab = _gameData.structureList[Random.Range(0, _gameData.structureList.Count)];
            GameObject structure = Instantiate(prefab, transform);
            structure.transform.SetParent(transform); // make sure that the terrain is the structures parent
            structure.transform.localPosition = Vector3.zero;

            // set a random position for the structure
            float randx = Random.Range(0, fullwidth);
            float randz = Random.Range(0, fullwidth);
            Vector3 structurePos = new Vector3(randx, 5000, randz);
            structure.transform.localPosition = structurePos;
            
            structure.GetComponent<Structure>().PlantStructure();
        }
    }
    
    private void CreateVerts()
    {
        _vertices = _terrainMap.GetRemappedFlattenedVectorMap(vertScale: vertexScale, min: remapMin, max: remapMax, curve: terrainCurve);
        // _vertices[0] = new Vector3(_vertices[0].x, remapMax, _vertices[0].z);
    }
    
    private void CreateTris()
    {
        // checkpointNotification.Raise("Generating " + (_data.dimension + 2) * (_data.dimension + 2) + " polygons...");
        List<int> trilist = new List<int>();

        for( int z = 0; z < meshSquares; ++z )
        {
            int offset = z * (meshSquares + 1); // offset
            for (int x = 0; x < meshSquares; ++x)
            {
                int bl = x + offset;
                int tl = x + meshSquares + offset + 1;
                int tr = x + meshSquares + offset + 2;
                int br = x + offset + 1;
                
                // left tri
                trilist.Add(br);
                trilist.Add(tl);
                trilist.Add(bl);

                // right tri
                trilist.Add(tr);
                trilist.Add(tl);
                trilist.Add(br);
            }
        }
        
        // Debug.Log("Mesh has " + trilist.Count / 3 + " triangles");
        _triangles = trilist.ToArray();
    }

    private void CreateNormals()
    {
        _normals = Utilities.Flatten2DArray(_terrainMap.GetNormalMap(remapMin, remapMax), meshVerts, meshVerts);
    }

    private void CreateUVs()
    {
        _uvs = new Vector2[_vertices.Length];
        
        for (int i = 0; i < _vertices.Length; ++i) _uvs[i] = new Vector2(_vertices[i].x, _vertices[i].z);
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

    public void ButtonTest()
    {
        Debug.Log("Building terrain");
        BuildTerrain();
    }

    public void SetGridPosition(Vector2 pos)
    {
        gridOffset = pos;
        // BuildTerrain();
        ManualReposition(_repositionOffset);
    }
    
    private struct MeshGenerationOutput
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;

        public bool reposition;
        // private Vector3[] normals;

        public Action<MeshGenerationOutput, bool> callback;
    }

    private struct MeshGenerationInput
    {
        public TerrainMap map;
        public int dim;
        public float vertexScale;
        public float remapMin;
        public float remapMax;
        public AnimationCurve terrainCurve;
        public int lod;
        public bool reposition;

        public MeshGenerationInput(TerrainMap map, int dim, float vertexScale, float remapMin, float remapMax, AnimationCurve terrainCurve, int lod, bool reposition)
        {
            this.map = map;
            this.dim = dim;
            this.vertexScale = vertexScale;
            this.remapMin = remapMin;
            this.remapMax = remapMax;
            this.terrainCurve = terrainCurve;
            this.lod = lod;
            this.reposition = reposition;
        }
    }
    
    private class MeshBuilderThread
    {
        private MeshGenerationInput meshInfo;
        private Queue<MeshGenerationOutput> resultQ;
        private Action<MeshGenerationOutput, bool> callback;
        private MeshGenerationOutput meshGenerationOutput;
        
        public MeshBuilderThread(MeshGenerationInput meshInfo, Queue<MeshGenerationOutput> resultQ, Action<MeshGenerationOutput, bool> callback)
        {
            this.meshInfo = meshInfo;
            this.resultQ = resultQ;
            this.callback = callback;
        }

        public void ThreadProc()
        {
            meshGenerationOutput = new MeshGenerationOutput();
            meshGenerationOutput.callback = callback;

            CreateVerts();
            CreateTris();
            CreateUVs();
            
            // Debug.Log("Mesh will have verts[" + meshInfo.dim * meshInfo.dim + "] tris[" + (meshInfo.dim - 1) * (meshInfo.dim - 1) + "] uvs[" + meshGenerationOutput.uvs.Length + "]");

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
                    lod: meshInfo.lod
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
        
        Gizmos.color = new Color(1, 0, 0, 0.75F);
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
