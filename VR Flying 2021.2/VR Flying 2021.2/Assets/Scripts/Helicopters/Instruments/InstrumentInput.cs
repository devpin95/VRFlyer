using UnityEngine;

public class InstrumentInput : ScriptableObject
{
    [SerializeField] private Transform heliTransform;

    public Transform HeliTransform
    {
        get => heliTransform;
        set => heliTransform = value;
    }
}
