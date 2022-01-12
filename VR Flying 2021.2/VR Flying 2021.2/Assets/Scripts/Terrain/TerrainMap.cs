using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMap
{
    public static bool ALTITUDE_BELOW(float rhs, float lhs) { return rhs <= lhs; }
    public static bool ALTITUDE_ABOVE(float rhs, float lhs) { return rhs >= lhs; }
    
    private const float NeighborWeight = 0.15f;
    private const float DiagonalWeight = 0.1f;
    
    private float[,] heightmap = null;
    private int mapverts = 256;
    private int mapsquares = 255;
    
    private float maxy = float.MinValue;
    private float miny = float.MaxValue;

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
        maxy = float.MinValue;
        miny = float.MaxValue;

        // convert these to ints now so we dont have to do it every loop
        int xoffset = (int) offset.x;
        int zoffset = (int) offset.y;

        float l1 = xoffset * scale;
        float l2 = l1 + scale;
        float r1 = zoffset * scale;
        float r2 = r1 + scale;
        
        float l11 = Utilities.Remap(0, 0, mapverts, l1, l2);
        float l12 = Utilities.Remap(mapverts, 0, mapverts, l1, l2);
        float r11 = Utilities.Remap(0, 0, mapverts, r1, r2);
        float r12 = Utilities.Remap(mapverts, 0, mapverts, r1, r2);

        // Debug.Log("Offset: x(" + l1 + "=" + l11 + "," + l2 + "=" + l12 + ") z(" + r1 + "=" + r11 + "," + r2 + "=" + r12 + ")");

        // create vertices
        for (int x = 0; x < mapverts; ++x)
        {
            for (int z = 0; z < mapverts; ++z)
            {
                float y = SampleOctavePerlinNoise(x, z, xoffset, zoffset, scale);

                if (y < miny) miny = y;
                if (y > maxy) maxy = y;

                heightmap[x, z] = y;
            }
        }
        
        Debug.Log("Min: " + miny + " Max: " + maxy);

        // !!!!!!!
        // We can't actually do the stretching here since we are generating blocks independently. Doing it here
        // means that each block is getting stretched by it's own local min and max value and will make the edges
        // between each block not match
        // !!!!!!!
        
        // Debug.Log("Min: " + miny);
        // Debug.Log("Max: " + maxy);
        
        // float nmaxy = float.MinValue;
        // float nminy = float.MaxValue;
        //
        // // remap the values [miny, maxy] -> [0,1]
        // for (int x = 0; x < mapverts; ++x)
        // {
        //     for (int z = 0; z < mapverts; ++z)
        //     {
        //         heightmap[x, z] = Utilities.Remap(heightmap[x, z], miny, maxy, 0, 1);
        //         
        //         if (heightmap[x, z] < nminy) nminy = heightmap[x, z];
        //         if (heightmap[x, z] > nmaxy) nmaxy = heightmap[x, z];
        //     }
        // }
        
        // Debug.Log("New Min: " + nminy);
        // Debug.Log("New Max: " + nmaxy);
        
        return heightmap;
    }

    public float[,] Get2DHeightMap()
    {
        return heightmap;
    }

    public float[,] Get2DHeightMapRemapped(float min, float max)
    {
        float[,] remapped = new float[mapverts, mapverts];

        for (int x = 0; x < mapverts; ++x)
        {
            for (int z = 0; z < mapverts; ++z)
            {
                remapped[x, z] = Utilities.Remap(heightmap[x, z], miny, maxy, min, max);
            }
        }

        return remapped;
    }

    public float[] GetFlattenedHeightMap()
    {
        return Utilities.Flatten2DArray(heightmap, mapverts, mapverts);
    }
    
    public float[] GetRemappedFlattenedHeightMap(float min, float max)
    {
        float[,] remapped = Get2DHeightMapRemapped(min, max);
        return Utilities.Flatten2DArray(remapped, mapverts, mapverts);
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
    
    public Vector3[] GetRemappedFlattenedVector3VertMap(float vertScale, float min, float max)
    {
        Vector3[,] vectormap = new Vector3[mapverts, mapverts];
        for (int z = 0; z < mapverts; ++z)
        {
            for (int x = 0; x < mapverts; ++x)
            {
                float y = Utilities.Remap(heightmap[x, z], 0, 1, min, max);
                vectormap[x, z] = new Vector3(x * vertScale, y, z * vertScale);
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

    public Vector3[,] GetNormalMap(float min, float max)
    {
        Vector3[,] normalmap = new Vector3[mapverts, mapverts];
        float[,] remapped = Get2DHeightMapRemapped(min, max);

        for (int x = 0; x < mapverts; ++x)
        {
            for (int z = 0; z < mapverts; ++z)
            {
                normalmap[x, z] = SampleMapNormalAtXY(remapped, x, z);
            }
        }
        
        return normalmap;
    }
    
    public Texture2D GetNormalMapTex2D(float min, float max)
    {
        Color[,] colormap = new Color[mapverts, mapverts];
        float[,] remapped = Get2DHeightMapRemapped(min, max);

        for (int x = 0; x < mapverts; ++x)
        {
            for (int z = 0; z < mapverts; ++z)
            {
                Vector3 normal = SampleMapNormalAtXY(remapped, x, z);
                normal = new Vector3(Math.Abs(normal.x), Math.Abs(normal.z), Math.Abs(normal.y));
                colormap[x, z] = new Color(normal.x, normal.y, normal.z, 1);
            }
        }

        Color[] flatcolors = Utilities.Flatten2DArray(colormap, mapverts, mapverts);
        
        Texture2D normalmap = new Texture2D(mapverts, mapverts);
        normalmap.SetPixels(flatcolors);
        normalmap.Apply();
        
        return normalmap;
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

        float xSamplePoint = Utilities.Remap(x, 0, mapsquares, minXSampleRange, minXSampleRange + scale);
        float zSamplePoint = Utilities.Remap(z, 0, mapsquares, minZSampleRange, minZSampleRange + scale);

        for (int i = 0; i < _noiseAmplitudes.Length; ++i)
        {
            float perlin = Mathf.PerlinNoise(_noiseFrequencies[i] * xSamplePoint, _noiseFrequencies[i] * zSamplePoint);
            float clamped = Mathf.Clamp01(perlin);
            float sample = _noiseAmplitudes[i] * clamped;

            y += sample;
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
    
    public Vector3 SampleNormalAtXY(int row, int col)
    {
        Vector3 normal = Vector3.zero;

        // direct neighbors
        // up
        if (row + 1 < mapverts)
        {
            normal += Vector3.Normalize(new Vector3(heightmap[row, col] - heightmap[row + 1, col], 1f, 0)) * NeighborWeight;
        }

        // down
        if (row - 1 >= 0) 
            normal += Vector3.Normalize(new Vector3(heightmap[row - 1, col] - heightmap[row, col], 1f, 0)) * NeighborWeight;
        
        // right
        if ( col + 1 < mapverts ) 
            normal += Vector3.Normalize(new Vector3(0, 1f, heightmap[row, col] - heightmap[row, col + 1])) * NeighborWeight;
        
        // left
        if ( col - 1 >= 0 ) 
            normal += Vector3.Normalize(new Vector3(0, 1f, heightmap[row, col-1] - heightmap[row, col])) * NeighborWeight;
        
        // diagonals
        float sqrt2 = Mathf.Sqrt(2);
        
        // up right
        if (row + 1 < mapverts && col + 1 < mapverts)
        {
            float val = heightmap[row, col] - heightmap[row + 1, col + 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * DiagonalWeight;
        }

        // up left
        if (row + 1 < mapverts && col - 1 >= 0)
        {
            float val = heightmap[row, col] - heightmap[row + 1, col - 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * DiagonalWeight;
        }
        
        // down right
        if (row - 1 >= 0 && col + 1 < mapverts)
        {
            float val = heightmap[row, col] - heightmap[row - 1, col + 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * DiagonalWeight;
        }
        
        // down left
        if (row - 1 >= 0 && col - 1 >= 0)
        {
            float val = heightmap[row, col] - heightmap[row - 1, col - 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * DiagonalWeight;
        }

        return normal;
    }

    public Vector3 SampleMapNormalAtXY(float[,] map, int x, int y)
    {
        Vector3 normal = Vector3.zero;

        // direct neighbors
        // up
        if (x + 1 < mapverts)
        {
            normal += Vector3.Normalize(new Vector3(map[x, y] - map[x + 1, y], 1f, 0)) * NeighborWeight;
        }

        // down
        if (x - 1 >= 0) 
            normal += Vector3.Normalize(new Vector3(map[x - 1, y] - map[x, y], 1f, 0)) * NeighborWeight;
        
        // right
        if ( y + 1 < mapverts ) 
            normal += Vector3.Normalize(new Vector3(0, 1f, map[x, y] - map[x, y + 1])) * NeighborWeight;
        
        // left
        if ( y - 1 >= 0 ) 
            normal += Vector3.Normalize(new Vector3(0, 1f, map[x, y-1] - map[x, y])) * NeighborWeight;
        
        // diagonals
        float sqrt2 = Mathf.Sqrt(2);
        
        // up right
        if (x + 1 < mapverts && y + 1 < mapverts)
        {
            float val = map[x, y] - map[x + 1, y + 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * DiagonalWeight;
        }

        // up left
        if (x + 1 < mapverts && y - 1 >= 0)
        {
            float val = map[x, y] - map[x + 1, y - 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * DiagonalWeight;
        }
        
        // down right
        if (x - 1 >= 0 && y + 1 < mapverts)
        {
            float val = map[x, y] - map[x - 1, y + 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * DiagonalWeight;
        }
        
        // down left
        if (x - 1 >= 0 && y - 1 >= 0)
        {
            float val = map[x, y] - map[x - 1, y - 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * DiagonalWeight;
        }
        
        return normal;
    }
}
