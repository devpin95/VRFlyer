using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain.Threading
{
    public class WaterBodyBuilderThread
    {
        public WaterBuilderInput waterBuilderInput;
        public Queue<WaterBuilderOutput> resultQ;
        public Action<WaterBuilderOutput> callback;
        
        private NativeArray<VertexAttributeDescriptor> meshVertexAttributes; 
        
        public enum MeshDataStreams : int
        {
            Vertices = 0,
            UVs = 1
        }
        
        public WaterBodyBuilderThread(ref WaterBuilderInput waterBuilderInput, Queue<WaterBuilderOutput> resultQ, Action<WaterBuilderOutput> callback)
        {
            this.waterBuilderInput = waterBuilderInput;
            this.resultQ = resultQ;
            this.callback = callback;
        }

        public void ThreadProc()
        {
            WaterBuilderOutput output = new WaterBuilderOutput();
            output.callback = callback;
            output.meshDataArray = waterBuilderInput.meshDataArray;

            List<IntVector2> lineSegments = MapVertices();
            
            SetVertexDescriptors();
            Mesh.MeshData data = waterBuilderInput.meshDataArray[waterBuilderInput.meshDataArray.Length - 1];
            CreateMesh(data, lineSegments);
            
            meshVertexAttributes.Dispose();
            
            // set the submesh so that the verts will show
            data.subMeshCount = 1;
            MeshUpdateFlags smflags = /*MeshUpdateFlags.DontRecalculateBounds | */MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;
            SubMeshDescriptor smdes = new SubMeshDescriptor(0, lineSegments.Count * 2);
            data.SetSubMesh(0, smdes, smflags);
            
            
            lock (resultQ) resultQ.Enqueue(output);
        }

        private void CreateMesh(Mesh.MeshData data, List<IntVector2> lineSegments)
        {
            // brute force this, then go back and remove verts that overlap
            List<float3> vertList = new List<float3>();
            List<uint> triList = new List<uint>();
            List<float2> uvList = new List<float2>();

            int vertCount = 0;
            
            for (int i = 0; i < lineSegments.Count; i += 2)
            {
                IntVector2 start = lineSegments[i];
                IntVector2 end = lineSegments[i + 1];

                // VERTEX ----------------------------------------------------------------------------------------------
                // bottom left
                vertList.Add(new float3(start.x * waterBuilderInput.vertScale, waterBuilderInput.waterElevation, start.y * waterBuilderInput.vertScale));
                // top left
                vertList.Add(new float3(start.x * waterBuilderInput.vertScale, waterBuilderInput.waterElevation, (start.y + 1) * waterBuilderInput.vertScale));
                // bottom right
                vertList.Add(new float3((end.x+1) * waterBuilderInput.vertScale, waterBuilderInput.waterElevation, end.y * waterBuilderInput.vertScale));
                // top right
                vertList.Add(new float3((end.x+1) * waterBuilderInput.vertScale, waterBuilderInput.waterElevation, (end.y + 1) * waterBuilderInput.vertScale));
                
                // TRIANGLES -------------------------------------------------------------------------------------------
                // left triangle
                triList.Add((uint)vertCount); // bottom left
                triList.Add((uint)vertCount + 1); // top left
                triList.Add((uint)vertCount + 2); // top right
                
                // right triangle
                triList.Add((uint)vertCount + 2); // bottom left
                triList.Add((uint)vertCount + 1); // top left
                triList.Add((uint)vertCount + 3); // top right
                
                // UVs -------------------------------------------------------------------------------------------------
                // bottom left
                uvList.Add(new float2(start.x / (float)waterBuilderInput.meshVerts, start.y / (float)waterBuilderInput.meshVerts));
                // top left
                uvList.Add(new float2(start.x / (float)waterBuilderInput.meshVerts, (start.y + 1) / (float)waterBuilderInput.meshVerts));
                // bottom right
                uvList.Add(new float2((end.x+1) / (float)waterBuilderInput.meshVerts, end.y / (float)waterBuilderInput.meshVerts));
                // top right
                uvList.Add(new float2((end.x+1) / (float)waterBuilderInput.meshVerts, (end.y + 1) / (float)waterBuilderInput.meshVerts));

                vertCount += 4;
            }
            
            // set up mesh data
            data.SetVertexBufferParams(vertList.Count, meshVertexAttributes);
            data.SetIndexBufferParams(triList.Count, IndexFormat.UInt32);
            
            // copy the vert data into the native array for the mesh data
            var nativeVerts = data.GetVertexData<float3>();
            for (int i = 0; i < vertList.Count; ++i) nativeVerts[i] = vertList[i];
            
            // copy the tris data into the native array for the mesh data
            var nativeTris = data.GetIndexData<uint>();
            for (int i = 0; i < triList.Count; ++i) nativeTris[i] = triList[i];
            
            // copy the UV data into the native array for the mesh data
            var nativeUvs = data.GetVertexData<float2>(1);
            for (int i = 0; i < uvList.Count; ++i) nativeUvs[i] = uvList[i];
        }

        private List<IntVector2> MapVertices()
        {
            List<IntVector2> lineSegments = new List<IntVector2>();
            for (int y = 0; y < waterBuilderInput.meshVerts; ++y)
            {
                bool lookingForStart = true;
                bool lookingForEnd = false;
                bool lineFind = false;
                
                IntVector2 startingPos = IntVector2.zero;
                IntVector2 endingPos = IntVector2.zero;
                for (int x = 0; x < waterBuilderInput.meshVerts; ++x)
                {
                    // we found a pixel that is water
                    if (lookingForStart && waterBuilderInput.water[x, y] == true)
                    {
                        // if we are looking for the start of a line, save this pos and start looking for the end
                        startingPos = new IntVector2(x, y);
                        lookingForStart = false;
                        lookingForEnd = true;
                        lineFind = true;
                    }
                        
                    if (lookingForEnd)
                    {
                        // check if the next x value goes off the end of the grid
                        if (x + 1 >= waterBuilderInput.meshVerts)
                        {
                            endingPos = new IntVector2(x, y);
                            lookingForEnd = false;
                            lookingForStart = true;
                        }
                            
                        // check if the next water value is false
                        else if (waterBuilderInput.water[x + 1, y] == false)
                        {
                            endingPos = new IntVector2(x + 1, y);
                            lookingForEnd = false;
                            lookingForStart = true;
                        }
                    }
                }

                if (lineFind)
                {
                    lineSegments.Add(startingPos);
                    lineSegments.Add(endingPos);
                }
            }

            return lineSegments;
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
        
        public class WaterBuilderInput
        {
            public bool[,] water;
            public float waterElevation;
            public float maxHeight;
            public int meshVerts;
            public float vertScale;
            public Mesh.MeshDataArray meshDataArray;

            public WaterBuilderInput(bool[,] water, float waterElevation, int meshVerts, float vertScale, ref Mesh.MeshDataArray meshDataArray)
            {
                this.water = water;
                this.waterElevation = waterElevation;
                this.meshVerts = meshVerts;
                this.vertScale = vertScale;
                this.meshDataArray = meshDataArray;
            }
        }

        public class WaterBuilderOutput
        {
            public Action<WaterBuilderOutput> callback;
            public Mesh.MeshDataArray meshDataArray;
        }
    }
}
