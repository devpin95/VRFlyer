using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMap
{
    public static bool ALTITUDE_BELOW(float rhs, float lhs) { return rhs <= lhs; }
    public static bool ALTITUDE_ABOVE(float rhs, float lhs) { return rhs >= lhs; }
    
    private float[,] heightmap = null;
    private int mapverts = 2;
    private int mapsquares = 1;

    // variables for generating octave noise
    private float[] _noiseFrequencies = null; // octave frequency 
    private float[] _noiseAmplitudes = null; // octave amplitude
    private float _maxSampleValue = 1; // the max sample value possible from the octave noise function (the sum of the amplitudes)

    public void InitMap(int dim)
    {
        if (dim <= 1)
        {
            Debug.LogError("Terrain map dimensions must be larger than 1");
            return;
        }
        
        mapverts = dim;
        mapsquares = dim - 1;
        heightmap = new float[dim, dim];
    }

    public float[,] GenerateMap(Vector2 offset, float scale, int octaves = 5)
    {
        GenerateOctaveParams(octaves);
        
        // init the min and max values so we can remap later
        float maxy = float.MinValue;
        float miny = float.MaxValue;

        // convert these to ints now so we dont have to do it every loop
        int xoffset = (int)offset.x;
        int zoffset = (int) offset.y;
        
        // create vertices
        for (int z = 0; z < mapverts; ++z)
        {
            for (int x = 0; x < mapverts; ++x)
            {
                float y = SampleOctavePerlinNoise(x, z, xoffset, zoffset, scale);

                if (y < miny) miny = y;
                if (y > maxy) maxy = y;

                heightmap[x, z] = y;
            }
        }
        
        // Debug.Log("Min: " + miny);
        // Debug.Log("Max: " + maxy);
        
        // remap the values [miny, maxy] -> [0,1]
        for (int z = 0; z < mapverts; ++z)
        {
            for (int x = 0; x < mapverts; ++x)
            {
                heightmap[x, z] = Utilities.Remap(heightmap[x, z], miny, maxy, 0, 1);
            }
        }
        
        return heightmap;
    }

    public float[,] Get2DHeightMap()
    {
        return heightmap;
    }

    public float[] GetFlattenedHeightMap()
    {
        return Utilities.Flatten2DArray(heightmap, mapverts, mapverts);
    }

    public Vector3[] GetFlattenedVector3VertMap(float vertScale)
    {
        Vector3[,] vectormap = new Vector3[mapverts, mapverts];
        for (int z = 0; z < mapverts; ++z)
        {
            for (int x = 0; x < mapverts; ++x)
            {
                vectormap[x, z] = new Vector3(x * vertScale, heightmap[x, z], z * vertScale);
            }
        }

        return Utilities.Flatten2DArray(vectormap, mapverts, mapverts);
    }

    public Texture2D GetHeightMapTexture2D()
    {
        float[] flatmap = GetFlattenedHeightMap();
        Color[] colormap = new Color[heightmap.Length];

        for (int i = 0; i < flatmap.Length; ++i)
        {
            colormap[i] = new Color(flatmap[i], flatmap[i], flatmap[i], 1);
        }
        
        Texture2D heightMapTex = new Texture2D(mapverts, mapverts);
        heightMapTex.SetPixels(colormap);
        heightMapTex.Apply();

        return heightMapTex;
    }

    public Texture2D GetAltitudeMap(float altitude, Func<float, float, bool> comp)
    {
        float[] flatmap = GetFlattenedHeightMap();
        Color[] colormap = new Color[heightmap.Length];

        for (int i = 0; i < heightmap.Length; ++i)
        {
            if ( comp(flatmap[i], altitude) ) colormap[i] = new Color(1, 1, 1, 1);
            else colormap[i] = new Color(0, 0, 0, 1);
        }
        Texture2D altitudeMapTex = new Texture2D(mapverts, mapverts);
        altitudeMapTex.SetPixels(colormap);
        altitudeMapTex.Apply();

        return altitudeMapTex;
    }
    
    public float SampleOctavePerlinNoise(int x, int z, int xoffset, int zoffset, float scale)
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

        float minXSampleRange = xoffset * scale;
        float minZSampleRange = zoffset * scale;

        float xSamplePoint = Utilities.Remap(x, 0, mapverts, minXSampleRange, minXSampleRange + scale);
        float zSamplePoint = Utilities.Remap(z, 0, mapverts, minZSampleRange, minZSampleRange + scale);

        for (int i = 0; i < _noiseAmplitudes.Length; ++i)
        {
            float perlin = _noiseAmplitudes[i] * Mathf.PerlinNoise(_noiseFrequencies[i] * xSamplePoint, _noiseFrequencies[i] * zSamplePoint);

            y += perlin;
        }

        y /= _maxSampleValue; // normalize the result

        return y;

        // return Remap(y, 0, 1, remapMin, remapMax);
    }

    private void GenerateOctaveParams(int octaves)
    {
        // PARAMETERS
        //      octaves (int) the number of octaves to generate
        // DESCRIPTION
        //      generates the amplitudes and frequencies that will be used to generate octave noise
        // OUTPUT
        //      _noiseAmplitudes and _noiseFrequencies class variables will be initialized with their correct values

        _noiseAmplitudes = new float[octaves];
        _noiseFrequencies = new float[octaves];
        
        _noiseAmplitudes[0] = 1;
        _noiseFrequencies[0] = 1;
        
        for (int i = 1; i < octaves; ++i)
        {
            // get the power of 2 that will be used for this octave
            float fact = (float) Math.Pow(2, i);
            
            // set the octave amplitude and increment the max value
            _noiseAmplitudes[i] = 1.0f / fact;
            _maxSampleValue += _noiseAmplitudes[i];
            
            // set the octave frequency
            _noiseFrequencies[i] = fact;
        }
    }
}
