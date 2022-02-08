using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public int meshSquares = 254;
    public int meshVerts = 255;
    public Vector2 gridOffset;
    public Vector2 worldOffset;
    [FormerlySerializedAs("offsetScale")] public float chunkSize = 5;
    public float vertexScale = 1;
    public AnimationCurve terrainCurve;
    
    [Header("Terrain")]
    public int noiseOctaves = 5;
    public float remapMin = -500;
    public float remapMax = 500;

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
    
    private Vector3 repositionOffset = Vector3.zero;

    private Queue<TerrainMap.MapGenerationData> _threadResultQueue;
    
    [Header("DEBUG")] 
    public Image img;

    // Start is called before the first frame update
    void Start()
    {
        _terrainMaterial = GetComponent<MeshRenderer>().material;
        _gameData = GameObject.Find("Game Manager").GetComponent<GameManager>().gameData;

        _threadResultQueue = new Queue<TerrainMap.MapGenerationData>();

        // while (true)
        // {
        //     if (_gameData.DataReady) break;
        // }
        
        // BuildTerrain();
    }

    private void Update()
    {
        // check if there is a map generation result in the queue
        if (_threadResultQueue.Count > 0)
        {
            // if there is, dequeue the data and invoke it's callback with the data
            TerrainMap.MapGenerationData data = _threadResultQueue.Dequeue();
            data.callback(data);
        }
    }

    private void BuildTerrain()
    {
        _mesh = new Mesh();

        _terrainMap = new TerrainMap();
        _terrainMap.InitMap(meshVerts);
        
        Vector2 nworldOffset = new Vector2(gridOffset.x, gridOffset.y);

        if (_gameData)
            nworldOffset = new Vector2(gridOffset.x + _gameData.PerlinOffset.x, gridOffset.y + _gameData.PerlinOffset.y);
        
        worldOffset = nworldOffset;

        _terrainMap.RequestMap(nworldOffset, chunkSize, callback: MapGenerationReceived, _threadResultQueue, octaves: noiseOctaves);
        
        // _terrainMap.GenerateMap(nworldOffset, chunkSize, remapMin, remapMax, noiseOctaves);
        // TerrainMap.MapGenerationData fakeData = new TerrainMap.MapGenerationData();
        // fakeData.map = _terrainMap.Get2DHeightMap();
        // MapGenerationReceived(fakeData);
    }

    private void MapGenerationReceived(TerrainMap.MapGenerationData data)
    {
        _terrainMap.SetMap(data.map, data.min, data.max);
        heightMapTex = _terrainMap.GetHeightMapTexture2D();
        altitudeMapTex = _terrainMap.GetAltitudeMap(altitudeVal, TerrainMap.ALTITUDE_BELOW);
        normalMapTex = _terrainMap.GetNormalMapTex2D(remapMin, remapMax);

        CreateVerts();
        CreateTris();
        CreateUVs();

        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
        // _mesh.normals = _normals;
        _mesh.uv = _uvs;
        _mesh.RecalculateNormals();
        
        GetComponent<MeshFilter>().sharedMesh = _mesh;
        GetComponent<MeshCollider>().sharedMesh = _mesh;

        float fullwidth = meshSquares * vertexScale; // the full width to move the block to it's correct position
        transform.position = new Vector3(gridOffset.x * fullwidth, 0, gridOffset.y * fullwidth);
        
        BuildStructures();
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
        repositionOffset += offset;
        transform.position += offset;
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
        BuildTerrain();
        ManualReposition(repositionOffset);
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
