using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/CEvent")]
public class CEvent : ScriptableObject
{
    private List<CEventListener> listeners = new List<CEventListener>();

    public void Raise()
    {
        foreach (var listener in listeners)
        {
            listener.OnEventRaised();
        }
    }

    public void RegisterListener(CEventListener listener)
    {
        listeners.Add(listener);
    }

    public void UnregisterListener(CEventListener listener)
    {
        listeners.Remove(listener);
    }
}