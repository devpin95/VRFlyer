using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class VegetationInfo : ScriptableObject
{
    public int vegetationGridSize = 20;
    public int treeCountPerGrid = 500;
    [FormerlySerializedAs("heightCutoff")] public float chunkHeightCutoff = 0.35f;
    public float treeHeightCutoff = 0.35f;
    public Vector2 treeScaleRange;
    public float sproutAnimationTime = 2;
    public float sproutAnimationDelay = 0.05f;

    [FormerlySerializedAs("vegetationCullDistance")] [Header("Culling")]
    public float recalculateLodDistance = 100f;
    public float defaultCullDistance = 1500f;
    public float lowAltitudeCutoff = 9250f;
    public float lowAltitudeCullDistance = 500f;

    [Header("Vegetation Prefabs")] 
    [FormerlySerializedAs("poolSize")] public int treePoolSize = 10000;

    [Header("Coroutine Variables")] 
    [Tooltip("The number of objects to allocate in a frame")] public int allocationBatchSize = 25;
    [Tooltip("The delay between allocation batches")] [Range(0f, 0.1f)] public float allocationDelay = 0f;
    [Tooltip("The number of objects to deallocate in a frame")] public int deallocationBatchSize = 25;
    [Tooltip("The delay between deallocation batches")] [Range(0f, 0.1f)] public float deallocationDelay = 0f;

    public int sproutAnimationBatchSize = 25;
}
