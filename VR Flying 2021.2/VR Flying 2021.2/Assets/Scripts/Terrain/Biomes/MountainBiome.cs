using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MountainBiome : ScriptableObject
{
    // private static int octaveCount = 5;
    // private static float[] frequencies = { 1, 2, 4, 8, 16 };
    // private static float[] amplitudes = { 1, 0.5f, 0.25f, 0.125f, 0.0625f }; // sum = 1.9375
    // private static float octaveMax = 1.75f; // play with this number
    //
    // [Tooltip("Octave data. x = frequency, y = amplitude.")] public List<Vector2> octaves = new List<Vector2>();
    // [Tooltip("0 to use the sum of the amplitudes, >0 for a custom value")] public float normal = 0;
    //
    // // private static float[] seeds = { 7462, 94128, 84174, 74571, 25304 };
    //
    // public override float Height(int x, int y, IntVector2 offset)
    // {
    //     // get the bottom corner of the window where the mesh will be sampling from
    //     float minXSampleRange = offset.x * terrainInfo.chunkSize;
    //     float minYSampleRange = offset.y * terrainInfo.chunkSize;
    //
    //     // get position of the sample points within the window of the mesh
    //     float xSamplePoint = Utilities.Remap(x, 0, terrainInfo.meshSquares, minXSampleRange, minXSampleRange + terrainInfo.chunkSize);
    //     float ySamplePoint = Utilities.Remap(y, 0, terrainInfo.meshSquares, minYSampleRange, minYSampleRange + terrainInfo.chunkSize);
    //
    //     float height = 0;
    //     
    //     for (int i = 0; i < octaves.Count; ++i)
    //     {
    //         float freq = octaves[i].x;
    //         float amp = octaves[i].y;
    //         
    //         float perlin = Mathf.PerlinNoise(xSamplePoint * freq, ySamplePoint * freq);
    //         float clamped = Mathf.Clamp01(perlin);
    //         float sample = amp * clamped;
    //
    //         height += sample;
    //     }
    //
    //     height /= octaveMax;
    //
    //     height = heightCurve.Evaluate(height);
    //     
    //     return height;
    // }
    //
    // public override float Weight(int x, int y, IntVector2 offset)
    // {
    //     return 1;
    // }
    
}
