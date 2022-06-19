using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClusterController : MonoBehaviour
{
    public GameObject menuContainer;

    [Header("Buttons")]
    public GameObject engineSwitch;
    public GameObject cruiseSwitch;

    [Header("Materials")] 
    public Material lightGreenMat;
    public Material lightRedMat;
    public Material lightOffMat;

    private MenuEnabler _menuEnabler;
    private List<List<Selectable>> _selectables;
    private IntVector2 _currentSelectable = IntVector2.zero; // x = row, y = column
    private IntVector2 _previousSelectable = IntVector2.zero;

    private HelicopterControl _helicopterControl;

    private bool _uiOpen = false;

    private struct SwitchMeta
    {
        public Button button;
        public TextMeshProUGUI switchState;
        public GameObject switchObj;
        public GameObject switchToggle;
        public GameObject switchLED;
        public MeshRenderer ledMeshRenderer;
    }

    private Dictionary<string, SwitchMeta> switchDict;

    // Start is called before the first frame update
    void Start()
    {
        switchDict = new Dictionary<string, SwitchMeta>();
        
        _menuEnabler = GetComponent<MenuEnabler>();
        _menuEnabler.IdleLimit = 10f;
        
        _menuEnabler.SetActivationCallback(MadeActive);
        _menuEnabler.SetNavigationCallback(Navigate);
        _menuEnabler.SetCancelCallback(MenuClosed);
        _menuEnabler.SetIdleCallback(MenuClosed);
        _menuEnabler.SetMenuClosedCallback(MenuClosed);
        
        _selectables = new List<List<Selectable>>();
        BuildExplicitNavigation();

        _helicopterControl = gameObject.GetComponentInParent<HelicopterControl>();

        SetUpSwitchDict();
        SetUpSwitchInitialStates();
        SetUpSwitchEvents();
    }

    private void SetUpSwitchDict()
    {
        switchDict.Add("Engine", ScrapeSwitchInfo(engineSwitch));
        switchDict.Add("Cruise", ScrapeSwitchInfo(cruiseSwitch));
    }

    private SwitchMeta ScrapeSwitchInfo(GameObject obj)
    {
        SwitchMeta switchInfo = new SwitchMeta();
        switchInfo.button = obj.transform.Find("Button").GetComponent<Button>();
        switchInfo.switchState = switchInfo.button.transform.Find("Data").Find("Switch State").GetComponent<TextMeshProUGUI>();
        switchInfo.switchObj = obj.transform.Find("Switch").gameObject;
        switchInfo.switchToggle = switchInfo.switchObj.transform.Find("Toggle").gameObject;
        switchInfo.switchLED = switchInfo.switchObj.transform.Find("LED").gameObject;
        switchInfo.ledMeshRenderer = switchInfo.switchLED.GetComponent<MeshRenderer>();

        return switchInfo;
    }

    private void SetUpSwitchInitialStates()
    {
        SetSwitchState(switchDict["Engine"].switchState, _helicopterControl.EngineState, "ON", "OFF", Color.green, Color.red);
        SetSwitchState(switchDict["Cruise"].switchState, _helicopterControl.CruiseState, "ON", "OFF", Color.green, Color.red);
    }

    private void SetSwitchState(TextMeshProUGUI tmesh, bool cond, string s1, string s2, Color c1, Color c2)
    {
        tmesh.text = (cond ? s1 : s2); 
        tmesh.color = (cond ? c1 : c2);
    }
    
    private void SetUpSwitchEvents()
    {
        var engineButton = engineSwitch.transform.Find("Button").GetComponent<Button>();
        engineButton.onClick.AddListener(() =>
        {
            _helicopterControl.Engine();

            SetSwitchState(switchDict["Engine"].switchState, _helicopterControl.EngineState, "ON", "OFF", Color.green, Color.red);
        });
        
        var cruiseButton = cruiseSwitch.transform.Find("Button").GetComponent<Button>();
        cruiseButton.onClick.AddListener(() =>
        {
            _helicopterControl.Cruise();
            SetSwitchState(switchDict["Cruise"].switchState, _helicopterControl.CruiseState, "ON", "OFF", Color.green, Color.red);
        });
    }

    public void MadeActive()
    {
        _currentSelectable = IntVector2.zero;
        SelectButton();
    }
    
    public void OpenClusterUI()
    {
        _uiOpen = !_uiOpen;

        if (_uiOpen)
        {
            // Debug.Log("Cluster UI opened!");
            MenuEnabler.EnableMenu(_menuEnabler);
            _currentSelectable = IntVector2.zero;
            SelectButton();
        }
        else
        {
            CloseClusterUI();
        }
    }

    public void CloseClusterUI()
    {
        // Debug.Log("Cluster UI closed! " + _currentSelectable);
        GameObject button = _selectables[_currentSelectable.x][_currentSelectable.y].gameObject;
        button.transform.Find("Data").gameObject.SetActive(false);
        
        MenuEnabler.DisableMenu(_menuEnabler);
        MenuEnabler.SetSelectedGameObject(null);
        
        _uiOpen = false;
    }

    public void MenuClosed()
    {
        // Debug.Log("Cluster UI closed! " + _currentSelectable);
        GameObject button = _selectables[_currentSelectable.x][_currentSelectable.y].gameObject;
        button.transform.Find("Data").gameObject.SetActive(false);
        // MenuEnabler.SetSelectedGameObject(null);

        _uiOpen = false;
    }
    
    public void Navigate(Vector2 nav)
    {
        // filter out <0, 0>
        if (nav == Vector2.zero) return;

        _previousSelectable = new IntVector2(_currentSelectable.x, _currentSelectable.y);
        
        if (nav.y < 0) SelectableUp();
        else if ( nav.y > 0 ) SelectableDown();
        
        if (nav.x < 0) SelectableLeft();
        else if (nav.x > 0) SelectableRight();
        
        SelectButton();
    }

    private void SelectableUp()
    {
        --_currentSelectable.x;
        if (_currentSelectable.x < 0)
        {
            _currentSelectable.x = _selectables.Count - 1;
            Debug.Log("_selectables.Count - 1 = " + (_selectables.Count - 1));
        }
        
        // move to the end of the current row if the previous row was longer
        if (_currentSelectable.y >= _selectables[_currentSelectable.x].Count)
        {
            _currentSelectable.y = _selectables[_currentSelectable.x].Count - 1;
        }
        
        Debug.Log("Up");
    }
    
    private void SelectableDown()
    {
        ++_currentSelectable.x;
        if (_currentSelectable.x >= _selectables.Count)
        {
            _currentSelectable.x = 0;
        }
        
        // move to the end of the current row if the previous row was longer
        if (_currentSelectable.y >= _selectables[_currentSelectable.x].Count)
        {
            _currentSelectable.y = _selectables[_currentSelectable.x].Count - 1;
        }

        // Debug.Log("Down");
    }
    
    private void SelectableLeft()
    {
        --_currentSelectable.y;
        if (_currentSelectable.y < 0)
        {
            _currentSelectable.y = _selectables[_currentSelectable.x].Count - 1;
        }
        
        // Debug.Log("Left");
    }
    
    private void SelectableRight()
    {
        ++_currentSelectable.y;
        if (_currentSelectable.y >= _selectables[_currentSelectable.x].Count)
        {
            _currentSelectable.y = 0;
        }
        
        // Debug.Log("Right");
    }
    
    private void SelectButton()
    {
        GameObject prevButton = _selectables[_previousSelectable.x][_previousSelectable.y].gameObject;
        prevButton.transform.Find("Data").gameObject.SetActive(false);
        
        GameObject button = _selectables[_currentSelectable.x][_currentSelectable.y].gameObject;
        button.transform.Find("Data").gameObject.SetActive(true);
        
        // Debug.Log("Deactivating " + prevButton.name + " Activating " + button.name);
        
        MenuEnabler.SetSelectedGameObject(button);
        // EventSystem.current.SetSelectedGameObject(null);
        // EventSystem.current.SetSelectedGameObject(_selectables[_currentSelectable].gameObject);
        
        // Debug.Log(destinationEntries[_currentSelectable].destinationEntryInstance.transform.localPosition);
    }

    private void BuildExplicitNavigation()
    {
        // loop through each row container
        for (int i = 0; i < menuContainer.transform.childCount; ++i)
        {
            List<Selectable> selectableRow = new List<Selectable>();
            GameObject rowContainer = menuContainer.transform.GetChild(i).gameObject;

            // loop through each of the switch objects in the row and extract the button selectable
            for (int j = 0; j < rowContainer.transform.childCount; ++j)
            {
                GameObject switchContainer = rowContainer.transform.GetChild(j).gameObject;
                Button button = switchContainer.transform.Find("Button").GetComponent<Button>();
                selectableRow.Add(button);
            }
            
            // add the row to the selectable grid
            _selectables.Add(selectableRow);
        }
    }

    public void EngineStateChangeResponse(bool state)
    {
        SetSwitchState(switchDict["Engine"].switchState, state, "ON", "OFF", Color.green, Color.red);

        if (state == true) switchDict["Engine"].ledMeshRenderer.material = lightGreenMat;
        else switchDict["Engine"].ledMeshRenderer.material = lightOffMat;
    }

    public void CruiseStateChangeResponse(bool state)
    {
        SetSwitchState(switchDict["Cruise"].switchState, state, "ON", "OFF", Color.green, Color.red);
        
        if (state == true) switchDict["Cruise"].ledMeshRenderer.material = lightGreenMat;
        else switchDict["Cruise"].ledMeshRenderer.material = lightOffMat;
    }
}
