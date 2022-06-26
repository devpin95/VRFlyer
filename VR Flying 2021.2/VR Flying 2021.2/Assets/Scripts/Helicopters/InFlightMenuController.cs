using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InFlightMenuController : MonoBehaviour
{
    [Header("Events")] 
    public CEvent GPSEvent;
    public CEvent clusterEvent;
    
    [FormerlySerializedAs("inFlightMenuInstance")] [Header("Menu")]
    public GameObject inFlightMenu;
    public GameObject repositionInstructions;
    public XRRepositionController repositionController;

    private GameObject actionsList;

    [Header("Actions")] 
    [Tooltip("Case sensitive")] public string actionMapName = "In-Flight Menu Controls";
    // public InputActionAsset menuControls;
    private InputActionMap menuControls;
    private bool _menuOpen = false;
    
    private PlayerInput playerInput;

    // events and menus
    private Transform actionsMenu;
    private MenuEnabler _menuEnabler;
    private List<Selectable> _selectables;
    private int _currentSelectable = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        transform.parent.GetComponent<Canvas>().worldCamera = GameObject.Find("XR Main Camera").GetComponent<Camera>();
        
        playerInput = FindObjectOfType<PlayerInput>();
        menuControls = playerInput.actions.FindActionMap(actionMapName);
        menuControls.Enable();
        menuControls["Start Menu"].performed += StartSelect;
        menuControls["GPS"].performed += GPSButton;
        menuControls["Cluster"].performed += ClusterButton;
        
        // get menu containers
        actionsMenu = transform.Find("Actions List");
        
        _menuEnabler = actionsMenu.GetComponent<MenuEnabler>();
        _menuEnabler.SetCancelCallback(DeactivateMenus);
        _menuEnabler.SetIdleCallback(DeactivateMenus);
        _menuEnabler.SetNavigationCallback(Navigate);
        _menuEnabler.SetMenuClosedCallback(() => { });
        _menuEnabler.IdleLimit = 10f;
        
        _selectables = _menuEnabler.BuildExplicitNavigation(actionsMenu.transform);

        actionsMenu.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartSelect(InputAction.CallbackContext ctx)
    {
        _menuOpen = !_menuOpen;
        Debug.Log("Menu " + (_menuOpen ? "" : "not") + " open");

        if (_menuOpen)
        {
            actionsMenu.gameObject.SetActive(true);
            _menuEnabler.SetActive(true);
            MenuEnabler.SetSelectedGameObject(_selectables[0].gameObject);
        }
        else
        {
            actionsMenu.gameObject.SetActive(false);
            _menuEnabler.SetActive(false);
            MenuEnabler.SetSelectedGameObject(null);
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

    public void RepositionXR()
    {
        inFlightMenu.SetActive(false);
        repositionInstructions.SetActive(true);
        repositionController.MakeRepositionActive(CloseRepositionXR);
    }

    public void CloseRepositionXR()
    {
        inFlightMenu.SetActive(true);
        repositionInstructions.SetActive(false);
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
        _menuOpen = false;
        for (int i = 0; i < transform.childCount; ++i)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
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

    private void SelectButton()
    {
        MenuEnabler.SetSelectedGameObject(_selectables[_currentSelectable].gameObject);
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
}
