using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
    public float offsetScale = 5;
    public float vertexScale = 1;
    public float yOffset = 0;
    
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
    [HideInInspector] public Texture2D heightMapTex;
    [HideInInspector] public Texture2D altitudeMapTex;
    [HideInInspector] public Texture2D normalMapTex;
    
    [Header("DEBUG")] 
    public Image img;

    // Start is called before the first frame update
    void Start()
    {
        // int minSampleRange = (int)gridOffset.x * 5;
        // int maxSampleRange = (int)gridOffset.y * 5;
        // Debug.Log(transform.gameObject.name + " Offset: " + gridOffset + " -> x(" + minSampleRange + ", " + (minSampleRange + 5) + ") z("  + maxSampleRange + ", " + (maxSampleRange + 5) + ")");
        BuildTerrain();
    }

    private void BuildTerrain()
    {
        _mesh = new Mesh();
        
        _terrainMap.InitMap(meshVerts);
        
        _terrainMap.GenerateMap(gridOffset, offsetScale, octaves: noiseOctaves);
        
        heightMapTex = _terrainMap.GetHeightMapTexture2D();
        altitudeMapTex = _terrainMap.GetAltitudeMap(altitudeVal, TerrainMap.ALTITUDE_BELOW);
        normalMapTex = _terrainMap.GetNormalMapTex2D(remapMin, remapMax);

        CreateVerts();
        CreateTris();
        // CreateNormals();
        CreateUVs();

        _mesh.vertices = _vertices;
        _mesh.triangles = _triangles;
        // _mesh.normals = _normals;
        _mesh.uv = _uvs;
        _mesh.RecalculateNormals();
        
        GetComponent<MeshFilter>().sharedMesh = _mesh;

        transform.position = new Vector3(gridOffset.x * meshSquares * vertexScale, 0, gridOffset.y * meshSquares * vertexScale);
    }
    
    private void CreateVerts()
    {
        _vertices = _terrainMap.GetRemappedFlattenedVector3VertMap(vertScale: vertexScale, min: remapMin, max: remapMax);
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
        transform.position += offset;
    }

    public void ButtonTest()
    {
        Debug.Log("Building terrain");
        BuildTerrain();
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
