using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class LODUtility
{
    public static TerrainInfo.LODInfo HighestLOD(TerrainInfo info)
    {
        // returns the LOD with the highest resolution
        TerrainInfo.LODInfo highestLOD;
        highestLOD.lod = Int32.MaxValue;
        highestLOD.distance = Int32.MaxValue;
        
        foreach (var level in info.lods)
        {
            if (level.lod < highestLOD.lod)
            {
                highestLOD = level;
            }
        }

        return highestLOD;
    }
    
    public static TerrainInfo.LODInfo LowestLOD(TerrainInfo info)
    {
        // returns the LOD with the lowest resolution
        TerrainInfo.LODInfo lowestLOD;
        lowestLOD.lod = Int32.MinValue;
        lowestLOD.distance = Int32.MinValue;
        
        foreach (var level in info.lods)
        {
            if (level.lod > lowestLOD.lod)
            {
                lowestLOD = level;
            }
        }

        return lowestLOD;
    }

    public static int LODToMeshResolution(TerrainInfo.LODInfo lodInfo)
    {
        return lodInfo.lod == 0 ? 1 : lodInfo.lod * 2;
    }
    
    public static int LODToMeshResolution(int lod)
    {
        return lod == 0 ? 1 : lod * 2;
    }

    public static int MeshResolutionToLOD(int res)
    {
        return res == 1 ? 0 : res / 2;
    }

    public static int MaxLODCount()
    {
        return 7; // possible LODs: 0-6
    }
}
