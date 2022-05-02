using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public GameData gameData;
    
    // Start is called before the first frame update
    void Awake()
    {
        // generate a seed for every object to use
        gameData.Seed = Random.Range(int.MinValue, int.MaxValue);
        
        Random.InitState(gameData.Seed);
        
        // generate a random starting coordinate for the terrain because perlin noise is mirrored over the x/z axis
        gameData.PerlinOffset = new Vector2(Random.Range(50, 1000), Random.Range(50, 1000));

        gameData.DataReady = true;
    }

    private void OnApplicationQuit()
    {
        gameData.DataReady = false;
    }

    public void TerrainReadyResponse()
    {
        Debug.Log("The terrain is ready, no we can do something else!");
    }
}
