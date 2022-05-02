using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

// [BurstCompile]
public struct BuildAdvancedVertexJob : IJobParallelFor
{
    [ReadOnly] public int dim;
    [ReadOnly] public float vertexScale;
    [ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<float> map;
    [ReadOnly] public int LOD;
    [ReadOnly] public float2 remap;

    [WriteOnly] [NativeDisableContainerSafetyRestriction] [NativeDisableParallelForRestriction] public NativeArray<float3> verts;

    public void Execute(int index)
    {
        int y = (index / dim) * LOD; // integer division, we want the truncated int here
        int x = (index % dim) * LOD; // we want the column of the current row

        // float height = Utilities.Remap(map[index], 0, 1, remap.x, remap.y);
        verts[index] = float3(x * vertexScale, map[index] * remap.y, y * vertexScale);
    }
}
