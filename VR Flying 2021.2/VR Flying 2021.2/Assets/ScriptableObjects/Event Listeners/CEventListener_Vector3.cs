using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CEventListener_Vector3 : MonoBehaviour
{
    [SerializeField] private CEvent_Vector3 Event;
    [SerializeField] private UnityEvent<Vector3> Response;

    private void OnEnable()
    {
        Event.RegisterListener(this);
    }

    private void OnDisable()
    {
        Event.UnregisterListener(this);
    }

    public void OnEventRaised(Vector3 v3)
    {
        Response.Invoke(v3);
    }
}