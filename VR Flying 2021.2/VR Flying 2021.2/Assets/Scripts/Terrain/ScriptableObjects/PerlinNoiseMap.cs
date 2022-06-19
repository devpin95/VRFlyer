using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Terrain/Perlin Noise Map")]
public class PerlinNoiseMap : ScriptableObject
{
    public TerrainInfo terrainInfo;
    [SerializeField] public Classes.ThreadsafeCurve heightCurve;
    
    [Header("Octaves")] 
    [Tooltip("Octave data. x = frequency, y = amplitude.")] 
    public List<Vector2> octaves = new List<Vector2>();

    public bool sumAmplitudes = true;
    public float normal = 0;

    public List<float> scalars = new List<float>();
    
    [Header("Randomness")]
    public IntVector2 randomOffset = IntVector2.zero;

    [Header("Operations")] 
    public bool invert = false;
    public bool abs = false;

    public float SampleCurved(int x, int y, IntVector2 offset, AnimationCurve curve)
    {
        return curve.Evaluate(Sample(x, y, offset));
    }
    
    public float Sample(int x, int y, IntVector2 offset)
    {
        // offset += randomOffset;
        
        // get the bottom corner of the window where the mesh will be sampling from
        float minXSampleRange = offset.x * terrainInfo.chunkSize;
        float minYSampleRange = offset.y * terrainInfo.chunkSize;
        
        // get position of the sample points within the window of the mesh
        float xSamplePoint = Utilities.Remap(x, 0, terrainInfo.meshSquares, minXSampleRange, minXSampleRange + terrainInfo.chunkSize);
        float ySamplePoint = Utilities.Remap(y, 0, terrainInfo.meshSquares, minYSampleRange, minYSampleRange + terrainInfo.chunkSize);
        
        float height = 0;
        
        for (int i = 0; i < octaves.Count; ++i)
        {
            float freq = octaves[i].x;
            float amp = octaves[i].y;
            float scale = (i < scalars.Count ? scalars[i] : 1);
            
            float perlin = Mathf.PerlinNoise(xSamplePoint * freq * scale, ySamplePoint * freq * scale);
            // float clamped = Mathf.Clamp01(perlin);
            // float sample = amp * clamped;
            float sample = amp * perlin;
        
            height += sample;
        }

        height /= normal;
        
        height = heightCurve.Evaluate(height);

        if (invert) height = 1 - height;
        if (abs)
        {
            height = Utilities.Remap(height, 0, 1, -1, 1);
            height = Mathf.Abs(height);
        }

        return height;
    }

    private void OnValidate()
    {
        if (sumAmplitudes && octaves != null) normal = octaves.Select(s => s.y).Sum();
        else if (!sumAmplitudes && normal == 0) normal = 1;
    }
}
