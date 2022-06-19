using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CEventListener_MenuEnabler : MonoBehaviour
{
    [SerializeField] private CEvent_MenuEnabler Event;
    [SerializeField] private UnityEvent<MenuEnabler> Response;

    private void OnEnable()
    {
        Event.RegisterListener(this);
    }

    private void OnDisable()
    {
        Event.UnregisterListener(this);
    }

    public void OnEventRaised(MenuEnabler menuEnabler)
    {
        Response.Invoke(menuEnabler);
    }
}
