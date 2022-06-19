using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    
    [Header("Probabilities")] 
    [Tooltip("Probability of a terrain chunk containing a helipad. Terrain chunk at [0, 0] will always have a helipad spawn")] 
    public float pHelipadSpawn = 0.25f;
    public float pLakeSpawn = 1f;
    public float pLakeVarianceThreshold = 0.05f;
    
    public static float PLakeSpawn = 1f;
    public static float PHelipadSpawn = 0.25f;
    public static float PLakeVarianceThreshold = 1f;

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

    [Header("Biomes")] 
    public List<Biome> biomes;

    public List<Biome> GetNonContributingBiomes()
    {
        return biomes.Where(b => !b.contribute).ToList();
    }
    
    public List<Biome> GetContributingBiomes()
    {
        return biomes.Where(b => b.contribute).ToList();
    }

    public float MaxTerrainHeight()
    {
        return biomes.Sum(b => b.maxHeight);
    }

    private void OnValidate()
    {
        PLakeSpawn = pLakeSpawn;
        PHelipadSpawn = pHelipadSpawn;
        PLakeVarianceThreshold = pLakeVarianceThreshold;
    }
}
