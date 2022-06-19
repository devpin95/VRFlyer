using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public GameData gameData;

    public bool gameDebug = false;
    [FormerlySerializedAs("useSavedRNGState")] public bool useSavedRngState = false;

    private Random.State _rngState;
    
    // Start is called before the first frame update
    void Awake()
    {
        if (useSavedRngState)
        {
            Random.state = _rngState;
            Debug.LogWarning("USING PREVIOUS RNG STATE!");
        }
        else _rngState = Random.state;

        // generate a seed for every object to use
        gameData.Seed = Random.Range(int.MinValue, int.MaxValue);

        // generate a random starting coordinate for the terrain because perlin noise is mirrored over the x/z axis
        gameData.PerlinOffset = new IntVector2(Random.Range(50, 1000), Random.Range(50, 1000));

        gameData.DataReady = true;
    }

    private void Start()
    {
        if (gameData.recordingSession)
        {
            FindObjectOfType<RecordingManager>().SetInitialGameState();
        }
    }

    private void OnApplicationQuit()
    {
        gameData.DataReady = false;
    }

    public void TerrainReadyResponse()
    {
        Debug.Log("The terrain is ready, no we can do something else!");
    }

    public int RequestSeed()
    {
        return Random.Range(int.MinValue, int.MaxValue);
    }
}
