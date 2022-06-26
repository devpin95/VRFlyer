using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain.Threading
{
    public class TerrainMapGeneratorThread
    {
        // TerrainMapThreaded will enable the generation of a 2D noise map on a thread instead of on the main Unity thread
        // and will improve performance when a new map needs to be generated
        private MapGenerationInput generationInput;
        private Action<MapGenerationOutput, bool> _callback;
        private Queue<MapGenerationOutput> _resultQ;

        public TerrainMapGeneratorThread(MapGenerationInput input, Action<MapGenerationOutput, bool> callback, Queue<MapGenerationOutput> q)
        {
            generationInput = input;
            _callback = callback;
            _resultQ = q;
        }

        public void ThreadProc()
        {
            // create a new object to hold the result of the map generation
            MapGenerationOutput output = new MapGenerationOutput();
            output.map = new float[generationInput.mapverts, generationInput.mapverts];
            output.water = new bool[generationInput.mapverts, generationInput.mapverts];
            output.callback = _callback;
            output.mean = 0;
            output.attributes = new TerrainMap.TerrainAttributes();
            output.attributes.conditions = new bool[3];

            // init the min and max values so we can remap later
            output.max = float.MinValue;
            output.min = float.MaxValue;
            output.maxpos = Vector2.negativeInfinity;
            output.minpos = Vector2.negativeInfinity;

            // convert these to ints now so we dont have to do it every loop
            // int xoffset = generationInput.offset.x;
            // int zoffset = generationInput.offset.y;

            // create vertices
            for (int x = 0; x < generationInput.mapverts; ++x)
            {
                for (int z = 0; z < generationInput.mapverts; ++z)
                {
                    float y = Biome.AccumulateBiomes01(x, z, generationInput.offset, generationInput.biomes, generationInput.maxHeight);
                    // float y = Noise.SampleOctavePerlinNoise(x, z, generationInput.mapsquares, xoffset, zoffset, generationInput.chunkSize, octaveData);

                    if (y < output.min)
                    {
                        output.min = y;
                        output.minpos.x = x;
                        output.minpos.y = z;
                    }

                    if (y > output.max)
                    {
                        output.max = y;
                        output.maxpos.x = x;
                        output.maxpos.y = z;
                    }

                    output.mean += y;
                    
                    output.map[x, z] = y;
                }
            }

            output.mean /= output.map.Length;
            float sqrdDiffSum = 0;
            
            // second pass, calculate the variance given the median
            for (int x = 0; x < generationInput.mapverts; ++x)
            {
                for (int z = 0; z < generationInput.mapverts; ++z)
                {
                    float diff = output.map[x, z] - output.mean;
                    sqrdDiffSum += diff * diff;
                }
            }

            output.variance = sqrdDiffSum / (output.map.Length - 1);
            output.stdDev = Mathf.Sqrt(output.variance);

            if (generationInput.attributes.conditions[(int)TerrainMap.Attributes.HAS_LAKE] && output.variance < TerrainInfo.PLakeVarianceThreshold)
            {
                output.attributes.conditions[(int)TerrainMap.Attributes.HAS_LAKE] = true;
                
                output.mean = 0;
                float minWeight = float.MaxValue;
                float maxWeight = float.MinValue;
                output.max = float.MinValue;
                output.min = float.MaxValue;
                output.maxpos = Vector2.negativeInfinity;
                output.minpos = Vector2.negativeInfinity;

                for (int x = 0; x < generationInput.mapverts; ++x)
                {
                    for (int z = 0; z < generationInput.mapverts; ++z)
                    {
                        float weight = 1;
                        switch (generationInput.attributes.lakeTypes[0])
                        {
                            case 0:
                                weight = Classes.DistanceFunctions.SquareBump.Distance(x, z, generationInput.mapverts - 1, generationInput.mapverts - 1, 
                                    Classes.DistanceFunctions.SquareBump.defaultOptsArray);
                                break;
                            case 1:
                                weight = Classes.DistanceFunctions.DistanceSquared.Distance(x, z, generationInput.mapverts, generationInput.mapverts);
                                break;
                            case 2:
                                weight = Classes.DistanceFunctions.Hyperboloid.Distance(x, z, generationInput.mapverts, generationInput.mapverts);
                                break;
                            case 3:
                                weight = Classes.DistanceFunctions.TrigProduct.Distance(x, z, generationInput.mapverts, generationInput.mapverts);
                                break;
                        }

                        if (weight > maxWeight) maxWeight = weight;
                        if (weight < minWeight) minWeight = weight;

                        output.map[x, z] *= weight;
                        
                        if (output.map[x, z] < output.min)
                        {
                            output.min = output.map[x, z];
                            output.minpos.x = x;
                            output.minpos.y = z;
                        }

                        if (output.map[x, z] > output.max)
                        {
                            output.max = output.map[x, z];
                            output.maxpos.x = x;
                            output.maxpos.y = z;
                        }
                        
                        output.mean += output.map[x, z];
                    }
                }
                
                Debug.Log("Weights [" + minWeight + ", " + maxWeight + "]");
                
                output.mean /= output.map.Length;
                sqrdDiffSum = 0;
            
                // second pass, calculate the variance given the median
                for (int x = 0; x < generationInput.mapverts; ++x)
                {
                    for (int z = 0; z < generationInput.mapverts; ++z)
                    {
                        float diff = output.map[x, z] - output.mean;
                        sqrdDiffSum += diff * diff;
                    }
                }
                
                output.variance = sqrdDiffSum / (output.map.Length - 1);
                output.stdDev = Mathf.Sqrt(output.variance);
                output.waterElevation = output.min + output.variance;
                
                for (int x = 0; x < generationInput.mapverts; ++x)
                {
                    for (int z = 0; z < generationInput.mapverts; ++z)
                    {
                        if (output.map[x, z] < output.min + output.variance)
                        {
                            output.water[x, z] = true;
                        }
                    }
                }
            }

            // put the result in the locked queue so that whoever needs it can access it
            lock (_resultQ)
            {
                _resultQ.Enqueue(output);   
            }
        }
        
        public struct MapGenerationInput
        {
            // this class holds the information needed to generate a 2D map of octave perlin noise
        
            public IntVector2 offset;
            public int mapverts;
            public List<Biome> biomes;
            public float maxHeight;
            public TerrainMap.TerrainAttributes attributes;
        
            public MapGenerationInput(int mapverts, IntVector2 offset, List<Biome> biomes, TerrainMap.TerrainAttributes attributes, float maxHeight)
            {
                this.mapverts = mapverts;
                this.offset = offset;
                this.biomes = biomes;
                this.attributes = attributes;
                this.maxHeight = maxHeight;
            }
        }

        public struct MapGenerationOutput
        {
            // this class holds the 2D map of octave perlin noise generated by the Noise class
            public float[,] map;
            public bool[,] water;
            public float waterElevation;
            public float min;
            public float max;
            public Vector2 minpos;
            public Vector2 maxpos;
            public float mean;
            public float variance;
            public float stdDev;
            public Action<MapGenerationOutput, bool> callback;
            public TerrainMap.TerrainAttributes attributes;
        }
    }   
}
