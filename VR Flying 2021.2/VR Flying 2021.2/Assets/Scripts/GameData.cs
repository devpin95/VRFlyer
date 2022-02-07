using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameData : ScriptableObject
{
    [SerializeField] private bool _dataReady = false;
    [SerializeField] private int _seed = 0;
    [SerializeField] private Vector2 _perlinOffset = Vector2.zero;
    
    public int Seed
    {
        get => _seed;
        set => _seed = value;
    }

    public Vector2 PerlinOffset
    {
        get => _perlinOffset;
        set => _perlinOffset = value;
    }

    public bool DataReady
    {
        get => _dataReady;
        set => _dataReady = value;
    }

    public List<GameObject> structureList = new List<GameObject>();
}
