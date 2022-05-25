using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/CEvent_GPSDestination")]
public class CEvent_GPSDestination : ScriptableObject
{
    private List<CEventListener_GPSDestination> listeners = new List<CEventListener_GPSDestination>();

    public void Raise(GPSGUI.GPSDestination des)
    {
        foreach (var listener in listeners)
        {
            listener.OnEventRaised(des);
        }
    }

    public void RegisterListener(CEventListener_GPSDestination listener)
    {
        listeners.Add(listener);
    }

    public void UnregisterListener(CEventListener_GPSDestination listener)
    {
        listeners.Remove(listener);
    }
}