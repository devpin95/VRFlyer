using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldRepositionManager : MonoBehaviour
{
    public float repositionDistance = 5000;
    public Transform playerTransform;
    
    public CEvent_Vector3 worldRepositionEvent;
    
    public Vector3 worldPos = Vector3.zero;
    [SerializeField] public Vector3 majorOffset = Vector3.zero;
    [SerializeField] public float distanceFromOrigin = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        worldPos = playerTransform.position;
        distanceFromOrigin += Vector3.Distance(playerTransform.position, Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        worldPos = playerTransform.position + majorOffset;
        
        float dis = Vector3.Distance(playerTransform.position, Vector3.zero);
        if (dis > repositionDistance)
        {
            distanceFromOrigin += dis;
            majorOffset += playerTransform.position;
            Vector3 offset = -playerTransform.position;
            worldRepositionEvent.Raise(offset);
        }
    }
}
