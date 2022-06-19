using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasRenderer))] 
public class TrailController : MonoBehaviour
{
    public MultiUILineRenderer multiLineRenderer;
    public GPSController gpsController;

    private Vector2 mapPosition;
    private Vector3 startingWorldPosition;
    private Vector3 worldPosition;

    private float timeCounter = 0;
    
    private WorldRepositionManager _worldRepositionManager;
    
    // Start is called before the first frame update
    void Start()
    {
        _worldRepositionManager = FindObjectOfType<WorldRepositionManager>();
        startingWorldPosition = _worldRepositionManager.playerWorldPos;
        multiLineRenderer.trailCapacity = gpsController.gpsInfo.checkpointCount;
        multiLineRenderer.thickness = gpsController.gpsInfo.trailLineWidth;
    }

    // Update is called once per frame
    void Update()
    {
        timeCounter += Time.deltaTime;
        if (timeCounter < gpsController.gpsInfo.updateTime) return;
        
        // reset the time counter
        timeCounter = 0;
        
        worldPosition = _worldRepositionManager.playerWorldPos;
        mapPosition = gpsController.WorldSpaceToMapSpace(worldPosition);

        if (Vector2.Distance(worldPosition.Flatten(), startingWorldPosition.Flatten()) >= gpsController.gpsInfo.distanceBetweenTrailPoints)
        {
            multiLineRenderer.AddPosition(mapPosition);
        }

        multiLineRenderer.anchorPosition = mapPosition;
        multiLineRenderer.ForceUpdate();
    }
}
