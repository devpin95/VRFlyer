using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    public List<MeshManager> meshManagers;

    [Header("Player")] 
    public Transform playerTransform;
    
    [Tooltip("The number of grid units from the player to draw.\nReal distance = viewDistance * vertexScale * meshVerts")]
    public float viewDistance = 1;
    private float realViewDistance = 1;
    
    [Header("Mesh Settings")]
    public int meshVerts = 255;
    public float offsetScale = 5;
    public float vertexScale = 1;
    public AnimationCurve terrainCurve;
    
    [Header("Terrain")]
    public int noiseOctaves = 5;
    public float remapMin = -500;
    public float remapMax = 500;

    // Start is called before the first frame update
    void Start()
    {
        realViewDistance = viewDistance * vertexScale * meshVerts;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ButtonTest()
    {
        foreach (var mesh in meshManagers)
        {
            mesh.meshVerts = meshVerts;
            mesh.meshSquares = meshVerts - 1;
            mesh.vertexScale = vertexScale;
            mesh.chunkSize = offsetScale;
            mesh.noiseOctaves = noiseOctaves;
            mesh.remapMin = remapMin;
            mesh.remapMax = remapMax;
            mesh.terrainCurve = terrainCurve;
            mesh.ButtonTest();
        }

        int fullmapdim = (int)Mathf.Sqrt(meshManagers.Count);
        Texture2D fullmap = new Texture2D(fullmapdim, fullmapdim);
    }
}

[CustomEditor(typeof(TerrainManager))]
public class TerrainManagerEditor : Editor 
{
    private SerializedProperty meshes;
    private float mapdim = 150f;
    private float edgesqr = 1;
    private int submapdim = 1;
    private float xoffset = 20;
    private float yoffset = 30;

    void OnEnable()
    {
        meshes = serializedObject.FindProperty("meshManagers");

        edgesqr = (int)Mathf.Sqrt(meshes.arraySize);
        submapdim = (int)(mapdim / edgesqr);
        yoffset += submapdim * (edgesqr / 2);
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        TerrainManager script = (TerrainManager) target;
        if (GUILayout.Button("Update Meshes"))
        {
            script.ButtonTest();
            EditorUtility.SetDirty(script);
            serializedObject.Update();
        }

        // EditorGUI.PrefixLabel(new Rect(25, 180, 100, 15), 0, new GUIContent(meshes.arraySize.ToString()));

        // Debug.Log(submapdim);
        //
        // for (int i = 0; i < meshes.arraySize; ++i)
        // {
        //     SerializedProperty prop = meshes.GetArrayElementAtIndex(i);
        //
        //     SerializedObject obj = new SerializedObject(prop.objectReferenceValue);
        //     
        //     SerializedProperty map = obj.FindProperty("heightMapTex");
        //     SerializedProperty offset = obj.FindProperty("gridOffset");
        //     
        //     // Debug.Log(map + " " + offset.vector2Value);
        //
        //     Vector2 moffset = offset.vector2Value;
        //     float x = moffset.x * submapdim + xoffset;
        //     float y = edgesqr - moffset.y * submapdim + yoffset;
        //
        //     Texture2D tex = (Texture2D) map.objectReferenceValue;
        //     tex = Utilities.ResizeTexture2D(tex, submapdim, submapdim);
        //     
        //     if ( tex ) EditorGUI.DrawPreviewTexture(new Rect(x, y, submapdim, submapdim), tex);
        // }
        //
        // EditorGUILayout.Space(edgesqr * submapdim + 30);

        DrawDefaultInspector();
            
        serializedObject.ApplyModifiedProperties();
        
        // DrawDefaultInspector();
    }
}
