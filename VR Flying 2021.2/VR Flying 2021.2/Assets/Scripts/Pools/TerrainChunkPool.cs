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
            Instance = this;
            chunks = new List<GameObject>();
            for (int i = 0; i < poolSize; ++i)
            {
                GameObject chunk = Instantiate(terrainChunkPrefab);
                chunk.SetActive(false);
                chunks.Add(chunk);
            }
        }
        else Destroy(this);
    }

    public GameObject RequestMeshChunkInstance()
    {
        foreach (var chunk in chunks)
        {
            if (!chunk.activeSelf)
            {
                chunk.SetActive(true);
                return chunk;
            }
        }

        return null;
    } 
}
