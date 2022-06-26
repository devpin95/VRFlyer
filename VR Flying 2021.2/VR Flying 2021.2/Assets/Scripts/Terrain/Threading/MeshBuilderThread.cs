using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


namespace Terrain.Threading
{
    public class MeshBuilderThread
    {
        public enum MeshDataStreams : int
        {
            Vertices = 0,
            UVs = 1
        }
        
        private MeshGenerationInput meshInfo;
        private Queue<MeshGenerationOutput> resultQ;
        private Action<MeshGenerationOutput, bool> callback;
        private MeshGenerationOutput meshGenerationOutput;
        private NativeArray<VertexAttributeDescriptor> meshVertexAttributes; 
        
        public MeshBuilderThread(ref MeshGenerationInput meshInfo, Queue<MeshGenerationOutput> resultQ, Action<MeshGenerationOutput, bool> callback)
        {
            this.meshInfo = meshInfo;
            this.resultQ = resultQ;
            this.callback = callback;
        }

        public void ThreadProc()
        {
            meshGenerationOutput = new MeshGenerationOutput();
            meshGenerationOutput.callback = callback;
            meshGenerationOutput.meshDataArray = meshInfo.meshDataArray;
            meshGenerationOutput.targetLod = LODUtility.MeshResolutionToLOD(meshInfo.resolution);
            // meshGenerationOutput.SetMeshDataArray(ref meshDataArray);

            // Debug.Log("Mesh Done");
            
            SetVertexDescriptors();
            int targetLOD = LODUtility.MeshResolutionToLOD(meshInfo.resolution);
            Mesh.MeshData data = meshInfo.meshDataArray[targetLOD];
            data.SetVertexBufferParams(meshInfo.map.GetVertexArraySizeAtLOD(meshInfo.resolution), meshVertexAttributes);
            data.SetIndexBufferParams(meshInfo.map.GetIndexArraySizeAtLOD(meshInfo.resolution), IndexFormat.UInt32);
            meshVertexAttributes.Dispose();
            
            
            // CreateVerts();
            CreateVertsNative(data);
            // CreateTris();
            CreateTrisNative(data);
            // CreateUVs();
            CreateUVsNative(data);

            // Debug.Log("Mesh will have verts[" + meshInfo.dim * meshInfo.dim + "] tris[" + (meshInfo.dim - 1) * (meshInfo.dim - 1) + "] uvs[" + meshGenerationOutput.uvs.Length + "]");

            data.subMeshCount = 1;
            MeshUpdateFlags smflags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;
            SubMeshDescriptor smdes = new SubMeshDescriptor(0, meshInfo.map.GetIndexArraySizeAtLOD(meshInfo.resolution));
            data.SetSubMesh(0, smdes, smflags);

            lock (resultQ) resultQ.Enqueue(meshGenerationOutput);
        }
        
        private void CreateVerts()
        {
            Debug.Log(meshInfo.map);
            meshGenerationOutput.vertices = meshInfo.map.GetRemappedFlattenedVectorMap(
                    vertScale: meshInfo.vertexScale, 
                    min: meshInfo.remapMin, 
                    max: meshInfo.remapMax, 
                    curve: meshInfo.terrainCurve, 
                    lod: meshInfo.resolution
                );
        }

        private void CreateVertsNative(Mesh.MeshData data)
        {
            var vertdata = data.GetVertexData<float3>();
            meshInfo.map.GetVectorMapNative(nativeMap: ref vertdata, meshInfo.vertexScale, meshInfo.remapMax, meshInfo.resolution);
        }
    
        private void CreateTris()
        {
            List<int> trilist = new List<int>();
            
            int width = meshInfo.dim - 1;
            
            for( int z = 0; z < width; ++z )
            {
                int offset = z * (width + 1); // offset
                for (int x = 0; x < width; ++x)
                {
                    int bl = x + offset;
                    int tl = x + width + offset + 1;
                    int tr = x + width + offset + 2;
                    int br = x + offset + 1;
                
                    // left tri
                    trilist.Add(tl);
                    trilist.Add(br);
                    trilist.Add(bl);

                    // right tri
                    trilist.Add(tl);
                    trilist.Add(tr);
                    trilist.Add(br);
                }
            }
        
            // Debug.Log("Mesh has " + trilist.Count / 3 + " triangles");
            meshGenerationOutput.triangles = trilist.ToArray();
        }

        private void CreateTrisNative(Mesh.MeshData data)
        {
            var trilist = data.GetIndexData<uint>();
            uint width = (uint)meshInfo.dim - 1;
            int index = 0;
            
            for( uint z = 0; z < width; ++z )
            {
                uint offset = z * (width + 1); // offset
                for (uint x = 0; x < width; ++x)
                {
                    uint bl = x + offset;
                    uint tl = x + width + offset + 1;
                    uint tr = x + width + offset + 2;
                    uint br = x + offset + 1;
                
                    // left tri
                    trilist[index++] = tl;
                    trilist[index++] = br;
                    trilist[index++] = bl;

                    // right tri
                    trilist[index++] = tl;
                    trilist[index++] = tr;
                    trilist[index++] = br;
                }
            }
        }
        
        private void CreateUVs( )
        {
            meshGenerationOutput.uvs = new Vector2[meshGenerationOutput.vertices.Length];

            for (int i = 0; i < meshGenerationOutput.vertices.Length; ++i)
            {
                meshGenerationOutput.uvs[i] = new Vector2(meshGenerationOutput.vertices[i].x, meshGenerationOutput.vertices[i].z);
            }
        }

        private void CreateUVsNative(Mesh.MeshData data)
        {
            var uvs = data.GetVertexData<float2>(1);
            int dim = meshInfo.map.GetMeshDimAtLOD(meshInfo.resolution);
            int i = 0;

            for (int z = 0; z < dim; ++z)
            {
                for (int x = 0; x < dim; ++x)
                {
                    uvs[i] = new float2(x * meshInfo.vertexScale, z * meshInfo.vertexScale);
                    ++i;
                }
            }
        }

        private void SetVertexDescriptors()
        {
            // make temp vertex attribute descriptors for vertex position and UV streams
            VertexAttributeDescriptor vertexPositionStream = new VertexAttributeDescriptor(VertexAttribute.Position, dimension: 3); // stream 0
            VertexAttributeDescriptor vertexUvStream = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2, stream: 1); // stream 1
            int vertexAttributeCount = 2;
        
            // set up the mesh vertex attributes position stream and UV stream
            meshVertexAttributes = new NativeArray<VertexAttributeDescriptor>(vertexAttributeCount, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            meshVertexAttributes[0] = vertexPositionStream;
            meshVertexAttributes[1] = vertexUvStream;
        }
        
        public struct MeshGenerationOutput
        {
            public Vector3[] vertices;
            public int[] triangles;
            public Vector2[] uvs;
            public Mesh.MeshDataArray meshDataArray;
            public int targetLod;

            public Action<MeshGenerationOutput, bool> callback;

            public void SetMeshDataArray(ref Mesh.MeshDataArray meshDataArray)
            {
                this.meshDataArray = meshDataArray;
            }
        }

        public struct MeshGenerationInput
        {
            public TerrainMap map;
            public int dim;
            public float vertexScale;
            public float remapMin;
            public float remapMax;
            public AnimationCurve terrainCurve;
            public int resolution;
            public Mesh.MeshDataArray meshDataArray;

            public MeshGenerationInput(TerrainMap map, int dim, float vertexScale, float remapMin, float remapMax, AnimationCurve terrainCurve, int resolution, ref Mesh.MeshDataArray meshDataArray)
            {
                this.map = map;
                this.dim = dim;
                this.vertexScale = vertexScale;
                this.remapMin = remapMin;
                this.remapMax = remapMax;
                this.terrainCurve = terrainCurve;
                this.resolution = resolution;
                this.meshDataArray = meshDataArray;
            }
        }
    }
}
