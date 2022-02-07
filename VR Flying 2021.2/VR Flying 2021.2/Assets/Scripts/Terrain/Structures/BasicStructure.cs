using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicStructure : Structure
{
    public LayerMask structurePlacementMask;
    public override void PlantStructure()
    {
        // a simple ray cast down at the terrain to find the point the structure should be placed at
        RaycastHit hit;
        bool coll = Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity, structurePlacementMask);

        if (coll)
        {
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }
        else
        {
            Debug.Log("bye bye");
            Destroy(gameObject);
        }
    }
}
