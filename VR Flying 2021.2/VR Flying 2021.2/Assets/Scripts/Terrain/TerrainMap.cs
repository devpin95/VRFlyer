using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Schema;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class TerrainMap
{
    public static bool ALTITUDE_BELOW(float rhs, float lhs) { return rhs <= lhs; }
    public static bool ALTITUDE_ABOVE(float rhs, float lhs) { return rhs >= lhs; }
    
    public static AnimationCurve defaultHeightCurve = AnimationCurve.Constant(0, 1, 1f);
    
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
        
        maxy = float.MinValue;
        miny = float.MaxValue;
    }

    public void SetMap(float[,] map, float min, float max)
    {
        if (map.GetLength(0) != mapverts)
        {
            Debug.LogError("Map does not match the dimensions of the current object");
            return;
        }
        
        heightmap = map;
        maxy = max;
        miny = min;
    }
    
    public void RequestMap(Vector2 offset, float chunkSize, Action<MapGenerationData> callback, Queue<MapGenerationData> queue, int octaves = 5)
    {
        // this function will package map generation info together into an object that will be stored in an object
        // with a function to be run in a thread. The result of the thread will be send to the callback action passed
        // in to this function
        
        
        // package the generation info together and make a new threaded terrain map object to hold that info
        MapGenerationInfo mapGenerationData = new MapGenerationInfo(mapverts, mapsquares, offset, chunkSize, octaves);
        
        // set up the thread data with the generation info, the callback to put on the queue, and the queue itself
        TerrainMapThreaded tmt = new TerrainMapThreaded(mapGenerationData, callback, queue);

        // make a delegate that will call the threaded terrain map function
        ThreadStart threadStart = delegate { tmt.ThreadProc(); };

        // make a new thread and call it to start the threaded terrain generation in the TerrainMapThreaded class
        Thread thread = new Thread(threadStart);
        thread.Start();
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
    
    public Vector3[] GetRemappedFlattenedVectorMap(float vertScale, float min, float max, AnimationCurve curve)
    {
        AnimationCurve animCurve = new AnimationCurve(curve.keys);
        
        Vector3[,] vectormap = new Vector3[mapverts, mapverts];
        for (int z = 0; z < mapverts; ++z)
        {
            for (int x = 0; x < mapverts; ++x)
            {
                float y = Utilities.Remap(animCurve.Evaluate(heightmap[x, z]), 0, 1, min, max);
                vectormap[x, z] = new Vector3(x * vertScale, y, z * vertScale);
            }
        }

        return Utilities.Flatten2DArray(vectormap, mapverts, mapverts);
    }

    public Texture2D GetHeightMapTexture2D(AnimationCurve curve = null)
    {
        if (curve == null) curve = defaultHeightCurve;
        
        float[] flatmap = GetFlattenedHeightMap();
        Color[] colormap = new Color[heightmap.Length];

        for (int i = 0; i < flatmap.Length; ++i)
        {
            float curvedValue = curve.Evaluate(flatmap[i]);
            colormap[i] = new Color(curvedValue, curvedValue, curvedValue, 1);
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

    public struct MapGenerationInfo
    {
        // this class holds the information needed to generate a 2D map of octave perlin noise
        
        public Vector2 offset;
        public float chunkSize;
        public int mapverts;
        public int mapsquares;
        public int octaves;
        
        public MapGenerationInfo(int mapverts, int mapsquares, Vector2 offset, float chunkSize, int octaves)
        {
            this.mapverts = mapverts;
            this.mapsquares = mapsquares;
            this.offset = offset;
            this.chunkSize = chunkSize;
            this.octaves = octaves;
        }
    }

    public struct MapGenerationData
    {
        // this class holds the 2D map of octave perlin noise generated by the Noise class
        public float[,] map;
        public float min;
        public float max;
        public Action<MapGenerationData> callback;
    }

    private class TerrainMapThreaded
    {
        // TerrainMapThreaded will enable the generation of a 2D noise map on a thread instead of on the main Unity thread
        // and will improve performance when a new map needs to be generated
        private MapGenerationInfo _generationInfo;
        private Action<MapGenerationData> _callback;
        private Queue<MapGenerationData> _resultQ;

        public TerrainMapThreaded(MapGenerationInfo info, Action<MapGenerationData> callback, Queue<MapGenerationData> q)
        {
            _generationInfo = info;
            _callback = callback;
            _resultQ = q;
        }

        public void ThreadProc()
        {
            // create a new object to hold the result of the map generation
            MapGenerationData data = new MapGenerationData();
            data.map = new float[_generationInfo.mapverts, _generationInfo.mapverts];
            data.callback = _callback;
            
            // generate octave frequencies and amplitudes of doing octave noise
            Noise.OctaveData octaveData = Noise.GenerateOctaveData(_generationInfo.octaves);
            
            Debug.Log("Octave data for this thread: Amplitudes[" + Utilities.ArrayToString(octaveData.Amplitudes) + "], Frequencies[" + Utilities.ArrayToString(octaveData.Frequencies) + "], " + octaveData.MAXSampleValue);

            // init the min and max values so we can remap later
            data.max = float.MinValue;
            data.min = float.MaxValue;

            // convert these to ints now so we dont have to do it every loop
            int xoffset = (int) _generationInfo.offset.x;
            int zoffset = (int) _generationInfo.offset.y;
        
            // create vertices
            for (int x = 0; x < _generationInfo.mapverts; ++x)
            {
                for (int z = 0; z < _generationInfo.mapverts; ++z)
                {
                    float y = Noise.SampleOctavePerlinNoise(x, z, _generationInfo.mapsquares, xoffset, zoffset, _generationInfo.chunkSize, octaveData);

                    if (y < data.min) data.min = y;
                    if (y > data.max) data.max = y;

                    data.map[x, z] = y;
                }
            }

            // put the result in the locked queue so that whoever needs it can access it
            lock (_resultQ)
            {
                _resultQ.Enqueue(data);   
            }

            // Debug.Log("Min: " + miny + " Max: " + maxy);

            // !!!!!!!
            // We can't actually do the stretching here since we are generating blocks independently. Doing it here
            // means that each block is getting stretched by it's own local min and max value and will make the edges
            // between each block not match
            // !!!!!!!

            // heightmap = HydraulicErosion.Simulate(heightmap, scale, min, max);
        }
    }
}
