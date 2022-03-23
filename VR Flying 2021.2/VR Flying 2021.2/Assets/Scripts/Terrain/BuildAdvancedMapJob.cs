using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

// https://catlikecoding.com/unity/tutorials/procedural-meshes/creating-a-mesh/
// https://www.raywenderlich.com/7880445-unity-job-system-and-burst-compiler-getting-started

[BurstCompile]
public struct BuildAdvancedMapJob : IJobParallelFor
{
    [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<float> map;
    [ReadOnly] public int dim;
    [ReadOnly] public int2 offset;
    [ReadOnly] public float chunkSize;

    [ReadOnly] public NativeArray<float> amplitudes; // 1/2^n
    [ReadOnly] public NativeArray<float> frequencies; // 2^n
    [ReadOnly] public float octaveMax; // sum of amplitudes
    
    public void Execute(int index)
    {
        int y = index / dim; // integer division, we want the truncated int here
        int x = index % dim; // we want the column of the current row
        map[index] = SamplePerlinNoise(new int2(x, y));
    }

    private float SamplePerlinNoise(int2 pos)
    {
        float minXSampleRange = offset.x * chunkSize;
        float minZSampleRange = offset.y * chunkSize;

        float xSamplePoint = Utilities.Remap(pos.x, 0, dim - 1, minXSampleRange, minXSampleRange + chunkSize);
        float zSamplePoint = Utilities.Remap(pos.y, 0, dim - 1, minZSampleRange, minZSampleRange + chunkSize);

        float2 samplePoints = new float2(xSamplePoint, zSamplePoint);

        float sample = 0;
        
        for (int i = 0; i < amplitudes.Length; ++i)
        {
            sample += noise.snoise(samplePoints * new float2(frequencies[i])) * amplitudes[i];
        }

        return sample / octaveMax; // return the normalized sample
    }

    // private void GenerateLODMeshes(NativeArray<float> map)
    // {
    //     int meshsquares = dim - 1; // the number of squares in each dimension
    //     int vertexAttributeCount = 2;
    //     
    //     var vertexAttributes = new NativeArray<VertexAttributeDescriptor>(vertexAttributeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
    //     VertexAttributeDescriptor vertexPositionStream = new VertexAttributeDescriptor(VertexAttribute.Position, dimension: 3);
    //     VertexAttributeDescriptor vertexUvStream = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2, stream: 1);
    //     vertexAttributes[0] = vertexPositionStream;
    //     vertexAttributes[1] = vertexUvStream;
    //
    //     int lod = 0;
    //     
    //     // int vertexCount = meshsquares / (lod == 0 ? 1 : lod * 2); // get the vertex count for this LOD, for LOD0 just dim, otherwise meshsquares/(lod * 2)
    //     int vertexCount = dim * dim;
    //     
    //     // set vertex buffer params
    //     meshDataArray[lod].SetVertexBufferParams(vertexCount, vertexAttributes);
    //     
    //     // set the vertex positions and UVs ------------------------------------------------------------------------
    //     NativeArray<float3> vertices = meshDataArray[lod].GetVertexData<float3>();
    //     // NativeArray<float2> uvs = meshDataArray[lod].GetVertexData<float2>(1);
    //
    //     float fdim = (float) dim;
    //     
    //     for (int y = 0; y < dim; y += lod)
    //     {
    //         for (int x = 0; x < dim; x += lod)
    //         {
    //             int mapoffset = x + y * dim; // index into the flat map array
    //             // vertices[mapoffset] = float3(x, map[mapoffset], y); // set the vertex position
    //             // uvs[mapoffset] = float2(x / fdim, y / fdim); // set the vertex UV
    //         }
    //     }
    //     //
    //     // // Set the triangles of the mesh ---------------------------------------------------------------------------
    //     // int triangleIndexCount = (meshsquares * 2) * meshsquares; // the number of triangles in the first row * number of rows
    //     // meshDataArray[lod].SetIndexBufferParams(triangleIndexCount, IndexFormat.UInt32);
    //     // var triangleIndices = meshDataArray[lod].GetIndexData<uint>();
    //     //
    //     // int triIndex = 0;
    //     // for (uint y = 0; y < meshsquares; ++y)
    //     // {
    //     //     uint mapoffset = y * (uint)(meshsquares + 1); // offset
    //     //     for (uint x = 0; x < meshsquares; ++x)
    //     //     {
    //     //         uint bl = x + mapoffset;
    //     //         uint tl = x + (uint)meshsquares + mapoffset + 1;
    //     //         uint tr = x + (uint)meshsquares + mapoffset + 2;
    //     //         uint br = x + mapoffset + 1;
    //     //
    //     //         // left tri
    //     //         triangleIndices[triIndex] = tl;
    //     //         triangleIndices[triIndex + 1] = br;
    //     //         triangleIndices[triIndex + 2] = bl;
    //     //
    //     //         // right tri
    //     //         triangleIndices[triIndex + 3] = tl;
    //     //         triangleIndices[triIndex + 4] = tr;
    //     //         triangleIndices[triIndex + 5] = br;
    //     //
    //     //         triIndex += 6;
    //     //     }
    //     // }
    //
    //     vertexAttributes.Dispose();
    // }
}
