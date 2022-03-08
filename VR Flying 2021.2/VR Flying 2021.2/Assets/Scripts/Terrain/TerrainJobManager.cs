using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TerrainJobManager : MonoBehaviour
{
    public TerrainJobManager Instance = null;
    private NativeArray<JobHandle> jobList;
    private List<JobHandle> lockList;
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        lockList = new List<JobHandle>();
        jobList = new NativeArray<JobHandle>(1, Allocator.Persistent);
    }

    private void Update()
    {
        
    }

    public void ScheduleJob(JobHandle handle)
    {
        lock (lockList)
        {
            
        }
    }
}
