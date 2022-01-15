using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldRepositionManager : MonoBehaviour
{
    public float repositionDistance = 5000;
    public Transform playerTransform;
    
    public CEvent_Vector3 worldRepositionEvent;
    
    public Vector3 worldPos = Vector3.zero;
    public Vector2 gridPos = Vector2.zero;
    public Vector4 gridQuad = Vector4.zero;
    [SerializeField] public Vector3 majorOffset = Vector3.zero;
    [SerializeField] public float distanceFromOrigin = 0;

    private float gridsize = 255 * 50;
    
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
        float absx = ((gridsize / 2) - worldPos.x) / gridsize * ( worldPos.x < 0 ? -1 : 1 );
        float absz = ((gridsize / 2) - worldPos.z) / gridsize * ( worldPos.z < 0 ? -1 : 1 );
        
        gridPos = new Vector2((int)absx, (int)absz);

        // float dis = Vector3.Distance(playerTransform.position, Vector3.zero);
        // if (dis > repositionDistance)
        // {
        //     distanceFromOrigin += dis;
        //     majorOffset += playerTransform.position;
        //     Vector3 offset = -playerTransform.position;
        //     worldRepositionEvent.Raise(offset);
        // }
    }

    public void Reposition()
    {
        float dis = Vector3.Distance(playerTransform.position, Vector3.zero);
        distanceFromOrigin += dis;
        majorOffset += playerTransform.position;
        Vector3 offset = -playerTransform.position;
        worldRepositionEvent.Raise(offset);
    }
}
