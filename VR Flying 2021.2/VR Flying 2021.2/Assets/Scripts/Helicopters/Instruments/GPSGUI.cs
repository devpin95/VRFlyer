using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class GPSGUI : ScriptableObject
{
    public enum IconTypes
    {
        Helipad,
        RadarTower
    }

    [Serializable]
    public struct Icon
    {
        public IconTypes iconType;
        [FormerlySerializedAs("image")] public Sprite sprite;
        public Vector2 guiScale;
    }

    [Header("Icons")] 
    public Icon defaultIcon;
    public List<Icon> gpsIcons;

    public Icon GetSpriteForType(IconTypes type)
    {
        foreach (var icon in gpsIcons)
        {
            if (icon.iconType == type) return icon;
        }

        return defaultIcon;
    }
    
    public class GPSDestination
    {
        public string name;
        public Vector3 worldPos;
        public GPSGUI.IconTypes icon;
        public bool preset;
    }
}
