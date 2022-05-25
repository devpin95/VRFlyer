using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPSInfo : ScriptableObject
{
    [Header("GPS Settings")] 
    public List<GPSHeightLevel> gpsLevels = new List<GPSHeightLevel>();

    [Serializable]
    public struct GPSHeightLevel
    {
        public float height;
        public Color color;
    }
}
