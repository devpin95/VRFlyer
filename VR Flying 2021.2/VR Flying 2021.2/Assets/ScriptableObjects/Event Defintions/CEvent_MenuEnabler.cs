using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/CEvent_MenuEnabler")]
public class CEvent_MenuEnabler : ScriptableObject
{
    private List<CEventListener_MenuEnabler> listeners = new List<CEventListener_MenuEnabler>();

    public void Raise(MenuEnabler menuEnabler)
    {
        foreach (var listener in listeners)
        {
            listener.OnEventRaised(menuEnabler);
        }
    }

    public void RegisterListener(CEventListener_MenuEnabler listener)
    {
        listeners.Add(listener);
    }

    public void UnregisterListener(CEventListener_MenuEnabler listener)
    {
        listeners.Remove(listener);
    }
}
