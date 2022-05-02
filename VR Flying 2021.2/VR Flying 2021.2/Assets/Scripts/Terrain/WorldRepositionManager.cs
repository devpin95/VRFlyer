using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class WorldRepositionManager : MonoBehaviour
{
    public TerrainInfo terrainInfo;
    
    [FormerlySerializedAs("travelDistanceThreshold")] public float repositionDistance = 5000;
    public Transform playerTransform;
    
    public CEvent_Vector3 worldRepositionEvent;
    
    [FormerlySerializedAs("worldPos")] [Header("World Position")]
    public Vector3 playerWorldPos = Vector3.zero;
    // public Vector2 gridPos = Vector2.zero;
    // public Vector4 gridQuad = Vector4.zero;
    // public Vector4 currentGridQuad = Vector4.zero;

    // [Header("Grid Objects")] 
    // public List<MeshManager> terrainBlocks = new List<MeshManager>();
    
    [Header("Offsets")]
    [SerializeField] public Vector3 majorOffset = Vector3.zero;
    [SerializeField] public float distanceFromOrigin = 0;

    private float gridsize = 255 * 100;

    private Vector3 _playerOldPosition;

    // Start is called before the first frame update
    void Start()
    {
        playerWorldPos = playerTransform.position;
        majorOffset.y += terrainInfo.baseAltitude;
        distanceFromOrigin += Vector3.Distance(playerTransform.position, Vector3.zero);
    }

    // Update is called once per frame
    void Update()
    {
        distanceFromOrigin = Vector3.Distance(playerTransform.position, Vector3.zero);
        playerWorldPos = playerTransform.position + majorOffset;

        bool distanceThresholdReached = Vector3.Distance(playerTransform.position, Vector3.zero) > repositionDistance;
        if (distanceThresholdReached) Reposition();
    }

    public void Reposition()
    {
        Debug.Log("Resetting position");
        float dis = Vector3.Distance(playerTransform.position, Vector3.zero);
        distanceFromOrigin += dis;
        majorOffset += playerTransform.position;
        Vector3 offset = -playerTransform.position;
        worldRepositionEvent.Raise(offset);
    }
}
