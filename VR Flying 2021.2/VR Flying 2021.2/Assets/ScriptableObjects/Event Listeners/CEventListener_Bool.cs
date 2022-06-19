using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CEventListener_Bool : MonoBehaviour
{
    [SerializeField] private CEvent_Bool Event;
    [SerializeField] private UnityEvent<bool> Response;

    private void OnEnable()
    {
        Event.RegisterListener(this);
    }

    private void OnDisable()
    {
        Event.UnregisterListener(this);
    }

    public void OnEventRaised(bool s)
    {
        Response.Invoke(s);
    }
}
