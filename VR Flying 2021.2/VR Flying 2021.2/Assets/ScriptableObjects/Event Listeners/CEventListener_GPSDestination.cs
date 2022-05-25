using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CEventListener_GPSDestination : MonoBehaviour
{
    [SerializeField] private CEvent_GPSDestination Event;
    [SerializeField] private UnityEvent<GPSGUI.GPSDestination> Response;

    private void OnEnable()
    {
        Event.RegisterListener(this);
    }

    private void OnDisable()
    {
        Event.UnregisterListener(this);
    }

    public void OnEventRaised(GPSGUI.GPSDestination des)
    {
        Response.Invoke(des);
    }
}