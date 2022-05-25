using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class TerrainInfo : ScriptableObject
{
    [Header("World Info")] 
    public float baseAltitude = 1000;
    
    [Header("Mesh")] 
    public int meshSquares;
    public int meshVerts;
    public float chunkSize;
    public float vertexScale;
    public AnimationCurve terrainCurve;
    public int terrainCurveSampleSize = 255;

    [Header("Terrain")] 
    public int noiseOctaves;
    public float remapMin;
    public float remapMax;
    public float recalculateLodDistance;

    [Header("Detail Levels (LODS)")]
    public List<LODInfo> lods = new List<LODInfo>();
    public float cullDistance;
    public float heightCullDistance = 1000;

    [Serializable]
    public struct LODInfo
    {
        public int lod;
        public float distance;

        public LODInfo(int lod, float distance)
        {
            this.lod = lod;
            this.distance = distance;
        }
    }

    public LODInfo GetLowestLOD()
    {
        return lods[lods.Count - 1];
    }
}
