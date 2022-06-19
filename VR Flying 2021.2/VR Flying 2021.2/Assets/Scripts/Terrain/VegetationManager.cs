using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DigitalRuby.WeatherMaker;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class VegetationManager : MonoBehaviour
{
    public TerrainInfo terrainInfo;
    public VegetationInfo vegetationInfo;
    private ProceduralSeed _procSeed;
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
        public Bounds bounds;
    }

    private struct TreeAnimationInfo
    {
        public float animationTime;
        public Transform transform;
        public Vector3 startingPos;
        public Vector3 targetPos;
    }
    
    private List<ChunkInfo> _chunks;
    private List<ChunkInfo> _validChunks;

    private bool _firstGeneration = true;

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

        _chunks = new List<ChunkInfo>();
        _validChunks = new List<ChunkInfo>();

        _procSeed = GetComponent<ProceduralSeed>();

        InitGrid();
    }

    // Update is called once per frame
    void Update()
    {
        // only recalculate the LOD when the player has traveled a certain distance
        if (Vector3.Distance(_previousPlayerPosition, _playerTransform.position) > vegetationInfo.recalculateLodDistance)
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
                
                // generate bounds for the chunk
                Bounds bound = new Bounds();
                bound.center = transform.position + chunk.pos;
                bound.size = new Vector3(_vegChunkSize, _vegChunkSize / 2, _vegChunkSize);
                chunk.bounds = bound;
                
                _chunks.Add(chunk);
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

        yield return ValidateChunkHeights(); // determine which chunks we can populate
        yield return RecalculateLODs(_playerTransform.position); // find the chunks close enough and populate them with trees

        _firstGeneration = false;
    }

    private void ClearChunks()
    {
        // remove all of the trees from this chunk
        for (int i = 0; i < _validChunks.Count; ++i)
        {
            StartCoroutine(DeallocateTrees(_validChunks[i]));
        }
    }

    IEnumerator DeallocateTrees(ChunkInfo chunk)
    {
        if (!chunk.populated && chunk.chunkContainer.transform.childCount == 0) yield break;
        
        // loop through all of the children in the container and relinquish them back to the pool
        // TODO do this in chunks in a coroutine so that it doesnt block the main thread too long
        int treeCount = 0;
        for (int child = chunk.chunkContainer.transform.childCount - 1; child >= 0; --child)
        {
            if (child >= chunk.chunkContainer.transform.childCount) break; // idk just skip it if this happens I guess
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
        // if (!chunk.valid) yield break;
        
        // the range of x and z values in the terrain map for this vegetation chunk
        Vector2 xBounds = new Vector2(chunk.mapX, chunk.mapX + _vertsPerChunk);
        Vector2 zBounds = new Vector2(chunk.mapZ, chunk.mapZ + _vertsPerChunk);

        int treeCount = 0;

        List<TreeAnimationInfo> treeTransforms = new List<TreeAnimationInfo>();
        
        // seed the rng so that we generate the same thing each time
        _procSeed.InitState((int)xBounds.x + (int)zBounds.x + (int)xBounds.y + (int)zBounds.y + (int)xBounds.x % 3 + (int)zBounds.y % 6);
        
        // create the required number of trees
        for (int i = 0; i < vegetationInfo.treeCountPerGrid; ++i)
        {
            Vector3 pos = Vector3.one;
            float x = 1; // random x position
            float z = 1; // random z position
            float y = 2; // y position [0, 1] at (x, z)
            float yScaled = 0; // scaled y position [remapMin, remapMax] at (x, z)

            // randomly pick an (x, z) in this chunk then sample the height map for y
            x = _procSeed.Range(xBounds.x, xBounds.y);
            z = _procSeed.Range(zBounds.x, zBounds.y);
            // x = UnityEngine.Random.Range(xBounds.x, xBounds.y);
            // z = UnityEngine.Random.Range(zBounds.x, zBounds.y);
                
            // sample the height map for the raw height and scaled height
            // x [0,1], y [remapmin, remapmax]
            Vector2 ySamples = _map.SampleAndScaleHeightMap(x, z, 0, terrainInfo.MaxTerrainHeight());
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
                // set the position of the tree to the start position under the map so that we can animate it up
                tree.transform.position = pos + transform.position - tree.transform.Find("Attach Point").transform.localPosition;
                Vector3 localEndPosition = tree.transform.localPosition;
                
                if (!_firstGeneration) tree.transform.localPosition -= new Vector3(0, 10, 0);

                // create a new animation object for sprouting the tree
                TreeAnimationInfo animationInfo;
                animationInfo.animationTime = 0;
                animationInfo.transform = tree.transform;
                animationInfo.startingPos = tree.transform.localPosition;
                animationInfo.targetPos = localEndPosition;
                treeTransforms.Add(animationInfo);
            }

            ++treeCount;

            if (treeCount > vegetationInfo.allocationBatchSize && !_firstGeneration)
            {
                treeCount = 0;
                yield return new WaitForSeconds(vegetationInfo.allocationDelay);
            }
        }
        
        if (!_firstGeneration) yield return SproutVegetation(treeTransforms);
    }
    
    IEnumerator ValidateChunkHeights()
    {
        _validChunks = new List<ChunkInfo>();

        int batch = 0;
        // loop through the chunks and deactivate those that are too high
        for (int i = 0; i < _chunks.Count; ++i)
        {
            var chunk = _chunks[i];
            
            // true if the height of the chunk is <= heightCutoff
            float chunkHeight = _map.SampleAndScaleHeightMap(chunk.mapX, chunk.mapZ, 0, terrainInfo.MaxTerrainHeight());
            chunk.pos = new Vector3(chunk.pos.x, chunkHeight, chunk.pos.z);
            chunk.valid = _map.SampleHeightMap(chunk.mapX, chunk.mapZ) <= vegetationInfo.chunkHeightCutoff;
            
            if ( chunk.valid ) _validChunks.Add(chunk);

            _chunks[i] = chunk;

            ++batch;
            if (batch == 250 && !_firstGeneration)
            {
                batch = 0;
                yield return null;
            }
        }
    }

    IEnumerator RecalculateLODs(Vector3 pos)
    {
        // yield return SortValidChunks(pos);
        
        // Debug.Log(_validChunks.Count + " Sorted chunks");
        
        for (int i = 0; i < _validChunks.Count; ++i)
        {
            // if the chunk is not valid, ignore it
            // if (!_validChunks[i].valid) continue;
            
            ChunkInfo targetChunk = _validChunks[i];
            
            // // generate bounds for the chunk
            // Bounds bound = new Bounds();
            // bound.center = transform.position + targetChunk.pos;
            // bound.size = new Vector3(_vegChunkSize, _vegChunkSize / 2, _vegChunkSize);
            
            Bounds bound = new Bounds();
            bound.center = transform.position + targetChunk.pos;
            bound.size = new Vector3(_vegChunkSize, _vegChunkSize / 2, _vegChunkSize);

            // get the distance between the player and the bounds for this chunk
            float dis = Mathf.Sqrt(bound.SqrDistance(pos));

            // if (_worldRepositionManager.playerWorldPos.y > vegetationInfo.lowAltitudeCutoff)
            //     cull = vegetationInfo.lowAltitudeCullDistance;
            
            bool previouslyActive = targetChunk.chunkContainer.activeSelf;
            
            if (dis > vegetationInfo.defaultCullDistance)
            {
                targetChunk.chunkContainer.SetActive(false);
                
                // then we are going to deactivate this chunk, so if it was active, we can deallocate all of it's trees
                if (previouslyActive)
                {
                    // deallocate trees
                    Debug.Log("Removing trees for " + targetChunk.chunkContainer.transform.name);
                    yield return DeallocateTrees(targetChunk);
                    targetChunk.populated = false;
                }
            }
            // otherwise, if the chunk is closer than the cull distance
            else
            {
                // copy the current state then set to active so that we can tell if we went from unactive to active
                // then set the state to active before allocating trees so that we can animate the trees up over time
                // instead of instantly placing them
                targetChunk.chunkContainer.SetActive(true);
                // then we are going to activate this chunk, so if it was unactive, we need to allocate all of it's trees
                if (!previouslyActive)
                {
                    // allocate trees
                    // Debug.Log("Allocating trees for " + targetChunk.chunkContainer.transform.name);
                    yield return AllocateTrees(targetChunk);
                    targetChunk.populated = true;
                }
            }

            _validChunks[i] = targetChunk;
        }

        yield return null;
    }

    IEnumerator SortValidChunks(Vector3 pos)
    {
        List<Tuple<ChunkInfo, float>> orderedChunkList = new List<Tuple<ChunkInfo, float>>();
        int batch = 0;
        
        for (int i = 0; i < _validChunks.Count; ++i)
        {
            // loop through each valid chunk and calculate it's distance
            float dis = Mathf.Sqrt(_validChunks[i].bounds.SqrDistance(pos));
            var chunk = new Tuple<ChunkInfo, float>(_validChunks[i], dis);

            bool inserted = false;
            // loop through the items in the ordered list until we find an item whose distance
            // is less that the current chunks distance. If we find one, insert the current item at that index
            for (int j = 0; j < orderedChunkList.Count; ++j)
            {
                if (dis < orderedChunkList[j].Item2)
                {
                    orderedChunkList.Insert(j, chunk);
                    inserted = true;
                    break;
                }
            }
            
            // if there are no items in the ordered list, or if this chunk is the furthest chunk so far, just add
            // it at the end of the list
            if (!inserted)
            {
                orderedChunkList.Add(chunk);
            }

            ++batch;
            if (batch > 10 && !_firstGeneration)
            {
                batch = 0;
                yield return null;
            }
        }

        // now extract all of the ChunkInfo items and put them back into the _validChunks list, which is now sorted
        _validChunks = orderedChunkList.Select(chunk => chunk.Item1).ToList();
        
        yield return null;
    }
    
    IEnumerator SproutVegetation(List<TreeAnimationInfo> animations)
    {
        if (animations.Count == 0) yield break;
        
        while (animations[0].animationTime / vegetationInfo.sproutAnimationTime < 1)
        {
            // loop through the tree animation in batches
            int batch = 0;
            for (int i = 0; i < animations.Count; ++i)
            {
                // copy out the tree animation and increment its animation time
                var treeAnimation = animations[i];
                treeAnimation.animationTime += Time.deltaTime;

                // set the tree's local position to the lerp of it's start/ending positions
                treeAnimation.transform.localPosition = Vector3.Lerp(treeAnimation.startingPos, treeAnimation.targetPos,
                    animations[0].animationTime / vegetationInfo.sproutAnimationTime);

                // copy back the animation info
                animations[i] = treeAnimation;

                // increment the batch count and stop for this frame if we reached out batch size
                ++batch;
                if (batch > vegetationInfo.sproutAnimationBatchSize)
                {
                    batch = 0;
                    yield return null;
                }
            }
            
            // we have gone through the animations one more time, now wait for a certain length of time
            // and continue the animations if the animation time has not reached the target duration
            yield return new WaitForSeconds(vegetationInfo.sproutAnimationDelay);
        }
    }

    public void SpawnPointResponse(Vector3 pos)
    {
        RecalculateLODs(pos);
    }

    private void GenerateTrees()
    {
        foreach (var chunk in _chunks)
        {
            AllocateTrees(chunk);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_map == null) return;
        
        foreach (var chunk in _chunks)
        {
            if (!chunk.valid) continue;
            
            if ( chunk.chunkContainer.activeSelf ) Gizmos.color = Color.blue;
            else Gizmos.color = Color.red;
            
            Gizmos.DrawWireCube(transform.position + chunk.pos, new Vector3(_vegChunkSize, _vegChunkSize / 2, _vegChunkSize));
        }
    }
}
