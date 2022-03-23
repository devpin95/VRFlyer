using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

[BurstCompile]
public struct BuildAdvancedVertexUVJob : IJobParallelFor
{
    [ReadOnly] public int dim;
    [ReadOnly] public float vertexScale;
    [ReadOnly] [NativeDisableParallelForRestriction] public NativeArray<float> map;
    [ReadOnly] public int LOD;
    public Mesh.MeshData meshData;
    
    public void Execute(int index)
    {
        float fdim = (float) dim;
        int y = index / dim; // integer division, we want the truncated int here
        int x = index % dim; // we want the column of the current row
        
        NativeArray<float3> verts = meshData.GetVertexData<float3>();
        NativeArray<float2> uvs = meshData.GetVertexData<float2>(stream: 1);

        verts[index] = float3(x * vertexScale, map[index], y * vertexScale);
        uvs[index] = float2(x / fdim, y / fdim);
    }
}
