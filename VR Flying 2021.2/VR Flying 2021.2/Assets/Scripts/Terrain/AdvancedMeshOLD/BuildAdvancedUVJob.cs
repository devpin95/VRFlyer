using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

// [BurstCompile]
public struct BuildAdvancedUVJob : IJobParallelFor
{
    [WriteOnly] [NativeDisableContainerSafetyRestriction] [NativeDisableParallelForRestriction] public NativeArray<float2> uvs;
    [ReadOnly] public int dim;
    [ReadOnly] public float vertexScale;
    [ReadOnly] public int LOD;
    
    public void Execute(int index)
    {
        int y = (index / dim) * LOD; // integer division, we want the truncated int here
        int x = (index % dim) * LOD; // we want the column of the current row
        uvs[index] = float2(x, y) * vertexScale;
    }
}
