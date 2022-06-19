using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InFlightMenuController : MonoBehaviour
{
    [Header("Events")] 
    public CEvent GPSEvent;
    public CEvent clusterEvent;

    [Header("Actions")] 
    [Tooltip("Case sensitive")] public string actionMapName = "In-Flight Menu Controls";
    // public InputActionAsset menuControls;
    private InputActionMap menuControls;
    private bool _menuOpen = false;
    
    private PlayerInput playerInput;

    // events and menus
    private Transform currentMenu;
    private Transform instrumentListMenu;
    private Transform defaultMenu;
    // Start is called before the first frame update
    void Start()
    {
        transform.parent.GetComponent<Canvas>().worldCamera = GameObject.Find("XR Main Camera").GetComponent<Camera>();
        
        playerInput = FindObjectOfType<PlayerInput>();
        menuControls = playerInput.actions.FindActionMap(actionMapName);
        menuControls.Enable();
        menuControls["Start Menu"].performed += ctx => StartSelect(ctx);
        menuControls["GPS"].performed += ctx => GPSButton(ctx);
        menuControls["Cluster"].performed += ctx => ClusterButton(ctx);
        
        // get menu containers
        defaultMenu = transform.Find("Actions List");
        instrumentListMenu = transform.Find("Instruments");
        
        defaultMenu.gameObject.SetActive(false);

        currentMenu = defaultMenu;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartSelect(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            _menuOpen = !_menuOpen;
            Debug.Log("Menu " + (_menuOpen ? "" : "not") + " open");

            if (_menuOpen)
            {
                OpenMenu(currentMenu, defaultMenu.transform);
            }
            else
            {
                DeactivateMenus();
            }
        }
    }

    public void GPSButton(InputAction.CallbackContext ctx)
    {
        // Debug.Log("Open GPS");
        DeactivateMenus();
        GPSEvent.Raise();
    }

    public void ClusterButton(InputAction.CallbackContext ctx)
    {
        // Debug.Log("Open Cluster");
        DeactivateMenus();
        clusterEvent.Raise();
    }

    public void ReturnToPadButtonPressed()
    {
        Debug.Log("Return to pad button pressed!");
    }

    public void ExitGameButtonPressed()
    {
        Debug.Log("Exit game button pressed!");
    }

    private void DeactivateMenus()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
    }

    private void OpenMenu(Transform oldMenu, Transform newMenu)
    {
        oldMenu.gameObject.SetActive(false);
        newMenu.gameObject.SetActive(true);
        
        currentMenu = newMenu;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(currentMenu.GetChild(0).gameObject);
    }
}
