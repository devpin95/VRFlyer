using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CEventListener_Texture2D_IntVector2 : MonoBehaviour
{
    [SerializeField] private CEvent_Texture2D_IntVector2 Event;
    [SerializeField] private UnityEvent<Texture2D, IntVector2> Response;

    private void OnEnable()
    {
        Event.RegisterListener(this);
    }

    private void OnDisable()
    {
        Event.UnregisterListener(this);
    }

    public void OnEventRaised(Texture2D texture2D, IntVector2 pos)
    {
        Response.Invoke(texture2D, pos);
    }
}