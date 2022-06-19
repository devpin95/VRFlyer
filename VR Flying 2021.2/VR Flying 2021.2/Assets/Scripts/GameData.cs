using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameData : ScriptableObject
{
    [SerializeField] private bool _dataReady = false;
    [SerializeField] private int _seed = 0;
    [SerializeField] private IntVector2 _perlinOffset = IntVector2.zero;
    [SerializeField] private IntVector2 _playerStartingOffset = IntVector2.zero;

    [Header("Important Structures")] 
    public GameObject helipadPrefab;

    public int Seed
    {
        get => _seed;
        set => _seed = value;
    }

    public IntVector2 PerlinOffset
    {
        get => _perlinOffset;
        set => _perlinOffset = value;
    }

    public bool DataReady
    {
        get => _dataReady;
        set => _dataReady = value;
    }

    public IntVector2 PlayerStartingOffset
    {
        get => _playerStartingOffset;
        set => _playerStartingOffset = value;
    }

    [Header("Structures")]
    public List<GameObject> structureList = new List<GameObject>();

    [Header("Modes")] 
    public bool recordingSession = false;
}
