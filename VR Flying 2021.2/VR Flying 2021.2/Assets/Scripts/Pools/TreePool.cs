using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TreePool : MonoBehaviour
{
    public VegetationInfo vegetationInfo;
    
    [Header("Prefabs")]
    public GameObject treePrefab;
    
    public int poolSize = 1;
    private List<GameObject> trees; // we will save these in a list so that we can prevent the objects from being GCed
    private Queue<GameObject> _treeQ; // a queue so that we can quickly get the next object out

    public static TreePool Instance = null;

    private void Awake()
    {
        if (Instance == null)
        {
            poolSize = vegetationInfo.treePoolSize;
            Debug.Log("Tree pool (" + poolSize + ")" );
            
            Instance = this;
            trees = new List<GameObject>();
            _treeQ = new Queue<GameObject>();
            
            for (int i = 0; i < poolSize; ++i)
            {
                GameObject tree = Instantiate(treePrefab, parent:transform);
                tree.name = "Tree Prefab (" + i + ")";
                
                // random scale and rotation
                float scale = UnityEngine.Random.Range(vegetationInfo.treeScaleRange.x, vegetationInfo.treeScaleRange.y);
                tree.transform.localScale = new Vector3(scale, scale, scale);
                tree.transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 360f), 0);

                trees.Add(tree);
                _treeQ.Enqueue(tree);
                tree.SetActive(false);
            }
        }
        else Destroy(this);
    }

    public GameObject RequestTreeInstance(Transform newParent)
    {
        GameObject tree = null;
        try
        {
            tree = _treeQ.Dequeue();
            tree.SetActive(true);
            tree.transform.SetParent(newParent);
        }
        catch (InvalidOperationException e)
        {
            Debug.LogWarning(newParent.name + " tried requesting a tree but there are none left. ");
        }

        return tree;
    }

    public void RelinquishTreeInstance(GameObject tree)
    {
        // if (!trees.Contains(tree)) return; // if an object not in the pool got passed in, just ignore it
        
        // set the chunk parent back to the pool empty and disable the object
        tree.transform.SetParent(transform);
        tree.SetActive(false);
        _treeQ.Enqueue(tree);
    }
}