using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GPSUIController : MonoBehaviour
{
    public GPSInfo gpsInfo;
    
    [Header("Menu Containers")]
    public Transform defaultMenuContainer;
    public Transform destinationSelectionContainer;
    
    [SerializeField] private bool _uiOpen = false;
    private List<Selectable> selectables;
    
    private InputActionMap uiControls;
    private bool _menuOpen = false;
    
    private PlayerInput playerInput;

    // Start is called before the first frame update
    void Start()
    {
        playerInput = FindObjectOfType<PlayerInput>();

        ConfigureMenuEnables();

        // default menu menu closed callback
        defaultMenuContainer.GetComponent<MenuEnabler>().SetMenuClosedCallback(() => { 
            defaultMenuContainer.transform.localScale = Vector3.zero;
            _uiOpen = false;
        });
        
        // default menu cancel button pressed callback
        defaultMenuContainer.GetComponent<MenuEnabler>().SetCancelCallback(() =>
        {
            defaultMenuContainer.transform.localScale = Vector3.zero;
            _uiOpen = false;
        });
        
        // destination menu closed callback
        destinationSelectionContainer.GetComponent<MenuEnabler>().SetMenuClosedCallback(() =>
        {
            destinationSelectionContainer.transform.localScale = Vector3.zero;
            // _uiOpen = false;
        });
        
        // destination menu cancel button pressed callback
        destinationSelectionContainer.GetComponent<MenuEnabler>().SetCancelCallback(() => { 
            SetMenuState(defaultMenuContainer);
        });
    }

    public void OpenGPSResponse()
    {
        _uiOpen = !_uiOpen;

        if (_uiOpen) SetMenuState(defaultMenuContainer);
        else
        {
            DisableAllMenues();
            EventSystem.current.SetSelectedGameObject(null);
            MenuEnabler.DisableCurrentMenu();
        }
    }

    public void SetDestinationButtonPressed()
    {
        SetMenuState(destinationSelectionContainer);
    }

    public void ExitGPSButtonPressed()
    {
        MenuEnabler.SetSelectedGameObject();
        DisableAllMenues();
        _uiOpen = false;
    }

    private void DisableAllMenues()
    {
        SetMenuState();
    }

    private void SetMenuState(Transform menu=null)
    {
        destinationSelectionContainer.transform.localScale = Vector3.zero;
        defaultMenuContainer.transform.localScale = Vector3.zero;

        if (menu != null)
        {
            // Debug.Log("Setting " + menu.name + " menu enabler to active");
            MenuEnabler.EnableMenu(menu.GetComponent<MenuEnabler>());
            menu.localScale = Vector3.one;
        }
    }

    private void ConfigureMenuEnables()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            MenuEnabler menuEnabler = transform.GetChild(i).GetComponent<MenuEnabler>();
            
            if (menuEnabler == null) continue;

            menuEnabler.IdleLimit = gpsInfo.idleTimeout;
            
            if (gpsInfo.exitWhenIdle) menuEnabler.SetIdleCallback(ExitGPSButtonPressed);
            else menuEnabler.SetIdleCallback(null);
        }
        
        destinationSelectionContainer.GetComponent<MenuEnabler>().SetCancelCallback(
            () =>
            {
                SetMenuState(defaultMenuContainer);
            });
        defaultMenuContainer.GetComponent<MenuEnabler>().SetCancelCallback(ExitGPSButtonPressed);
    }
}
