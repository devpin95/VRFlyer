using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterBodyPool : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject waterPrefab;
    
    public int poolSize = 1;
    private List<GameObject> waterBodies; // we will save these in a list so that we can prevent the objects from being GCed
    private Queue<GameObject> _waterBodyQ; // a queue so that we can quickly get the next object out

    public static WaterBodyPool Instance = null;

    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log("Water Body pool (" + poolSize + ")");

            Instance = this;
            waterBodies = new List<GameObject>();
            _waterBodyQ = new Queue<GameObject>();

            for (int i = 0; i < poolSize; ++i)
            {
                GameObject body = Instantiate(waterPrefab, parent: transform);
                body.name = "Water Body Prefab (" + i + ")";

                waterBodies.Add(body);
                _waterBodyQ.Enqueue(body);
                body.SetActive(false);
            }
        }
        else Destroy(this);
    }
    
    public GameObject RequestWaterBodyInstance(Transform newParent)
    {
        GameObject body = null;
        try
        {
            body = _waterBodyQ.Dequeue();
            body.SetActive(true);
            body.transform.SetParent(newParent);
        }
        catch (InvalidOperationException e)
        {
            Debug.LogWarning(newParent.name + " tried requesting a tree but there are none left. ");
        }

        return body;
    }

    public void RelinquishWaterBodyInstance(GameObject body)
    {
        // if (!trees.Contains(tree)) return; // if an object not in the pool got passed in, just ignore it
        
        // set the chunk parent back to the pool empty and disable the object
        body.transform.SetParent(transform);
        body.SetActive(false);
        _waterBodyQ.Enqueue(body);
    }
}
