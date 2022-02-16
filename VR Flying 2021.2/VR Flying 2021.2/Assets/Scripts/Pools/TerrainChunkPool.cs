using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainChunkPool : MonoBehaviour
{
    public GameObject terrainChunkPrefab;
    
    [Range(1, 100)]
    public int poolSize = 1;
    private List<GameObject> chunks;

    public static TerrainChunkPool Instance = null;

    private void Awake()
    {
        if (Instance == null)
        {
            int gridSize = (FindObjectOfType<TerrainManager>().viewDistance * 2) + 1;
            poolSize = gridSize * gridSize;
            
            Debug.Log("Terrain chunk pool (" + poolSize + ")" );
            
            Instance = this;
            chunks = new List<GameObject>();
            int id = 0;
            for (int i = 0; i < poolSize; ++i)
            {
                GameObject chunk = Instantiate(terrainChunkPrefab, parent:transform);
                chunk.SetActive(false);
                chunk.name = "Terrain Chunk Prefab (" + id + ")";
                chunks.Add(chunk);

                ++id;
            }
        }
        else Destroy(this);
    }

    public GameObject RequestMeshChunkInstance(Transform newParent)
    {
        foreach (var chunk in chunks)
        {
            if (!chunk.activeSelf)
            {
                chunk.SetActive(true);
                chunk.transform.SetParent(newParent);
                return chunk;
            }
        }

        return null;
    }

    public void RelinquishMeshChunkInstance(GameObject chunk)
    {
        if (!chunks.Contains(chunk)) return; // if an object not in the pool got passed in, just ignore it
        
        // set the chunk parent back to the pool empty and disable the object
        chunk.transform.SetParent(transform);
        chunk.SetActive(false);
    }
}
