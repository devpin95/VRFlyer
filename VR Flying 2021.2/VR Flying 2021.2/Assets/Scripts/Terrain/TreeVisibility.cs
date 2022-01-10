using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeVisibility : MonoBehaviour
{
    public GameObject player;
    public float renderDistance;

    private SpriteRenderer _meshRenderer;
        
    // Start is called before the first frame update
    void Start()
    {
        _meshRenderer = GetComponent<SpriteRenderer>();
        _meshRenderer.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if ( Vector3.Distance(player.transform.position, transform.position) < renderDistance )
        {
            _meshRenderer.enabled = true;
        }
        else
        {
            _meshRenderer.enabled = false;
        }
    }
}
