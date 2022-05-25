using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// for making polygons w/ fill
// https://forum.unity.com/threads/spriteshape-preview-package.522575/#post-3441363

public class GPSController : MonoBehaviour
{
    [Header("GUI")] 
    public GPSGUI guiElements;
    public TerrainInfo terrainInfo;

    [Header("Objects")] 
    public RectTransform containerTrans;
    public GameObject iconArea;
    private RectTransform iconAreaRecTrans;
    public GameObject pathArea;
    private RectTransform pathAreaRecTrans;
    public GameObject loadingOverlay;
    public Transform positionOverlayIcon;
    
    [Header("Destination Overlay")]
    public GameObject destinationOverlay;
    public TextMeshProUGUI destinationName;
    public TextMeshProUGUI destinationDistance;
    public TextMeshProUGUI destinationETA;

    [Header("Map Control")]
    public GameObject mapArea;
    private RectTransform mapAreaRecTrans;
    public GameObject imagePrefab;
    
    [Header("Control")]
    public InstrumentInput instrumentInputs;
    [Range(0.5f, 10)]
    public float zoomScale = 1;
    public bool update = true;
    public Vector3 previousWorldPosition;
    public Vector3 worldPosition;
    public Vector3 mapPosition;
    private Vector2 containerSizeDelta;
    private float worldScale;
    public float updateTime = 10f;
    private float timeCounter = 0;

    [Header("Path Lines")] 
    public SimpleUILineRenderer targetPathLine;
    public Vector3 targetLocation;
    private GPSGUI.GPSDestination _targetDestination;
    private bool _targetDestinationSet = false;
    
    [Header("Opacity group 1")]
    [Range(0.25f, 1)]
    public float group1Opacity = 1;
    public CanvasGroup group1;

    private WorldRepositionManager worldRepositionManager;

    // Start is called before the first frame update
    void Start()
    {
        worldRepositionManager = FindObjectOfType<WorldRepositionManager>();
        mapAreaRecTrans = mapArea.GetComponent<RectTransform>();
        iconAreaRecTrans = iconArea.GetComponent<RectTransform>();
        pathAreaRecTrans = pathArea.GetComponent<RectTransform>();

        containerSizeDelta = containerTrans.sizeDelta;
        
        worldPosition = worldRepositionManager.playerWorldPos;
        previousWorldPosition = worldPosition;
        mapPosition = WorldSpaceToMapSpace(worldPosition);
        worldScale = terrainInfo.meshVerts * terrainInfo.vertexScale;
        
        targetPathLine.point1 = new Vector2(0, 0);
        targetPathLine.point2 = new Vector2(0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        if (update)
        {
            previousWorldPosition = worldPosition;
            worldPosition = worldRepositionManager.playerWorldPos;
            mapPosition = WorldSpaceToMapSpace(worldPosition);
        }
        
        // do this every update
        float heading = instrumentInputs.HeliTransform.rotation.eulerAngles.y;
        positionOverlayIcon.localRotation = Quaternion.Euler(0, 0, -heading);

        // update the distance text every frame
        UpdateDistanceText();

        // -------------------------------------------------------------------------------------------------------------
        timeCounter += Time.deltaTime;
        if (timeCounter < updateTime) return;
        // reset the time counter
        timeCounter = 0;

        UpdateETAText();
        
        loadingOverlay.SetActive(false);

        Vector3 localMapPosition = new Vector3(-1 * mapPosition.x + (containerSizeDelta.x / 2), -1 * mapPosition.z + (containerSizeDelta.y / 2), 0);

        if (update)
        {
            mapAreaRecTrans.localPosition = localMapPosition;
            iconAreaRecTrans.localPosition = localMapPosition;
            pathAreaRecTrans.localPosition = localMapPosition;
        }

        group1.alpha = group1Opacity;
        
        UpdatePathLines();
    }

    public void AddMapImage(Texture2D map, IntVector2 gridPos)
    {
        map.filterMode = FilterMode.Point;
        GameObject imageInstance = Instantiate(imagePrefab, mapArea.transform);
        Image imageComp = imageInstance.GetComponent<Image>();
        imageComp.sprite = Sprite.Create(map, new Rect(0.0f, 0.0f, map.width, map.height), new Vector2(0, 0));

        RectTransform rectTransform = imageInstance.GetComponent<RectTransform>();
        rectTransform.sizeDelta = Vector2.one * containerSizeDelta;
        rectTransform.localPosition = new Vector3(gridPos.x * rectTransform.sizeDelta.x, gridPos.y * rectTransform.sizeDelta.y, 0);
    }

    public void SetGPSTarget(GPSGUI.GPSDestination des)
    {
        if (des == null)
        {
            Debug.Log("Deselecting destination");
            _targetDestination = null;
            targetLocation = Vector3.zero;
            targetPathLine.cull = true;
            _targetDestinationSet = false;
            destinationOverlay.SetActive(false);

            destinationName.text = "--";
            destinationETA.text = "--";
            destinationDistance.text = "--";
        }
        else
        {
            Debug.Log("New destination selected");
            _targetDestination = des;
            _targetDestinationSet = true;
            targetPathLine.cull = false;
            targetLocation = WorldSpaceToMapSpace(des.worldPos);
            destinationOverlay.SetActive(true);
            
            destinationName.text = des.name;
            destinationETA.text = "--";
            destinationDistance.text = "--";
        }

        // force an update of the map
        timeCounter = updateTime;
    }

    public void AddIcon(Vector2 objWorldPos, GPSGUI.IconTypes type)
    {
        GameObject imageInstance = Instantiate(imagePrefab, iconArea.transform);
        Image imageComp = imageInstance.GetComponent<Image>();
        GPSGUI.Icon icon = guiElements.GetSpriteForType(type);
        imageComp.sprite = icon.sprite;
        

        RectTransform rectTransform = imageInstance.GetComponent<RectTransform>();
        rectTransform.sizeDelta = containerSizeDelta * icon.guiScale;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;
        rectTransform.localPosition = new Vector3((objWorldPos.x / worldScale) * containerSizeDelta.x - (rectTransform.sizeDelta.x / 2), (objWorldPos.y  / worldScale) * containerSizeDelta.y - (rectTransform.sizeDelta.y / 2), 0);

        // Debug.Log("Helipad icon world pos: " + objWorldPos + " localpos: " + rectTransform.localPosition);
    }

    private void UpdatePathLines()
    {
        if (update)
        {
            targetPathLine.point1 = new Vector2(targetLocation.x, targetLocation.z);
            targetPathLine.point2 = new Vector2(mapPosition.x, mapPosition.z);
            targetPathLine.ForceUpdate();
        }
    }

    public void SpawnPointResponse(Vector3 pos)
    {
        targetLocation = WorldSpaceToMapSpace(pos);
        mapPosition = WorldSpaceToMapSpace(pos);
    }

    private Vector3 WorldSpaceToMapSpace(Vector3 worldPos)
    {
        worldPos /= terrainInfo.meshVerts * terrainInfo.vertexScale;
        worldPos.x *= containerSizeDelta.x;
        worldPos.z *= containerSizeDelta.y;
        return worldPos;
    }

    private void UpdateDistanceText()
    {
        if ( _targetDestinationSet )
        {
            float distanceToDestination = Vector2.Distance(_targetDestination.worldPos.Flatten(), worldPosition.Flatten());
            destinationDistance.text = Utilities.UnityDistanceToMiles(distanceToDestination).ToString("n1");
        }
    }
    
    private void UpdateETAText()
    {
        // update the ETA text every map refresh
        if ( _targetDestinationSet )
        {
            float distanceToDestination = Vector2.Distance(_targetDestination.worldPos.Flatten(), worldPosition.Flatten());
            float velocity = (Vector2.Distance(previousWorldPosition.Flatten(), worldPosition.Flatten()) / updateTime);
            float etaInSeconds = -1;
            if (velocity != 0)
            {
                etaInSeconds = distanceToDestination * velocity;
            }

            string etaText = (etaInSeconds == -1 ? destinationETA.text : "~" + (etaInSeconds / 60f).ToString("n1"));
            destinationETA.text = etaText;
        }
    }
    public static Color PreProcessMapImage(int x, int y, int width, int height, Color intendedColor)
    {
        if ( x == 0 || y == 0 ) return new Color(intendedColor.r * 0.75f, intendedColor.g * 0.75f, intendedColor.b * 0.75f, 1);

        return intendedColor;
    }
}
