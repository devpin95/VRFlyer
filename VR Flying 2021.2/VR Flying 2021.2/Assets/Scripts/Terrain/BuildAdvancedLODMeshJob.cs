using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public struct BuildAdvancedLODMeshJob : IJob
{
    public int LOD;
    public int2 dim;
    public NativeArray<float> map;

    public void Execute()
    {
        throw new System.NotImplementedException();
    }
}
