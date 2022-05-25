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
    private List<GameObject> trees;

    public static TreePool Instance = null;

    private void Awake()
    {
        if (Instance == null)
        {
            poolSize = vegetationInfo.treePoolSize;
            Debug.Log("Tree pool (" + poolSize + ")" );
            
            Instance = this;
            trees = new List<GameObject>();
            
            for (int i = 0; i < poolSize; ++i)
            {
                GameObject tree = Instantiate(treePrefab, parent:transform);
                tree.name = "Tree Prefab (" + i + ")";
                
                float scale = UnityEngine.Random.Range(vegetationInfo.treeScaleRange.x, vegetationInfo.treeScaleRange.y);
                tree.transform.localScale = new Vector3(scale, scale, scale);
                tree.transform.eulerAngles = new Vector3(0, UnityEngine.Random.Range(0, 360f), 0);

                trees.Add(tree);
                tree.SetActive(false);
            }
        }
        else Destroy(this);
    }

    public GameObject RequestTreeInstance(Transform newParent)
    {
        foreach (var tree in trees)
        {
            if (!tree.activeSelf)
            {
                tree.SetActive(true);
                tree.transform.SetParent(newParent);
                return tree;
            }
        }

        return null;
    }

    public void RelinquishTreeInstance(GameObject tree)
    {
        if (!trees.Contains(tree)) return; // if an object not in the pool got passed in, just ignore it
        
        // set the chunk parent back to the pool empty and disable the object
        tree.transform.SetParent(transform);
        tree.SetActive(false);
    }
}