using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/CEvent_Texture2D_IntVector2")]
public class CEvent_Texture2D_IntVector2 : ScriptableObject
{
    private List<CEventListener_Texture2D_IntVector2> listeners = new List<CEventListener_Texture2D_IntVector2>();

    public void Raise(Texture2D texture2D, IntVector2 pos)
    {
        foreach (var listener in listeners)
        {
            listener.OnEventRaised(texture2D, pos);
        }
    }

    public void RegisterListener(CEventListener_Texture2D_IntVector2 listener)
    {
        listeners.Add(listener);
    }

    public void UnregisterListener(CEventListener_Texture2D_IntVector2 listener)
    {
        listeners.Remove(listener);
    }
}