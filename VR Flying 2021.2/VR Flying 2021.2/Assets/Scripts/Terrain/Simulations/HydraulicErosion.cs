using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HydraulicErosion
{
    private const float HydraulicParticleInitialVolume = 1f;

    private class Constants
    {
        public static float maxDropSpeed = 2f;
        public static int maxDropIterations = 80;
        public static int maxSpeedThreshold = 2;
        public static float NeighborWeight = 0.15f;
        public static float DiagonalWeight = 0.1f;
    }

    private static class Parameters
    {
        public static float DT = 1.2f;
        public static int DropCount = 200000;
        public static float StartingVolume = 1f;
        public static float MINVolume = 0.01f;
        public static float Density = 1f;
        public static float Friction = 0.05f;
        public static float DepositeRate = 0.05f;
        public static float EvaporationRate = 0.038f;
        public static bool FlipXY = false;
    }
    
    
    public class HydraulicParticle
    {
        public HydraulicParticle(Vector2 npos) {pos = npos;}
        
        public Vector2 pos = Vector2.zero;
        public Vector2 speed = Vector2.zero;
        public float volume = HydraulicParticleInitialVolume;
        public float sediment = 0;
    }

    public static float[,] Simulate(float[,] map)
    {
        // https://github.com/weigert/SimpleErosion/blob/master/source/include/world/world.cpp

        int mapdim = map.GetLength(0) - 1;
        
        // Debug.Log(parameters.FlipXY);

        for (int drops = 0; drops < Parameters.DropCount; ++drops)
        {
            if (drops % 2000 == 0)
            {
                Debug.Log("Drop " + drops);
            }

            HydraulicParticle drop = new HydraulicParticle(Vector2.zero);

            float x = Random.Range(0f, mapdim);
            float y = Random.Range(0f, mapdim);
            
            drop.pos.x = x;
            drop.pos.y = y;
            drop.speed.x = 0;
            drop.speed.y = 0;
            drop.sediment = 0;
            drop.volume = Parameters.StartingVolume;

            int iteration = 0;
            int maxspeedcount = 0;

            while (drop.volume > Parameters.MINVolume && iteration < Constants.maxDropIterations)
            {
                // make sure the drop is still in the bounds of the mesh
                if ( !SampleInBounds(map, drop.pos.x, drop.pos.y ) ) break;
                // if (map.SampleOutOfBounds(drop.pos.x, drop.pos.y)) break; // old

                Vector2 initialPos = drop.pos;
                Vector3 norm;
                
                norm = SampleBetaNormalAtXY(map, (int)initialPos.x, (int)initialPos.y);

                Vector2 forceVector;

                if (Parameters.FlipXY) forceVector = new Vector2(norm.z, norm.x);
                else forceVector = new Vector2(norm.x, norm.z);

                Vector2 F = Parameters.DT * forceVector; // force
                float m = (drop.volume * Parameters.Density); // mass

                // a = F/m
                drop.speed += F / m;

                // update pos
                drop.pos += Parameters.DT * drop.speed;

                if ( !SampleInBounds(map, drop.pos.x, drop.pos.y ) ) break;
                // if (map.SampleOutOfBounds(drop.pos.x, drop.pos.y)) break; // old

                // apply friction
                drop.speed *= 1f - Parameters.DT * Parameters.Friction;

                if (drop.speed.magnitude > Constants.maxDropSpeed)
                {
                    ++maxspeedcount;
                    drop.speed = drop.speed.normalized * Constants.maxDropSpeed * drop.volume;
                }

                if (maxspeedcount > Constants.maxSpeedThreshold) break;

                // if ( drop.speed.magnitude > 1 ) Debug.Log("BIG SPEED BOIIIIII");

                // figure out sediment levels
                // the height at our initial position
                float prevHeight = map[(int) initialPos.x, (int) initialPos.y];

                // the height at our current position
                float curHeight = map[(int) initialPos.x, (int) initialPos.y];
                
                float travelDistance = drop.speed.magnitude * (prevHeight - curHeight);
                float maxsed = drop.volume * travelDistance;

                if (maxsed < 0) maxsed = 0f;
                float seddiff = maxsed - drop.sediment;

                // change the map based on the sediment
                drop.sediment += Parameters.DT * Parameters.DepositeRate * seddiff;

                float amount = Parameters.DT * drop.volume * Parameters.DepositeRate * seddiff;
                
                map[(int)initialPos.x, (int)initialPos.y] -= amount;
                // map.ChangeNode((int)initialPos.x, (int)initialPos.y, amount); // old
                
                // do some evaporation
                drop.volume *= 1f - Parameters.DT * Parameters.EvaporationRate;

                ++iteration;
            }
        }

        return map;
    }
    
    private static Vector3 SampleBetaNormalAtXY(float[,] map, int row, int col)
    {
        int meshEdge = map.GetLength(0) - 1;
        Vector3 normal = Vector3.zero;

        // direct neighbors
        // up
        if (row + 1 < meshEdge) 
            normal += Vector3.Normalize(new Vector3(map[row, col] - map[row + 1, col], 1f, 0)) * Constants.NeighborWeight;
        
        // down
        if (row - 1 >= 0) 
            normal += Vector3.Normalize(new Vector3(map[row - 1, col] - map[row, col], 1f, 0)) * Constants.NeighborWeight;
        
        // right
        if ( col + 1 < meshEdge ) 
            normal += Vector3.Normalize(new Vector3(0, 1f, map[row, col] - map[row, col + 1])) * Constants.NeighborWeight;
        
        // left
        if ( col - 1 >= 0 ) 
            normal += Vector3.Normalize(new Vector3(0, 1f, map[row, col-1] - map[row, col])) * Constants.NeighborWeight;
        
        // diagonals
        float sqrt2 = Mathf.Sqrt(2);
        
        // up right
        if (row + 1 < meshEdge && col + 1 < meshEdge)
        {
            float val = map[row, col] - map[row + 1, col + 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * Constants.DiagonalWeight;
        }

        // up left
        if (row + 1 < meshEdge && col - 1 >= 0)
        {
            float val = map[row, col] - map[row + 1, col - 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * Constants.DiagonalWeight;
        }
        
        // down right
        if (row - 1 >= 0 && col + 1 < meshEdge)
        {
            float val = map[row, col] - map[row - 1, col + 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * Constants.DiagonalWeight;
        }
        
        // down left
        if (row - 1 >= 0 && col - 1 >= 0)
        {
            float val = map[row, col] - map[row - 1, col - 1];
            normal += Vector3.Normalize(new Vector3(val/sqrt2, sqrt2, val/sqrt2)) * Constants.DiagonalWeight;
        }

        return normal;
    }

    private static bool SampleInBounds(float[,] map, float x, float y)
    {
        int dim = map.GetLength(0);
        
        if (x >= dim || y >= dim) return false;
        if (x < 0 || y < 0) return false;

        return true;
    }
}
