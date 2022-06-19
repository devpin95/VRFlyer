using UnityEngine;

public class InstrumentInput : ScriptableObject
{
    [SerializeField] private Transform heliTransform;
    [SerializeField] private Rigidbody heliRb;

    public Transform HeliTransform
    {
        get => heliTransform;
        set => heliTransform = value;
    }

    public Rigidbody HeliRigidBody
    {
        get => heliRb;
        set => heliRb = value;
    }

    public Vector3 Velocity
    {
        get => heliRb.velocity;
    }

    public float VelocityMagnitude
    {
        get => Velocity.magnitude;
    }
}
