using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuEnabler : MonoBehaviour
{
    [SerializeField] private bool _menuActive;
    [SerializeField] private bool _navigationRegistered = false;
    
    private Action _setActiveCallback;
    private Action<Vector2> _navigationCallback;
    
    private PlayerInput playerInput;
    private InputActionMap uiControls;

    private void Start()
    {
        playerInput = FindObjectOfType<PlayerInput>();
        uiControls = playerInput.actions.FindActionMap("UI");

        uiControls["Navigate"].performed += Navigate;
    }

    public void SetActive(bool state)
    {
        _menuActive = state;

        if (_menuActive) _setActiveCallback?.Invoke();
    }

    public bool Active()
    {
        return _menuActive;
    }

    public void SetActivationCallback(Action callback)
    {
        _setActiveCallback = callback;
    }

    public void RegisterNavigationInputActions(Action<Vector2> callback)
    {
        _navigationRegistered = true;
        _navigationCallback = callback;
    }

    public void Navigate(InputAction.CallbackContext ctx)
    {
        if (!_navigationRegistered || !_menuActive) return;

        _navigationCallback.Invoke(ctx.ReadValue<Vector2>());
    }
}
