using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CEventListener_Vector2_IconType : MonoBehaviour
{
    [SerializeField] private CEvent_Vector2_IconType Event;
    [SerializeField] private UnityEvent<Vector2, GPSGUI.IconTypes> Response;

    private void OnEnable()
    {
        Event.RegisterListener(this);
    }

    private void OnDisable()
    {
        Event.UnregisterListener(this);
    }

    public void OnEventRaised(Vector2 v2, GPSGUI.IconTypes type)
    {
        Response.Invoke(v2, type);
    }
}