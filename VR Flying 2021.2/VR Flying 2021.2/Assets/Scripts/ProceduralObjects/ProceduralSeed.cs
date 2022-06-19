using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralSeed : MonoBehaviour
{
    [SerializeField] private int seed;

    public int Seed => seed;
    
    // Start is called before the first frame update
    void Start()
    {
        seed = FindObjectOfType<GameManager>().RequestSeed();
    }

    public void InitState()
    {
        Random.InitState(Seed);
    }
    
    public void InitState(int salt)
    {
        Random.InitState(Seed + salt);
    }

    public float Range(float lower, float upper)
    {
        return Random.Range(lower, upper);
    }
    
    public float RangeSingle(float lower, float upper, int seed)
    {
        InitState();
        return Random.Range(lower, upper);
    }
    
    public float Range(int lower, int upper)
    {
        return Random.Range(lower, upper);
    }
    
    public float RangeSingle(int lower, int upper, int seed)
    {
        InitState();
        return Random.Range(lower, upper);
    }
}
