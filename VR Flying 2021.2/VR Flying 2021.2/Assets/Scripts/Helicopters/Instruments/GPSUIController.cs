using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GPSUIController : MonoBehaviour
{
    public Transform defaultMenuContainer;
    public Transform destinationSelectionContainer;
    
    private bool _uiOpen = false;
    private List<Selectable> selectables;
    
    private InputActionMap uiControls;
    private bool _menuOpen = false;
    
    private PlayerInput playerInput;

    // Start is called before the first frame update
    void Start()
    {
        playerInput = FindObjectOfType<PlayerInput>();
        uiControls = playerInput.actions.FindActionMap("UI");

        selectables = new List<Selectable>();
        BuildExplicitNavigation(defaultMenuContainer);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenGPSResponse()
    {
        _uiOpen = !_uiOpen;

        if (_uiOpen)
        {
            EnableCallbacks();
            SetMenuState(defaultMenuContainer);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectables[0].gameObject);
        }
        else
        {
            DisableCallbacks();
            DisableAllMenues();
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void SetDestinationButtonPressed()
    {
        for (int i = 0; i < destinationSelectionContainer.Find("List Container").childCount; ++i)
        {
            Selectable selectable = destinationSelectionContainer.Find("List Container").GetChild(i).GetComponent<Button>();

            if (selectable)
            {
                Debug.Log(selectable.transform.Find("name") + " selected!");
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
                break;
            }
        }

        SetMenuState(destinationSelectionContainer);
    }

    public void ExitGPSButtonPressed()
    {
        EventSystem.current.SetSelectedGameObject(null);
        DisableAllMenues();
        DisableCallbacks();
        _uiOpen = false;
    }

    private void DisableAllMenues()
    {
        SetMenuState();
    }

    private void BuildExplicitNavigation(Transform container)
    {
        for (int i = 0; i < container.childCount; ++i)
        {
            Selectable selectable = container.GetChild(i).GetComponent<Button>();

            if (selectable) selectables.Add(selectable);
        }

        for (int i = 0; i < selectables.Count; ++i)
        {
            // get a copy of the navigation
            Navigation navigation = selectables[i].navigation;
            
            // set the up selectable index
            int up = i - 1;
            if (up < 0) up = selectables.Count - 1;

            // set the down selectable index
            int down = i + 1;
            if (down >= selectables.Count) down = 0;

            navigation.selectOnUp = selectables[up];
            navigation.selectOnDown = selectables[down];

            // copy the navigation back into the selectable
            selectables[i].navigation = navigation;
        }
    }

    public void CancelButtonPressed(InputAction.CallbackContext ctx)
    {
        Debug.Log("Cancel Button Pressed!");
        if (defaultMenuContainer.GetComponent<MenuEnabler>().Active()) ExitGPSButtonPressed();
        else if (destinationSelectionContainer.GetComponent<MenuEnabler>().Active())
        {
            SetMenuState(defaultMenuContainer);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectables[0].gameObject);
        }
    }

    private void EnableCallbacks()
    {
        uiControls["Cancel"].performed += CancelButtonPressed;
    }

    private void DisableCallbacks()
    {
        uiControls["Cancel"].performed -= CancelButtonPressed;
    }

    private void SetMenuState(Transform menu=null)
    {
        destinationSelectionContainer.GetComponent<MenuEnabler>().SetActive(false);
        destinationSelectionContainer.transform.localScale = Vector3.zero;
        
        defaultMenuContainer.GetComponent<MenuEnabler>().SetActive(false);
        defaultMenuContainer.transform.localScale = Vector3.zero;

        if (menu != null)
        {
            menu.GetComponent<MenuEnabler>().SetActive(true);
            menu.localScale = Vector3.one;
        }
    }
}
