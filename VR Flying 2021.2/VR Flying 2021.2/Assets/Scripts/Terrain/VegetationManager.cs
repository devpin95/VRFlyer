using System;
using System.Collections;
using System.Collections.Generic;
using DigitalRuby.WeatherMaker;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class VegetationManager : MonoBehaviour
{
    public TerrainInfo terrainInfo;
    public VegetationInfo vegetationInfo;
    public float cullDistance = 500;
    public int activeChunkCount = 0;
    
    private MeshManager _meshManager;
    private float _meshDim; // the size of the mesh after scaling
    private float _vegChunkSize; // the size of each veg chunk after scaling
    private int _vertsPerChunk; // the number of verts in a chunk (in 1 dimension)
    private GameObject _vegetationContainer;
    private bool startup = true; // used to skip clearing out chunks on before any trees have been allocated

    private WorldRepositionManager _worldRepositionManager;
    private Transform _playerTransform;
    private Vector3 _previousPlayerPosition;

    private TerrainMap _map;

    private struct ChunkInfo
    {
        public Vector3 pos;
        public int mapCenterX;
        public int mapCenterZ;
        public int mapX;
        public int mapZ;
        public bool valid;
        public bool populated;
        public GameObject chunkContainer;
    }
    
    private List<ChunkInfo> _activeChunks;

    private void Awake()
    {
        _vegetationContainer = new GameObject();
        _vegetationContainer.transform.position = Vector3.zero;
        _vegetationContainer.transform.SetParent(transform, worldPositionStays: false);
        _vegetationContainer.transform.name = "Vegetation";
    }

    // Start is called before the first frame update
    void Start()
    {
        _meshManager = GetComponent<MeshManager>();
        _meshDim = terrainInfo.meshVerts * terrainInfo.vertexScale;
        _vegChunkSize = _meshDim / vegetationInfo.vegetationGridSize;
        _vertsPerChunk = terrainInfo.meshVerts / vegetationInfo.vegetationGridSize;

        _worldRepositionManager = FindObjectOfType<WorldRepositionManager>();
        _playerTransform = FindObjectOfType<WorldRepositionManager>().playerTransform;
        _previousPlayerPosition = _playerTransform.position;

        _activeChunks = new List<ChunkInfo>();

        InitGrid();
    }

    // Update is called once per frame
    void Update()
    {
        // only recalculate the LOD when the player has traveled a certain distance
        if (Vector3.Distance(_previousPlayerPosition, _playerTransform.position) > terrainInfo.recalculateLodDistance)
        {
            _previousPlayerPosition = _playerTransform.position;
            StartCoroutine(RecalculateLODs(_playerTransform.position));
        }
    }

    private void InitGrid()
    {
        // make a grid of blocks that will hold the tree prefabs
        
        for (int z = 0; z < vegetationInfo.vegetationGridSize; ++z)
        {
            for (int x = 0; x < vegetationInfo.vegetationGridSize; ++x)
            {
                // index into map
                int mapX = x * _vertsPerChunk + _vertsPerChunk / 2;
                int mapZ = z * _vertsPerChunk + _vertsPerChunk / 2;
                
                // local position
                float xpos = x * _vegChunkSize + (_vegChunkSize / 2);
                float zpos = z * _vegChunkSize + (_vegChunkSize / 2);

                ChunkInfo chunk = new ChunkInfo();
                chunk.pos = new Vector3(xpos, 0, zpos);
                chunk.mapCenterX = mapX;
                chunk.mapCenterZ = mapZ;
                chunk.mapX = x * _vertsPerChunk;
                chunk.mapZ = z * _vertsPerChunk;
                chunk.valid = false;
                chunk.populated = false;
                
                chunk.chunkContainer = new GameObject();
                chunk.chunkContainer.transform.position = Vector3.zero;
                chunk.chunkContainer.transform.SetParent(_vegetationContainer.transform, worldPositionStays: false);
                chunk.chunkContainer.transform.localPosition = chunk.pos;
                chunk.chunkContainer.transform.name = "Vegetation chunk [" + x + ", " + z + "]";
                chunk.chunkContainer.SetActive(false);
                
                _activeChunks.Add(chunk);
            }
        }
    }

    public IEnumerator GenerateVegetation(TerrainMap map)
    {
        _map = map;

        if (!startup)
        {
            // only clear the chunks if this is NOT the first time vegetation has been generated
            ClearChunks();
            startup = false;
        }

        ValidateChunkHeights(); // determine which chunks we can populate
        yield return RecalculateLODs(_playerTransform.position); // find the chunks close enough and populate them with trees
    }

    private void ClearChunks()
    {
        // remove all of the trees from this chunk
        for (int i = 0; i < _activeChunks.Count; ++i)
        {
            StartCoroutine(DeallocateTrees(_activeChunks[i]));
        }
    }

    IEnumerator DeallocateTrees(ChunkInfo chunk)
    {
        if (!chunk.populated) yield break;
        
        // loop through all of the children in the container and relinquish them back to the pool
        // TODO do this in chunks in a coroutine so that it doesnt block the main thread too long
        int treeCount = 0;
        for (int child = chunk.chunkContainer.transform.childCount - 1; child >= 0; --child)
        {
            var tree = chunk.chunkContainer.transform.GetChild(child);
            TreePool.Instance.RelinquishTreeInstance(tree.gameObject);

            ++treeCount;
            if (treeCount > vegetationInfo.deallocationBatchSize)
            {
                treeCount = 0;
                yield return new WaitForSeconds(vegetationInfo.deallocationDelay);
            }
        }
                
        chunk.populated = false;
    }

    IEnumerator AllocateTrees(ChunkInfo chunk)
    {
        // only generate trees for valid chunks
        if (!chunk.valid) yield break;
        
        // the range of x and z values in the terrain map for this vegetation chunk
        Vector2 xBounds = new Vector2(chunk.mapX, chunk.mapX + _vertsPerChunk);
        Vector2 zBounds = new Vector2(chunk.mapZ, chunk.mapZ + _vertsPerChunk);

        int treeCount = 0;
        
        // create the required number of trees
        for (int i = 0; i < vegetationInfo.treeCountPerGrid; ++i)
        {
            Vector3 pos = Vector3.one;
            float x = 1; // random x position
            float z = 1; // random z position
            float y = 2; // y position [0, 1] at (x, z)
            float yScaled = 0; // scaled y position [remapMin, remapMax] at (x, z)

            // randomly pick an (x, z) in this chunk then sample the height map for y
            x = UnityEngine.Random.Range(xBounds.x, xBounds.y);
            z = UnityEngine.Random.Range(zBounds.x, zBounds.y);
                
            // sample the height map for the raw height and scaled height
            // x [0,1], y [remapmin, remapmax]
            Vector2 ySamples = _map.SampleAndScaleHeightMap(x, z, terrainInfo.terrainCurve, terrainInfo.remapMin, terrainInfo.remapMax);
            y = ySamples.x;
            yScaled = ySamples.y;

            // check if the height is outside the cutoff range 
            // or if the height is somehow NaN or infinity to prevent an error
            if (y > vegetationInfo.treeHeightCutoff || float.IsNaN(yScaled) || float.IsInfinity(yScaled)) continue;

            // make a new vector3 for the tree's position
            pos = new Vector3(x * terrainInfo.vertexScale, yScaled, z * terrainInfo.vertexScale);

            // GameObject tree = Instantiate(vegetationInfo.tree1Prefab, Vector3.zero, Quaternion.identity, chunk.chunkContainer.transform);
            GameObject tree = TreePool.Instance.RequestTreeInstance(chunk.chunkContainer.transform);

            // make sure the pool actually returned an tree object
            if (tree != null)
            {
                // set the position of the tree
                tree.transform.position = pos + transform.position - tree.transform.Find("Attach Point").transform.localPosition;
            }

            ++treeCount;

            if (treeCount > vegetationInfo.allocationBatchSize)
            {
                treeCount = 0;
                yield return new WaitForSeconds(vegetationInfo.allocationDelay);
            }
        }
    }
    
    private void ValidateChunkHeights()
    {
        // loop through the chunks and deactivate those that are too high
        for (int i = 0; i < _activeChunks.Count; ++i)
        {
            var chunk = _activeChunks[i];
            
            // true if the height of the chunk is <= heightCutoff
            chunk.valid = _map.SampleHeightMap(chunk.mapX, chunk.mapZ) <= vegetationInfo.chunkHeightCutoff;

            _activeChunks[i] = chunk;
        }
    }

    IEnumerator RecalculateLODs(Vector3 pos)
    {
        for (int i = 0; i < _activeChunks.Count; ++i)
        {
            // if the chunk is not valid, ignore it
            if (!_activeChunks[i].valid) continue;
            
            ChunkInfo targetChunk = _activeChunks[i];
            
            // generate bounds for the chunk
            Bounds bound = new Bounds();
            bound.center = transform.position + targetChunk.pos;
            bound.size = new Vector3(_vegChunkSize, _vegChunkSize / 2, _vegChunkSize);

            // get the distance between the player and the bounds for this chunk
            float dis = Mathf.Sqrt(bound.SqrDistance(pos));

            // if the chunk is further away from the player than the cull distance
            float cull = vegetationInfo.defaultCullDistance;
            
            // if (_worldRepositionManager.playerWorldPos.y > vegetationInfo.lowAltitudeCutoff)
            //     cull = vegetationInfo.lowAltitudeCullDistance;
            
            if (dis > cull)
            {
                // then we are going to deactivate this chunk, so if it was active, we can deallocate all of it's trees
                if (targetChunk.chunkContainer.activeSelf)
                {
                    // deallocate trees
                    yield return DeallocateTrees(targetChunk);
                    targetChunk.populated = false;
                }
                
                targetChunk.chunkContainer.SetActive(false);
            }
            // otherwise, if the chunk is closer than the cull distance
            else
            {
                // then we are going to activate this chunk, so if it was unactive, we need to allocate all of it's trees
                if (!targetChunk.chunkContainer.activeSelf)
                {
                    // allocate trees
                    yield return AllocateTrees(targetChunk);
                    targetChunk.populated = true;
                }
                targetChunk.chunkContainer.SetActive(true);
            }

            _activeChunks[i] = targetChunk;
        }

        yield return null;
    }

    public void SpawnPointResponse(Vector3 pos)
    {
        RecalculateLODs(pos);
    }

    private void GenerateTrees()
    {
        foreach (var chunk in _activeChunks)
        {
            AllocateTrees(chunk);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_map == null) return;
        
        foreach (var chunk in _activeChunks)
        {
            if (!chunk.valid) continue;
            
            if ( chunk.chunkContainer.activeSelf ) Gizmos.color = Color.blue;
            else Gizmos.color = Color.red;
            
            Gizmos.DrawWireCube(transform.position + chunk.pos, new Vector3(_vegChunkSize, _vegChunkSize / 2, _vegChunkSize));
        }
    }
}
