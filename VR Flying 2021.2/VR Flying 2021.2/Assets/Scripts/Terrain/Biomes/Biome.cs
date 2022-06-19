using System.Collections.Generic;
using System.Linq;
using TreeEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Terrain/Biome")]
public class Biome : ScriptableObject
{
    [Header("Meta")]
    public TerrainInfo terrainInfo;
    [FormerlySerializedAs("randomSeed")] public bool seedHeights = false;
    public bool seedWeights = false;
    public bool contribute = true;

    [Header("Mapping")]
    public float maxHeight;
    public PerlinNoiseMap heightMap;
    public PerlinNoiseMap weightMap;

    public float Height(int x, int y, IntVector2 offset)
    {
        return Height01(x, y, offset) * maxHeight;
    }

    public float Height01(int x, int y, IntVector2 offset)
    {
        return heightMap.Sample(x, y, offset);
    }
    
    public float Height01Curved(int x, int y, IntVector2 offset, AnimationCurve curve)
    {
        return heightMap.SampleCurved(x, y, offset, curve);
    }

    public float Weight(int x, int y, IntVector2 offset)
    {
        return weightMap.Sample(x, y, offset);
    }
    
    public float WeightCurved(int x, int y, IntVector2 offset, AnimationCurve curve)
    {
        return weightMap.SampleCurved(x, y, offset, curve);
    }

    public virtual float WeightedHeight(int x, int y, IntVector2 offset)
    {
        return Height(x, y, offset) * Weight(x, y, offset);
    }
    
    public float WeightedHeight01(int x, int y, IntVector2 offset)
    {
        return Height01(x, y, offset) * Weight(x, y, offset);
    }
    
    public float WeightedHeight01Curved(int x, int y, IntVector2 offset, AnimationCurve hCurve, AnimationCurve wCurve)
    {
        return Height01Curved(x, y, offset, hCurve) * WeightCurved(x, y, offset, wCurve);
    }

    public static float AccumulateBiomes(int x, int y, IntVector2 offset, List<Biome> biomes)
    {
        float height = 0;

        foreach (var biome in biomes)
        {
            height += biome.WeightedHeight(x, y, offset );
        }
        
        return height;
    }
    
    public static float AccumulateBiomes01(int x, int y, IntVector2 offset, List<Biome> biomes, float maxHeight)
    {
        float height = 0;

        foreach (var biome in biomes)
        {
            // var heightCurve = new AnimationCurve(biome.heightMap.heightCurve.keys);
            // var weightCurve = new AnimationCurve(biome.weightMap.heightCurve.keys);
            // height += biome.WeightedHeight01Curved(x, y, offset, heightCurve, weightCurve);
            height += biome.WeightedHeight(x, y, offset) / maxHeight;
        }
        
        return height;
    }

    public static float Remap01Height(float y, List<Biome> biomes)
    {
        return y * biomes.Sum(b => b.maxHeight);
    }
}
