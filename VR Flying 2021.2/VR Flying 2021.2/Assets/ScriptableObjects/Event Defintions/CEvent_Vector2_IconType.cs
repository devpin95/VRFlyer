using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/CEvent_Vector2_IconType")]
public class CEvent_Vector2_IconType : ScriptableObject
{
    private List<CEventListener_Vector2_IconType> listeners = new List<CEventListener_Vector2_IconType>();

    public void Raise(Vector2 v2, GPSGUI.IconTypes type)
    {
        foreach (var listener in listeners)
        {
            listener.OnEventRaised(v2, type);
        }
    }

    public void RegisterListener(CEventListener_Vector2_IconType listener)
    {
        listeners.Add(listener);
    }

    public void UnregisterListener(CEventListener_Vector2_IconType listener)
    {
        listeners.Remove(listener);
    }
}