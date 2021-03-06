using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Runtime.ConstrainedExecution;
using System.Threading;
using OpenCover.Framework.Model;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

// https://catlikecoding.com/unity/tutorials/procedural-meshes/creating-a-mesh/
// https://www.raywenderlich.com/7880445-unity-job-system-and-burst-compiler-getting-started

public class TerrainMap
{
    public static bool ALTITUDE_BELOW(float rhs, float lhs) { return rhs <= lhs; }
    public static bool ALTITUDE_ABOVE(float rhs, float lhs) { return rhs >= lhs; }
    
    public static AnimationCurve defaultHeightCurve = AnimationCurve.Constant(0, 1, 1f);
    
    private const float NeighborWeight = 0.15f;
    private const float DiagonalWeight = 0.1f;

    private NativeArray<float> nativeHeightMap;
    private NativeArray<float> nativemin;
    private NativeArray<float> nativemax;
    
    private float[,] heightmap = null;
    private bool[,] watermap = null;
    private int mapverts = 256;
    private int mapsquares = 255;
    public IntVector2 offset;
    
    private float maxy = float.MinValue;
    private float miny = float.MaxValue;
    public Vector2 maxypos = Vector2.negativeInfinity;
    public Vector2 minypos = Vector2.negativeInfinity;
    public float mean = 0;
    public float variance = 0;
    public float stdDev = 0;

    // variables for generating octave noise
    private float[] _noiseFrequencies = null; // octave frequency 
    private float[] _noiseAmplitudes = null; // octave amplitude
    private float _maxSampleValue = 1; // the max sample value possible from the octave noise function (the sum of the amplitudes)

    public List<Biome> nonContributingBiomes = new List<Biome>();
    public List<Biome> contributingBiomes = new List<Biome>();
    public float maxHeight;

    public List<Texture2D> biomeMaps = new List<Texture2D>();

    public class TerrainAttributes
    {
        public static List<int> defaultLakeTypes = new List<int>() { 0 };
        public bool[] conditions =
        {
            false, // has lake
            false, // has split lakes
            false, // has town
        };

        public List<int> lakeTypes;
    }

    public enum Attributes
    {
        HAS_LAKE = 0,
        HAS_SPLIT_LAKE = 1,
        HAS_TOWN = 2,
        LAKE_TYPES = 3
    }

    public TerrainAttributes terrainAttributes = new TerrainAttributes();
    public TerrainAttributes generatedAttributes;

    public struct Debug_MapBiomes
    {
        public Texture2D weightMap;
        public Texture2D heightMap;
    }

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
        
        // nativeHeightMap.Dispose();
        // nativeHeightMap = new NativeArray<float>(dim * dim, Allocator.Persistent);
        
        maxy = float.MinValue;
        maxypos = Vector2.negativeInfinity;

        miny = float.MaxValue;
        minypos = Vector2.negativeInfinity;

        // reset the terrain attribute conditions
        for (int i = 0; i < terrainAttributes.conditions.Length; ++i) terrainAttributes.conditions[i] = false;
        terrainAttributes.lakeTypes = new List<int> {1};
    }
    
    public void SetMap(float[,] map, float min, float max, Vector2 minpos, Vector2 maxpos)
    {
        if (map.GetLength(0) != mapverts)
        {
            Debug.LogError("Map does not match the dimensions of the current object");
            return;
        }
        
        heightmap = map;
        maxy = max;
        miny = min;
        minypos = minpos;
        maxypos = maxpos;
    }
    
    public void SetWaterMap(bool[,] map)
    {
        if (map.GetLength(0) != mapverts)
        {
            Debug.LogError("Map does not match the dimensions of the current object");
            return;
        }
        
        watermap = map;

    }
    
    public void RequestMap(Action<Terrain.Threading.TerrainMapGeneratorThread.MapGenerationOutput, bool> callback, Queue<Terrain.Threading.TerrainMapGeneratorThread.MapGenerationOutput> queue)
    {
        // this function will package map generation info together into an object that will be stored in an object
        // with a function to be run in a thread. The result of the thread will be send to the callback action passed
        // in to this function
        
        // package the generation info together and make a new threaded terrain map object to hold that info
        Terrain.Threading.TerrainMapGeneratorThread.MapGenerationInput mapGenerationData = new Terrain.Threading.TerrainMapGeneratorThread.MapGenerationInput(mapverts, offset, contributingBiomes, terrainAttributes, maxHeight);
        Terrain.Threading.TerrainMapGeneratorThread generatorThreadTerrainMapGenerator = new Terrain.Threading.TerrainMapGeneratorThread(mapGenerationData, callback, queue);
        ThreadPool.QueueUserWorkItem(delegate { generatorThreadTerrainMapGenerator.ThreadProc(); });

    }

    public void SetTerrainAttribute<T>(Attributes attr, T s)
    {
        var data = (object)s;
        switch (attr)
        {
            case Attributes.HAS_LAKE:
            case Attributes.HAS_TOWN:
            case Attributes.HAS_SPLIT_LAKE:
                terrainAttributes.conditions[(int)attr] = (bool)data;
                break;
            case Attributes.LAKE_TYPES:
                terrainAttributes.lakeTypes = (List<int>)data;
                break;
        }
    }

    public float SampleHeightMap(int x, int y)
    {
        return heightmap[x, y];
    }

    public bool SampleWaterMap(int x, int y)
    {
        return watermap[x, y];
    }

    public float SampleAndScaleHeightMap(int x, int z, float remapmin, float remapmax)
    {
        float y = heightmap[x, z]; // get the height map value [0, 1]
        // y = curve.Evaluate(y); // evaluate it on the curve
        y = Utilities.Remap(y, 0, 1, remapmin, remapmax); // remap to [min, max]
        return y;
    }
    
    public Vector2 SampleAndScaleHeightMap(float x, float z, float remapmin, float remapmax)
    {
        // returns a vector2 of the map samples
        // vector2.x is the height map value [0,1]
        // vector2.y is the scaled map value [remapmin, remapmax]
        
        Vector2 samples = Vector2.zero;
        
        // BiLerp the point using the terrain map
        // then evaluate the height on the terrain curve
        // then remap the value to the min/max terrain heights
        float lerpHeight = Utilities.BiLerp(x, z, heightmap);
        // float height = curve.Evaluate(lerpHeight);
        float y = Utilities.Remap(lerpHeight, 0, 1, remapmin, remapmax);

        samples.x = lerpHeight;
        samples.y = y;
        
        return samples;
    }

    public float[,] GenerateMap(Vector2 offset, float chunkSize, float min, float max, int octaves = 5)
    {
        GenerateOctaveParams(octaves);
        
        Debug.Log("Octave data for this thread: Amplitudes[" + Utilities.ArrayToString(_noiseAmplitudes) + "], Frequencies[" + Utilities.ArrayToString(_noiseFrequencies) + "], " + _maxSampleValue);
        
        // init the min and max values so we can remap later
        maxy = float.MinValue;
        miny = float.MaxValue;

        // convert these to ints now so we dont have to do it every loop
        int xoffset = (int) offset.x;
        int zoffset = (int) offset.y;
        
        // create vertices
        for (int x = 0; x < mapverts; ++x)
        {
            for (int z = 0; z < mapverts; ++z)
            {
                float y = SampleOctavePerlinNoise(x, z, xoffset, zoffset, chunkSize);

                if (y < miny) miny = y;
                if (y > maxy) maxy = y;

                heightmap[x, z] = y;
            }
        }
        
        // Debug.Log("Min: " + miny + " Max: " + maxy);

        // !!!!!!!
        // We can't actually do the stretching here since we are generating blocks independently. Doing it here
        // means that each block is getting stretched by it's own local min and max value and will make the edges
        // between each block not match
        // !!!!!!!

        // heightmap = HydraulicErosion.Simulate(heightmap, scale, min, max);

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

    public Vector3[] GetRemappedFlattenedVectorMap(float vertScale, float min, float max, AnimationCurve curve, int lod = 1)
    {
        AnimationCurve animCurve = new AnimationCurve(curve.keys);

        int lodWidth = mapsquares / lod + 1;
        
        Debug.Log("LOD " + lod + " mesh has width " + lodWidth + " and area " + lodWidth * lodWidth);
        
        Vector3[] vectormap = new Vector3[lodWidth * lodWidth];
        int vertIndex = 0;
        for (int z = 0; z < mapverts; z += lod)
        {
            for (int x = 0; x < mapverts; x += lod)
            {
                float y = Utilities.Remap(animCurve.Evaluate(heightmap[x, z]), 0, 1, min, max);
                vectormap[vertIndex] = new Vector3(x * vertScale, y, z * vertScale);

                ++vertIndex;
            }
        }

        return vectormap;
        // return Utilities.Flatten2DArray(vectormap, lodWidth, lodWidth);
    }

    public void GetVectorMapNative(ref NativeArray<float3> nativeMap, float vertScale, float min, float max, AnimationCurve curve, int lod = 1)
    {
        AnimationCurve animCurve = new AnimationCurve(curve.keys);
        
        // Debug.Log("LOD " + lod + " mesh has width " + lodWidth + " and area " + lodWidth * lodWidth);
        
        int vertIndex = 0;
        for (int z = 0; z < mapverts; z += lod)
        {
            for (int x = 0; x < mapverts; x += lod)
            {
                float y = Utilities.Remap(animCurve.Evaluate(heightmap[x, z]), 0, 1, min, max);
                nativeMap[vertIndex] = new float3(x * vertScale, y, z * vertScale);

                ++vertIndex;
            }
        }
    }

    public void GetVectorMapNative(ref NativeArray<float3> nativeMap, float vertScale, float max, int lod = 1)
    {
        int vertIndex = 0;
        for (int z = 0; z < mapverts; z += lod)
        {
            for (int x = 0; x < mapverts; x += lod)
            {
                float y = heightmap[x, z] * max;
                nativeMap[vertIndex] = new float3(x * vertScale, y, z * vertScale);

                ++vertIndex;
            }
        }
    }

    public int GetVertexArraySizeAtLOD(int lod)
    {
        int side = mapsquares / lod;
        int verts = side + 1;
        return verts * verts;
    }

    public int GetMeshDimAtLOD(int lod)
    {
        return mapverts / lod;
    }
    
    public int GetIndexArraySizeAtLOD(int lod)
    {
        int side = mapsquares / lod;
        return side * side * 6;
    }

    public Texture2D GetHeightMapTexture2D(AnimationCurve curve = null)
    {
        if (curve == null) curve = defaultHeightCurve;

        float[] flatmap = GetFlattenedHeightMap();
        Color[] colormap = new Color[heightmap.Length];

        for (int i = 0; i < heightmap.Length; ++i)
        {
            float curvedValue = curve.Evaluate(flatmap[i]);
            colormap[i] = new Color(0, curvedValue, 0, 1);
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

    public Texture2D GetAltitudeMap(float altitude, Func<float, float, bool> comp, Color color1, Color color2)
    {
        float[] flatmap = GetFlattenedHeightMap();
        Color[] colormap = new Color[heightmap.Length];

        for (int i = 0; i < heightmap.Length; ++i)
        {
            if ( comp(flatmap[i], altitude) ) colormap[i] = color1;
            else colormap[i] = color2;
        }
        Texture2D altitudeMapTex = new Texture2D(mapverts, mapverts);
        altitudeMapTex.SetPixels(colormap);
        altitudeMapTex.Apply();

        return altitudeMapTex;
    }

    public Texture2D GetAltitudeMap(List<GPSInfo.GPSHeightLevel> levels)
    {
        float[] flatmap = GetFlattenedHeightMap();
        Color[] colormap = new Color[heightmap.Length];

        int lowest = (int)minypos.x + (int)minypos.y * mapverts;

        for (int i = 0; i < heightmap.Length; ++i)
        {
            int y = i / mapverts; // integer division, we want the truncated int here
            int x = i % mapverts; // we want the column of the current row

            if (i == lowest)
            {
                colormap[i] = Color.black;
                continue;
            }
            
            for (int j = 0; j < levels.Count; ++j)
            {
                if (flatmap[i] < levels[j].height)
                {
                    colormap[i] = levels[j].color;
                    break;
                }
            }

            colormap[i] = GPSController.PreProcessMapImage(x, y, mapverts, mapverts, colormap[i]);
        }
        
        Texture2D altitudeMapTex = new Texture2D(mapverts, mapverts);
        altitudeMapTex.SetPixels(colormap);
        altitudeMapTex.Apply();

        return altitudeMapTex;
    }

    public void GetDebugBiomeMaps(List<Biome> biomes, IntVector2 offset)
    {
        biomeMaps = new List<Texture2D>();
        
        foreach (var biome in biomes)
        {
            Color[] biomeHeightMap = new Color[heightmap.Length];
            Color[] biomeWeightMap = new Color[heightmap.Length];

            int lowest = (int)minypos.x + (int)minypos.y * mapverts;

            for (int i = 0; i < heightmap.Length; ++i)
            {
                int y = i / mapverts; // integer division, we want the truncated int here
                int x = i % mapverts; // we want the column of the current row

                float height = biome.Height01(x, y, offset);
                biomeHeightMap[i] = new Color(height, height, height);
                
                
                float weight = biome.Weight(x, y, offset);
                biomeWeightMap[i] = new Color(weight, weight, weight);
            }
        
            Texture2D debugHeightMap = new Texture2D(mapverts, mapverts);
            debugHeightMap.SetPixels(biomeHeightMap);
            debugHeightMap.Apply();
            
            Texture2D debugWeightMap = new Texture2D(mapverts, mapverts);
            debugWeightMap.SetPixels(biomeWeightMap);
            debugWeightMap.Apply();
            
            biomeMaps.Add(debugHeightMap);
            biomeMaps.Add(debugWeightMap);
        }
        
        // -------------------------------------------------------------------------------------------------------------
        Color[] heightMap = new Color[heightmap.Length];

        for (int i = 0; i < heightmap.Length; ++i)
        {
            int y = i / mapverts; // integer division, we want the truncated int here
            int x = i % mapverts; // we want the column of the current row

            if (heightmap[x, y] > 1) heightMap[i] = new Color(255, 0, 0);
            else if (heightmap[x, y] < 0) heightMap[i] = new Color(0, 255, 0);
            else heightMap[i] = new Color(heightmap[x, y], heightmap[x, y], heightmap[x, y]);   
        }
        
        Texture2D debugTerrainHeightMap = new Texture2D(mapverts, mapverts);
        debugTerrainHeightMap.SetPixels(heightMap);
        debugTerrainHeightMap.Apply();
        
        biomeMaps.Add(debugTerrainHeightMap);
        
        // -------------------------------------------------------------------------------------------------------------
        Color[] waterMap = new Color[heightmap.Length];

        for (int i = 0; i < heightmap.Length; ++i)
        {
            int y = i / mapverts; // integer division, we want the truncated int here
            int x = i % mapverts; // we want the column of the current row
            
            if ( watermap[x, y] ) heightMap[i] = new Color(0, 0, 255, 255);
            else waterMap[i] = new Color(0, 0, 0, 255);
        }
        
        Texture2D debugWaterMap = new Texture2D(mapverts, mapverts);
        debugWaterMap.SetPixels(waterMap);
        debugWaterMap.Apply();
        
        biomeMaps.Add(debugWaterMap);
    }
    
    public float SampleOctavePerlinNoise(int x, int z, int xoffset, int zoffset, float chunkSize)
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

    public float MapMinRemapped(float min, float max, AnimationCurve curve)
    {
        return Utilities.Remap(curve.Evaluate(miny), 0, 1, min, max);
    }

    public float MapMaxRemapped(float min, float max, AnimationCurve curve)
    {
        return Utilities.Remap(curve.Evaluate(maxy), 0, 1, min, max);
    }

    public float MapMax()
    {
        return maxy;
    }
    
    public float MapMin()
    {
        return miny;
    }

    public Vector3 MapMinPosition()
    {
        return new Vector3(minypos.x, heightmap[(int)minypos.x, (int)minypos.y], minypos.y);
    }
    
    public Vector3 MapMaxPosition()
    {
        return new Vector3(maxypos.x, heightmap[(int)maxypos.x, (int)maxypos.y], maxypos.y);
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
    
    // STATIC FUNCTIONS ------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    
    public static Vector3 ScaleMapPosition(Vector3 mapPos, int verts, float vertScale, AnimationCurve curve, float remapMin, float remapMax, IntVector2 gridOffset)
    {
        float height = curve.Evaluate(mapPos.y);
        height = Utilities.Remap(height, 0, 1, remapMin, remapMax);
        return new Vector3(mapPos.x * vertScale, height, mapPos.z * vertScale);
    }
    
    public static Vector3 ScaleMapPosition(Vector3 mapPos, float vertScale, float maxHeight)
    {
        float height = mapPos.y * maxHeight;
        return new Vector3(mapPos.x * vertScale, height, mapPos.z * vertScale);
    }
    
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------------------------
    
    public struct TerrainMapIJob : IJob
    {
        // public MapGenerationInput generationInput;
        
        // MapGenerationInput mapGenerationData = new MapGenerationInput(mapverts, mapsquares, offset, chunkSize, octaves);

        private NativeArray<float> map;
        private NativeArray<float> min;
        private NativeArray<float> max;
        private int mapverts;
        private int mapsquares;
        private int2 offset;
        private float chunkSize;
        private int octaves;

        struct OctaveData
        {
            public NativeArray<float> amplitudes;
            public NativeArray<float> frequencies;
            public NativeArray<float2> seeds;
            public float max;
        }

        public TerrainMapIJob(NativeArray<float> map, NativeArray<float> min, NativeArray<float> max, int mapverts, Vector2 offset, float chunkSize, int octaves)
        {
            this.map = map;
            this.min = min;
            this.max = max;
            this.mapverts = mapverts;
            mapsquares = mapverts - 1;
            this.offset.x = (int)offset.x;
            this.offset.y = (int)offset.y;
            this.chunkSize = chunkSize;
            this.octaves = octaves;
        }
        
        public void Execute()
        {
            // generate octave frequencies and amplitudes of doing octave noise
            OctaveData octaveData = CalculateOctaves(octaves);
            
            // Debug.Log("Octave data for this thread: Amplitudes[" + Utilities.ArrayToString(octaveData.Amplitudes) + "], Frequencies[" + Utilities.ArrayToString(octaveData.Frequencies) + "], " + octaveData.MAXSampleValue);

            // init the min and max values so we can remap later
            max[0] = float.MinValue;
            min[0] = float.MaxValue;

            // convert these to ints now so we dont have to do it every loop
            int xoffset = (int) offset.x;
            int zoffset = (int) offset.y;
        
            // create vertices
            for (int x = 0; x < mapverts; ++x)
            {
                for (int z = 0; z < mapverts; ++z)
                {
                    float y = SampleNoise(x, z, mapsquares, xoffset, zoffset, chunkSize, octaveData);

                    if (y < min[0]) min[0] = y;
                    if (y > max[0]) max[0] = y;

                    map[z + x * mapverts] = y;
                }
            }

            octaveData.amplitudes.Dispose();
            octaveData.frequencies.Dispose();
            octaveData.seeds.Dispose();
        }

        private OctaveData CalculateOctaves(int octaves)
        {
            OctaveData octaveData = new OctaveData();
            
            octaveData.amplitudes = new NativeArray<float>(octaves, Allocator.Temp);
            octaveData.frequencies = new NativeArray<float>(octaves, Allocator.Temp);
            octaveData.seeds = new NativeArray<float2>(octaves, Allocator.Temp);

            octaveData.amplitudes[0] = 1;
            octaveData.frequencies[0] = 1;
            octaveData.max = 1;
        
            for (int i = 1; i < octaves; ++i)
            {
                // get the power of 2 that will be used for this octave
                float fact = (float) Mathf.Pow(2, i);
            
                // set the octave amplitude and increment the max value
                octaveData.amplitudes[i] = 1.0f / fact;
                octaveData.max += octaveData.amplitudes[i];
            
                // set the octave frequency
                octaveData.frequencies[i] = fact;
                octaveData.seeds[i] = new float2(UnityEngine.Random.Range(0, 100000), UnityEngine.Random.Range(0, 100000));
            }

            return octaveData;
        }

        private float SampleNoise(int x, int z, int mapsquares, int xoffset, int zoffset, float chunkSize, OctaveData octaveData)
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

            for (int i = 0; i < octaveData.amplitudes.Length; ++i)
            {
                float perlin = Mathf.PerlinNoise(octaveData.frequencies[i] * xSamplePoint + octaveData.seeds[i].x, octaveData.frequencies[i] * zSamplePoint + octaveData.seeds[i].y);
                float clamped = Mathf.Clamp01(perlin);
                float sample = octaveData.amplitudes[i] * clamped;

                y += sample;
                y += sample;
            }

            Debug.Log("MAP OCTAVE MAX: " + octaveData.max);
            y /= octaveData.max; // normalize the result

            return y;

            // return Remap(y, 0, 1, remapMin, remapMax);
        }
    }
}
