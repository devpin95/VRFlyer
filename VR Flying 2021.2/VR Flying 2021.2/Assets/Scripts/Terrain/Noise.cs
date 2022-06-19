using System.Collections;
using System.Collections.Generic;
using DigitalRuby.WeatherMaker;
using Unity.Mathematics;
using UnityEngine;

public static class Noise
{
    public static int octaveCount = 5;
    public static float[] frequencies = { 1, 2, 4, 8, 16 };
    public static float[] amplitudes = { 1, 0.5f, 0.25f, 0.125f, 0.0625f }; // sum = 1.9375
    public static float octaveMax = 1.75f; // play with this number
    public static float[] seeds = { 7462, 94128, 84174, 74571, 25304 };
    
    public static float SampleOctavePerlinNoise(int x, int z, int mapsquares, int xoffset, int zoffset, float chunkSize, OctaveData octaveData)
    {
        // PARAMETERS
        //      x (int) the position to sample in the x direction
        //      z (int) the position to sample in the z direction
        //      xoffset (int) the offset of sample space in the x direction
        //      zoffset (int) the offset of sample space in the z direction
        //      scale (float) the scale of offset chunks
        // DESCRIPTION
        //      Samples Perlin Noise at a given (x,z) with offset (xoffset, zoffset). xoffset and zoffset are blocks
        //      of certain size that indicate that starting and ending sample points. For example, with offset (0,0) and
        //      scale=5, noise in both the x and z directions will be sampled at [0,5]. At offset (0, 1) and scale=5, noise
        //      in the x direction will be sampled at [0,5] and the z direction will be sampled at [5,10]
        // OUTPUT
        //      the value sampled from the noise function

        float y = 0;

        float minXSampleRange = xoffset * chunkSize;
        float minZSampleRange = zoffset * chunkSize;

        float xSamplePoint = Utilities.Remap(x, 0, mapsquares, minXSampleRange, minXSampleRange + chunkSize);
        float zSamplePoint = Utilities.Remap(z, 0, mapsquares, minZSampleRange, minZSampleRange + chunkSize);

        for (int i = 0; i < octaveCount; ++i)
        {
            float perlin = Mathf.PerlinNoise(frequencies[i] * xSamplePoint + seeds[i], frequencies[i] * zSamplePoint + seeds[i]);
            float clamped = Mathf.Clamp01(perlin);
            float sample = amplitudes[i] * clamped;

            y += sample;
        }

        // Debug.Log("MAP OCTAVE MAX: " + octaveData.MAXSampleValue);
        y /= octaveMax; // normalize the result

        return y;

        // return Remap(y, 0, 1, remapMin, remapMax);
    }

    public static OctaveData GenerateOctaveData(int octaves)
    {
        // PARAMETERS
        //      octaves (int) the number of octaves to generate
        // DESCRIPTION
        //      generates the amplitudes and frequencies that will be used to generate octave noise
        // OUTPUT
        //      _noiseAmplitudes and _noiseFrequencies class variables will be initialized with their correct values

        OctaveData octaveData = new OctaveData();
        
        octaveData.Amplitudes = new float[octaves];
        octaveData.Frequencies = new float[octaves];
        octaveData.Seeds = new float[octaves];

        octaveData.Amplitudes[0] = 1;
        octaveData.Frequencies[0] = 1;
        octaveData.MAXSampleValue = 1;
        
        for (int i = 1; i < octaves; ++i)
        {
            // get the power of 2 that will be used for this octave
            float fact = (float) Mathf.Pow(2, i);
            
            // set the octave amplitude and increment the max value
            octaveData.Amplitudes[i] = 1.0f / fact;
            octaveData.MAXSampleValue += octaveData.Amplitudes[i];
            
            // set the octave frequency
            octaveData.Frequencies[i] = fact;
        }

        return octaveData;
    }

    public class OctaveData
    {
        private float[] amplitudes;
        private float[] frequencies;
        private float[] seeds;
        private float maxSampleValue = 1;

        public float[] Amplitudes
        {
            get => amplitudes;
            set => amplitudes = value;
        }

        public float[] Frequencies
        {
            get => frequencies;
            set => frequencies = value;
        }

        public float[] Seeds
        {
            get => seeds;
            set => seeds = value;
        }

        public float MAXSampleValue
        {
            get => maxSampleValue;
            set => maxSampleValue = value;
        }
    }
}
