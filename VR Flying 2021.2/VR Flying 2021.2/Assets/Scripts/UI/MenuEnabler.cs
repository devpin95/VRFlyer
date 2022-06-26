using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuEnabler : MonoBehaviour
{
    public CEvent_MenuEnabler notifyMenuActive;
    [SerializeField] private bool _menuActive;
    [SerializeField] private bool _navigationRegistered = false;
    
    private Action _setActiveCallback;
    private Action<Vector2> _navigationCallback;
    private Action _idleCallback;
    private Action _cancelCallback;
    public Action menuClosedCallback;
    
    private PlayerInput playerInput;
    private InputActionMap uiControls;

    private float _idleTime = 0;
    private float _idleLimit;

    private bool _ignoreEvent = false;

    public static MenuEnabler Instance;
    public GameObject selectedObject = null;

    // public static MenuEnabler Master;
    // public MenuEnabler MasterCurrentMenu;
    public static List<MenuEnabler> menuEnablers = new List<MenuEnabler>();

    public float IdleLimit
    {
        get => _idleLimit;
        set => _idleLimit = value;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Debug.Log(transform.name + " is now the MenuEnabler instance");
            Instance = this;
        }
        
        AddEnablerToList(this);
    }

    private void Start()
    {
        playerInput = FindObjectOfType<PlayerInput>();
        uiControls = playerInput.actions.FindActionMap("UI");

        uiControls["Navigate"].performed += Navigate;
        uiControls["Cancel"].performed += Cancel;
    }
    

    public IEnumerator DelayedSelect(GameObject obj)
    {
        EventSystem.current.SetSelectedGameObject(obj);
        yield return new WaitForNextFrameUnit();
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(obj);
    }

    public static void EnableMenu(MenuEnabler menuEnabler)
    {
        for (int i = 0; i < menuEnablers.Count; ++i)
        {
            if (menuEnablers[i] != menuEnabler)
            {
                menuEnablers[i].SetActive(false);
                menuEnablers[i].menuClosedCallback.Invoke();
            }
            else
            {
                menuEnablers[i].SetActive(true);   
            }
        }
    }

    public static void DisableMenu(MenuEnabler menuEnabler)
    {
        for (int i = 0; i < menuEnablers.Count; ++i)
        {
            if (menuEnablers[i] == menuEnabler)
            {
                menuEnablers[i].SetActive(false);
                menuEnablers[i].menuClosedCallback.Invoke();
                break;
            }
        }
    }

    public static void DisableCurrentMenu()
    {
        for (int i = 0; i < menuEnablers.Count; ++i)
        {
            if (menuEnablers[i]._menuActive)
            {
                menuEnablers[i].SetActive(false);
                menuEnablers[i].menuClosedCallback.Invoke();
                break;
            }
        }
    }

    private void Update()
    {
        if (!Active()) return;
        
        _idleTime += Time.deltaTime;

        if (_idleTime > _idleLimit)
        {
            _idleTime = 0;
            SetActive(false);
            _idleCallback?.Invoke();
            Debug.Log("Idle limit reached for " + transform.name);
        }
    }

    public static void AddEnablerToList(MenuEnabler menuEnabler)
    {
        menuEnablers.Add(menuEnabler);
    }

    public void SetActive(bool state)
    {
        _menuActive = state;

        if (_menuActive)
        {
            _idleTime = 0;
            Debug.Log("Invoking " + _setActiveCallback);
            _setActiveCallback?.Invoke();
            // EnableMenu(this);
        }
    }

    public bool Active()
    {
        return _menuActive;
    }

    public List<Selectable> BuildExplicitNavigation(Transform container=null)
    {
        // if no container is passed in, then just use the transform of the object this menu enable is attached to
        if (container == null) container = transform;
        
        List<Selectable> selectables = new List<Selectable>();

        for (int i = 0; i < container.childCount; ++i)
        {
            Selectable selectable = container.GetChild(i).transform.GetComponent<Selectable>();

            if (selectable != null)
            {
                selectables.Add(selectable);
            }
        }

        return selectables;
    }

    public void SetActivationCallback(Action callback)
    {
        _setActiveCallback = callback;
    }

    public void SetNavigationCallback(Action<Vector2> callback)
    {
        _navigationRegistered = true;
        _navigationCallback = callback;
    }

    public void SetIdleCallback(Action callback)
    {
        _idleCallback = callback;
    }

    public void SetCancelCallback(Action callback)
    {
        _cancelCallback = callback;
        // _previousMenu = previousMenu;
    }

    public void SetMenuClosedCallback(Action callback)
    {
        menuClosedCallback = callback;
    }

    public void Navigate(InputAction.CallbackContext ctx)
    {
        if (!_navigationRegistered || !_menuActive) return;

        _idleTime = 0;
        _navigationCallback.Invoke(ctx.ReadValue<Vector2>());
    }

    public void Cancel(InputAction.CallbackContext ctx)
    {
        if (!_menuActive) return;
        
        _cancelCallback?.Invoke();
    }

    public static void SetSelectedGameObject(GameObject obj = null)
    {
        // if ( EventSystem.current.currentSelectedGameObject != null )
        //     Debug.Log("Deselecting " + EventSystem.current.currentSelectedGameObject.name);
        EventSystem.current.SetSelectedGameObject(null);

        Instance.StartCoroutine(Instance.DelayedSelect(obj));
    }
    
    public void NewMenuOpened(MenuEnabler menuEnabler)
    {
        
    }
}
