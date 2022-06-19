using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GPSInfo : ScriptableObject
{
    [Header("Global Settings")] 
    public float updateTime = 10;
    
    [Header("Menu Settings")]
    public bool exitWhenIdle = true;
    public float idleTimeout = 10;
    
    [Header("Map Settings")]
    public List<GPSHeightLevel> gpsLevels = new List<GPSHeightLevel>();

    [Header("Trail Line Settings")] 
    public float trailLineWidth;
    public int checkpointCount = 50;
    [FormerlySerializedAs("worldDistanceBetweenCheckpoints")] public float distanceBetweenTrailPoints;

    [Header("Destination Settings")] 
    [Tooltip("Destinations outside this range will not appear in the destination list (unless they are a preset)")] public float destinationDistanceFilter = 30f;

    [Serializable]
    public struct GPSHeightLevel
    {
        public float height;
        public Color color;
    }
}
