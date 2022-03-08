using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

public class TerrainManager : MonoBehaviour
{
    // public List<MeshManager> meshManagers;
    
    public TerrainInfo terrainInfo;

    public Transform terrainRootObject;

    [Header("Player")] 
    public Transform playerTransform;
    public float playerTravelDistanceUpdateThreshold = 25;
    [Tooltip("The number of grid units from the player to draw.\nReal distance = viewDistance * vertexScale * meshVerts")]
    public int viewDistance = 1;

    private float realViewDistance = 1;

    [Header("Tracking Variables")]
    [SerializeField] private Vector2 _playerGridPosition = new Vector2(float.MaxValue, float.MaxValue);
    private Vector3 _oldPlayerPos = Vector3.zero;
    public Vector3 totalWorldOffset = Vector3.zero;

    private WorldRepositionManager worldRepositionManager;

    private bool firstUpdate = true;

    private List<GridCoordinate> _activeGridCoordinates;
    private Dictionary<GridCoordinate, GameObject> _activeGridChunks;
    private struct GridCoordinate
    {
        public int x;
        public int y;

        public GridCoordinate(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    private void Awake()
    {
        worldRepositionManager = FindObjectOfType<WorldRepositionManager>();
        _oldPlayerPos = playerTransform.position;
    }

    // Start is called before the first frame update
    void Start()
    {
        _activeGridCoordinates = new List<GridCoordinate>();
        _activeGridChunks = new Dictionary<GridCoordinate, GameObject>();

        // do this in start so that the player doesnt have to travel playerTravelDistanceUpdateThreshold before generating the terrain
        // actually, we can't do this in start because some of the terrain chunks will not have done their start so we will get an error
        // UpdateTerrainGrid(startup: true);
    }

    // Update is called once per frame
    void Update()
    {
        if (firstUpdate)
        {
            firstUpdate = false;
            UpdateTerrainGrid(startup: true);
        }
        
        if (Vector3.Distance(_oldPlayerPos, playerTransform.position) > playerTravelDistanceUpdateThreshold)
        {
            _oldPlayerPos = playerTransform.position;
            UpdateTerrainGrid();
        }
    }

    private void UpdateTerrainGrid(bool startup = false)
    {
        // get the grid position using the players current position
        int xGridOffset = Mathf.FloorToInt(worldRepositionManager.playerWorldPos.x / (terrainInfo.meshVerts * terrainInfo.vertexScale));
        int yGridOffset = Mathf.FloorToInt(worldRepositionManager.playerWorldPos.z / (terrainInfo.meshVerts * terrainInfo.vertexScale));
        Vector2 currentGridPos = new Vector2(xGridOffset, yGridOffset);
        
        // Debug.Log("Update grid " + currentGridPos);

        // if the player's current grid pos has changed, we need to find out which grid coordinates are now "in view"
        if (currentGridPos != _playerGridPosition || startup)
        {
            // reset the players grid position for testing in Update
            _playerGridPosition = new Vector2(xGridOffset, yGridOffset);
            Debug.Log("Player moved from " + _playerGridPosition + " to " + currentGridPos);

            // create a list of grid coordinates that we need for the player's current position
            // this list will be used to test which chunk instances are no longer needed and can be relinquished
            List<GridCoordinate> _newActiveGrid = new List<GridCoordinate>();
            
            // loop through [-viewDistance, viewDistance] and create a list of all the grid coords we need
            for (int y = (int) _playerGridPosition.y - viewDistance; y <= (int) _playerGridPosition.y + viewDistance; ++y)
            {
                for ( int x = (int) _playerGridPosition.x - viewDistance; x <= (int) _playerGridPosition.x + viewDistance; ++x)
                {
                    // make a new coord obj and add it to the list of the active grid coords for this update
                    GridCoordinate targetCoord = new GridCoordinate(x, y);
                    _newActiveGrid.Add(targetCoord);
                }
            }
            
            string removedItems = "";
            List<GridCoordinate> itemsToRemove = new List<GridCoordinate>();
            // now that we have a list of the grid chunk we currently need, we can go through the list of active
            // chunks and make a new list of items to remove remove
            foreach (var activeChunk in _activeGridChunks)
            {
                if (!_newActiveGrid.Contains(activeChunk.Key))
                {
                    // if the GridCoordinate of this active chunk is not in the list of coords we need now, remove it
                    // from the active list and relinquish the chunk instance
                    removedItems += "[" + activeChunk.Key.x + ", " + activeChunk.Key.y + "] ";
                    TerrainChunkPool.Instance.RelinquishMeshChunkInstance(activeChunk.Value);
                    itemsToRemove.Add(activeChunk.Key);
                }
            }

            // now that we have a list of items ot remove, remove them
            foreach (var item in itemsToRemove)
            {
                _activeGridChunks.Remove(item);
            }

            StartCoroutine(BuildTerrainCoroutine(_newActiveGrid));

            // string addedItems = "";
            //
            // // now loop through all of the coords we found earlier and generate a new mesh at that coord
            // foreach (var coord in _newActiveGrid)
            // {
            //     // if the target coordinate is NOT in the list, that means that chunk has not been instantiated
            //     // we need to request a grid chunk and initialize it for that target coord
            //     if (!_activeGridChunks.ContainsKey(coord))
            //     {
            //         // request a chunk instances
            //         GameObject chunkInstance = TerrainChunkPool.Instance.RequestMeshChunkInstance(terrainRootObject);
            //         
            //         // add this coord/chunkInstance to the dictionary of active chunks for later
            //         _activeGridChunks.Add(coord, chunkInstance);
            //
            //         // set up the mesh manager before we ask it to generate the terrain
            //         MeshManager meshManager = chunkInstance.GetComponent<MeshManager>();
            //         
            //         if ( meshManager == null ) Debug.LogWarning("No object returned from pool");
            //         
            //         meshManager.SetRepositionOffset(totalWorldOffset); // set the reposition offset for this chunk so that it can be positioned properly
            //         meshManager.SetGridPosition(new Vector2(coord.x, coord.y)); // set the grid coordinate for this chunk
            //         // meshManager.EnableRenderer(); // make sure that the renderer is re-enabled if it was off
            //         // meshManager.SetLod(0);
            //
            //         // get the chunk to start building the terrain
            //         meshManager.BuildTerrain();
            //
            //         addedItems += "[" + coord.x + ", " + coord.y + "]";
            //     }
            // }
            //
            // Debug.Log("Added " + addedItems + " " +"Removed " + removedItems);

        }
    }

    private IEnumerator BuildTerrainCoroutine(List<GridCoordinate> newActiveGrid)
    {
        string addedItems = "";
        foreach (var coord in newActiveGrid)
        {
            // if the target coordinate is NOT in the list, that means that chunk has not been instantiated
            // we need to request a grid chunk and initialize it for that target coord
            if (!_activeGridChunks.ContainsKey(coord))
            {
                // request a chunk instances
                GameObject chunkInstance = TerrainChunkPool.Instance.RequestMeshChunkInstance(terrainRootObject);
                    
                // add this coord/chunkInstance to the dictionary of active chunks for later
                _activeGridChunks.Add(coord, chunkInstance);

                // set up the mesh manager before we ask it to generate the terrain
                MeshManager meshManager = chunkInstance.GetComponent<MeshManager>();
                    
                if ( meshManager == null ) Debug.LogWarning("No object returned from pool");
                    
                meshManager.SetRepositionOffset(totalWorldOffset); // set the reposition offset for this chunk so that it can be positioned properly
                meshManager.SetGridPosition(new Vector2(coord.x, coord.y)); // set the grid coordinate for this chunk
                // meshManager.EnableRenderer(); // make sure that the renderer is re-enabled if it was off
                // meshManager.SetLod(0);

                // get the chunk to start building the terrain
                meshManager.BuildTerrain();

                yield return new WaitForSeconds(1 / 60 * 5);

                addedItems += "[" + coord.x + ", " + coord.y + "]";
            }
        }
        
        Debug.Log("Added " + addedItems);
    }

    // receive the reposition event so that we can update mesh chunk positions that were not active when the event happened
    public void Reposition(Vector3 offset)
    {
        totalWorldOffset += offset;
    }

    // public void ButtonTest()
    // {
    //     foreach (var mesh in meshManagers)
    //     {
    //         mesh.meshVerts = meshVerts;
    //         mesh.meshSquares = meshVerts - 1;
    //         mesh.vertexScale = vertexScale;
    //         mesh.chunkSize = offsetScale;
    //         mesh.noiseOctaves = noiseOctaves;
    //         mesh.remapMin = remapMin;
    //         mesh.remapMax = remapMax;
    //         mesh.terrainCurve = terrainCurve;
    //         mesh.ButtonTest();
    //     }
    //
    //     int fullmapdim = (int)Mathf.Sqrt(meshManagers.Count);
    //     Texture2D fullmap = new Texture2D(fullmapdim, fullmapdim);
    // }
}

// [CustomEditor(typeof(TerrainManager))]
// public class TerrainManagerEditor : Editor 
// {
//     private SerializedProperty meshes;
//     private float mapdim = 150f;
//     private float edgesqr = 1;
//     private int submapdim = 1;
//     private float xoffset = 20;
//     private float yoffset = 30;
//
//     void OnEnable()
//     {
//         meshes = serializedObject.FindProperty("meshManagers");
//
//         edgesqr = (int)Mathf.Sqrt(meshes.arraySize);
//         submapdim = (int)(mapdim / edgesqr);
//         yoffset += submapdim * (edgesqr / 2);
//     }
//     
//     public override void OnInspectorGUI()
//     {
//         serializedObject.Update();
//         
//         TerrainManager script = (TerrainManager) target;
//         if (GUILayout.Button("Update Meshes"))
//         {
//             script.ButtonTest();
//             EditorUtility.SetDirty(script);
//             serializedObject.Update();
//         }
//
//         // EditorGUI.PrefixLabel(new Rect(25, 180, 100, 15), 0, new GUIContent(meshes.arraySize.ToString()));
//
//         // Debug.Log(submapdim);
//         //
//         // for (int i = 0; i < meshes.arraySize; ++i)
//         // {
//         //     SerializedProperty prop = meshes.GetArrayElementAtIndex(i);
//         //
//         //     SerializedObject obj = new SerializedObject(prop.objectReferenceValue);
//         //     
//         //     SerializedProperty map = obj.FindProperty("heightMapTex");
//         //     SerializedProperty offset = obj.FindProperty("gridOffset");
//         //     
//         //     // Debug.Log(map + " " + offset.vector2Value);
//         //
//         //     Vector2 moffset = offset.vector2Value;
//         //     float x = moffset.x * submapdim + xoffset;
//         //     float y = edgesqr - moffset.y * submapdim + yoffset;
//         //
//         //     Texture2D tex = (Texture2D) map.objectReferenceValue;
//         //     tex = Utilities.ResizeTexture2D(tex, submapdim, submapdim);
//         //     
//         //     if ( tex ) EditorGUI.DrawPreviewTexture(new Rect(x, y, submapdim, submapdim), tex);
//         // }
//         //
//         // EditorGUILayout.Space(edgesqr * submapdim + 30);
//
//         DrawDefaultInspector();
//             
//         serializedObject.ApplyModifiedProperties();
//         
//         // DrawDefaultInspector();
//     }
// }
