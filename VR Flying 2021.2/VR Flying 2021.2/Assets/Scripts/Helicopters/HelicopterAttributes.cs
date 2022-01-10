using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Helicopter Attributes")]
public class HelicopterAttributes : ScriptableObject
{
    [Header("Prefab")] 
    public GameObject helicopterPrefab;
    public Vector3 rotation;
    public Vector3 offset;
    public Vector3 scale;

    [Header("Box Collider")] 
    public Vector3 boxColliderCenter;
    public Vector3 boxColliderSize;
    
    [Header("Constant Forces")]
    public float upwardLift;
    public float hoverLift;
    public float maxVelocity;
    public float mass;

    [Header("Control Forces")] 
    public float ascendPower;
    public float descendPower;
    [FormerlySerializedAs("bodyRotationPower")] public float rotationPower;
    [FormerlySerializedAs("bodyTiltPower")] public float tiltPower;
    [FormerlySerializedAs("rollRotationPower")] public float rollPower;

    [Header("Fuel")] 
    public float fuelCapacity;
    public float defaultFuelUsage;
    public float stressedFuelUsage;

    [Header("Precision Mode")] 
    public bool hasPrecisionMode;
    public float precisionModeCoefficient = 0.5f;

    [Header("Altitude Control")] 
    public bool hasAltitudeControl;
}
