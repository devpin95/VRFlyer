using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/CEvent_Bool")]
public class CEvent_Bool : ScriptableObject
{
    private List<CEventListener_Bool> listeners = new List<CEventListener_Bool>();

    public void Raise(bool s)
    {
        foreach (var listener in listeners)
        {
            listener.OnEventRaised(s);
        }
    }

    public void RegisterListener(CEventListener_Bool listener)
    {
        listeners.Add(listener);
    }

    public void UnregisterListener(CEventListener_Bool listener)
    {
        listeners.Remove(listener);
    }
}
