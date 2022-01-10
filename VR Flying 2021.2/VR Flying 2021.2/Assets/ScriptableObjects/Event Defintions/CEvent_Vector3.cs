using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/CEvent_Vector3")]
public class CEvent_Vector3 : ScriptableObject
{
    private List<CEventListener_Vector3> listeners = new List<CEventListener_Vector3>();

    public void Raise(Vector3 v3)
    {
        foreach (var listener in listeners)
        {
            listener.OnEventRaised(v3);
        }
    }

    public void RegisterListener(CEventListener_Vector3 listener)
    {
        listeners.Add(listener);
    }

    public void UnregisterListener(CEventListener_Vector3 listener)
    {
        listeners.Remove(listener);
    }
}