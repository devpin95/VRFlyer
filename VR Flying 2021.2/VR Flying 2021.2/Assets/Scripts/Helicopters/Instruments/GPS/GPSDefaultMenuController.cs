using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GPSDefaultMenuController : MonoBehaviour
{
    private int _currentSelectable = 0;
    private List<Selectable> _selectables;

    private MenuEnabler _menuEnabler;
    
    // Start is called before the first frame update
    void Start()
    {
        _menuEnabler = GetComponent<MenuEnabler>();
        _menuEnabler.SetActivationCallback(MadeActive);
        // _menuEnabler.SetCancelCallback(MadeInactive);
        // SetMenuClosedCallback set in GPSUIController
        
        _selectables = _menuEnabler.BuildExplicitNavigation(transform);
    }

    // Update is called once per frame
    void Update()
    {
        _menuEnabler.SetNavigationCallback(Navigate);
    }
    
    public void MadeActive()
    {
        Debug.Log("Default Menu Active Selectable[0]");
        _currentSelectable = 0;
        SelectButton();
    }
    
    public void Navigate(Vector2 nav)
    {
        // filter out <0, 0>
        if (nav == Vector2.zero) return;

        if (nav.y != 0)
        {
            if (nav.y < 0) IncrementSelectable();
            else DecrementSelectable();
            
            SelectButton();
        }
    }
    
    private void IncrementSelectable()
    {
        ++_currentSelectable;
        if (_currentSelectable >= _selectables.Count) _currentSelectable = 0;
    }
    
    private void DecrementSelectable()
    {
        --_currentSelectable;
        if (_currentSelectable < 0) _currentSelectable = _selectables.Count - 1;
    }
    
    private void SelectButton()
    {
        Debug.Log("Selecting " + _selectables[_currentSelectable].gameObject.name);
        MenuEnabler.SetSelectedGameObject(_selectables[_currentSelectable].gameObject);
    }
}
