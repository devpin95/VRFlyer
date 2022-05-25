using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GPSDestinationsController : MonoBehaviour
{
    public RectTransform listContainer;
    public GridLayoutGroup gridLayout;
    public GPSGUI gpsGUI;

    public GameObject destinationUIPrefab;

    [Header("Control")] 
    [Tooltip("Radius, in miles, that destinations will appear in list")]
    public float nearbyCutoff = 10f;
    public float updateTime = 10f;
    private float updateCounter = 0f;

    [Header("Events")] 
    public CEvent_GPSDestination setTargetDestinationEvent;

    private float gridItemHeight;
    private float gridItemSpacing;
    private float totalContainerHeight;
    private bool _menuActive = false; // use this to stop scrolling

    private List<GPSGUI.GPSDestination> presetDestination = new List<GPSGUI.GPSDestination>();
    private List<GPSGUI.GPSDestination> nearbyDestination = new List<GPSGUI.GPSDestination>();

    private List<DestinationEntry> destinationEntries = new List<DestinationEntry>();
    private List<Selectable> _selectables = new List<Selectable>();
    private int _currentSelectable = 0;
    
    private WorldRepositionManager worldRepositionManager;

    private MenuEnabler _menuEnabler;
    private DestinationEntry _currentTargetDestination;

    private struct DestinationEntry
    {
        public GameObject destinationEntryInstance;
        public GPSGUI.GPSDestination destination;
        public float previousDistance;
        public float currentDistance;
        public TextMeshProUGUI distanceText;
        public Button button;
        public bool targeted;

        public float PreviousDistance
        {
            get => previousDistance;
            set => previousDistance = value;
        }

        public float CurrentDistance
        {
            get => currentDistance;
            set => currentDistance = value;
        }

        public bool Targeted
        {
            get => targeted;
            set => targeted = value;
        }

        public static int Sort(DestinationEntry a, DestinationEntry b)
        {
            return a.currentDistance.CompareTo(b.currentDistance);
        }
        
        public static bool operator==(DestinationEntry a, DestinationEntry b)
        {
            return a.destinationEntryInstance == b.destinationEntryInstance;
        }

        public static bool operator!=(DestinationEntry a, DestinationEntry b)
        {
            return a.destinationEntryInstance != b.destinationEntryInstance;
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        gridItemHeight = gridLayout.cellSize.y;
        gridItemSpacing = gridLayout.spacing.y;
        
        // starting height, without and destinations and only the headers
        totalContainerHeight = gridItemHeight * 2 + gridItemSpacing;
        
        worldRepositionManager = FindObjectOfType<WorldRepositionManager>();
        _menuEnabler = GetComponent<MenuEnabler>();
        _menuEnabler.SetActivationCallback(MadeActive);
        
        _menuEnabler.RegisterNavigationInputActions(Navigate);
    }

    // Update is called once per frame
    void Update()
    {
        // if (!_menuEnabler.Active()) return;
        
        updateCounter += Time.deltaTime;

        if (updateCounter > updateTime)
        {
            updateCounter = 0;
            ReorderDestinations();
        }
    }

    public void AddedDestination(GPSGUI.GPSDestination destination)
    {
        //Debug.Log("Adding " + destination.icon + " " + destination.name + " to " + (destination.preset ? "preset" : "nearby") + " destinations at " + destination.worldPos);

        // add the new destination to the list
        if ( destination.preset ) presetDestination.Add(destination);
        else nearbyDestination.Add(destination);

        // update the height of the list container
        totalContainerHeight += gridItemHeight + gridItemSpacing;
        var delta = listContainer.sizeDelta;
        delta.y = totalContainerHeight;
        listContainer.sizeDelta = delta;
        
        // create an entry
        DestinationEntry entry = new DestinationEntry();
        entry.destination = destination;
        entry.currentDistance = 0;
        entry.previousDistance = 0;

        GameObject entryInstance = Instantiate(destinationUIPrefab, listContainer);
        entryInstance.transform.Find("icon").GetComponent<Image>().sprite = gpsGUI.GetSpriteForType(destination.icon).sprite;
        entryInstance.transform.Find("name").GetComponent<TextMeshProUGUI>().text = destination.name;
        entryInstance.transform.SetSiblingIndex(entryInstance.transform.parent.childCount - 1);
        
        // get the world distance between the player and the destination
        // store the TextMeshProUGUI object because we will need to update it often
        entry.distanceText =  entryInstance.transform.Find("distance").GetComponent<TextMeshProUGUI>();
        entry.distanceText.SetText("--");

        entry.destinationEntryInstance = entryInstance;

        entry.button = entry.destinationEntryInstance.GetComponent<Button>();
        entry.targeted = false;
        
        entry.button.onClick.AddListener(() => { ButtonClicked(entry); });

        // set the position of the destination in either the presets or nearby list
        destinationEntries.Add(entry);
        _selectables.Add(entry.button);
        
        // if the added destination is home, make that the target destination by default
        if (destination.name == "Home") _currentTargetDestination = entry;
        
        entryInstance.transform.SetSiblingIndex((destination.preset ? 1 : presetDestination.Count + 2));
    }

    public void ReorderDestinations()
    {
        Debug.Log("Reorder destinations");

        // clear out the list and make a new one the size of the number of entries
        _selectables = new List<Selectable>(destinationEntries.Count);
        
        // update the distances of destinations
        for (int i = 0; i < destinationEntries.Count; ++i)
        {
            float distance = Vector2.Distance(worldRepositionManager.playerWorldPos.Flatten(), destinationEntries[i].destination.worldPos.Flatten());
            distance = Utilities.UnityDistanceToMiles(distance);

            DestinationEntry entry = destinationEntries[i];
            entry.distanceText.text = distance.ToString("n2") + " miles";
            entry.PreviousDistance = entry.CurrentDistance;
            entry.currentDistance = distance;

            destinationEntries[i] = entry;
        }
        
        // sort the destinations by distance ascending
        destinationEntries.Sort(DestinationEntry.Sort);
        
        // only if the current target destination is NOT a preset destination
        // remove the target entry from the list and insert it at the front
        // if (!_currentTargetDestination.destination.preset)
        // {
        //     int index = destinationEntries.FindIndex(v =>
        //         v.destinationEntryInstance == _currentTargetDestination.destinationEntryInstance);
        //     destinationEntries.RemoveAt(index);
        //     destinationEntries.Insert(0, _currentTargetDestination);
        // }

        int presets = 0;
        int nearbies = 0;

        for (int i = 0; i < destinationEntries.Count; ++i)
        {
            // Debug.Log(destinationEntries[i].distanceText.text);
            int siblingIndex = 0;
            
            if (destinationEntries[i].destination.preset)
            {
                siblingIndex = presets;
                destinationEntries[i].destinationEntryInstance.transform.SetSiblingIndex(siblingIndex + 1);
                ++presets;
            }
            else
            {
                siblingIndex = presets + nearbies;
                destinationEntries[i].destinationEntryInstance.transform.SetSiblingIndex(siblingIndex + 2);
                ++nearbies;
            }

            _selectables.Add(destinationEntries[i].button);
            _selectables.MoveLastItemTo(siblingIndex);
            // _selectables.Remove(destinationEntries[i].button);
            // _selectables.Insert(siblingIndex, destinationEntries[i].button);

            // reset the navigation of the buttons
            // int up = i - 1;
            // int down = i + 1;
            // if (up < 0) up = destinationEntries.Count - 1;
            // if (down >= destinationEntries.Count) down = 1;
            // var tempNav = destinationEntries[i].button.navigation;
            // tempNav.selectOnUp = destinationEntries[up].button;
            // tempNav.selectOnDown = destinationEntries[down].button;
            // destinationEntries[i].button.navigation = tempNav;
        }
    }

    public void MadeActive()
    {
        listContainer.position = Vector3.zero;
        updateCounter = 0;
        ReorderDestinations();
        _currentSelectable = 0;
    }

    public void Navigate(Vector2 nav)
    {
        // filter out <0, 0>
        if (nav == Vector2.zero) return;

        // if (nav.x != 0)
        // {
        //     if ( nav.x < 0 ) Debug.Log("Left");
        //     else Debug.Log("Right");
        // }
        
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
        if (_currentSelectable >= destinationEntries.Count) _currentSelectable = 0;
    }
    
    private void DecrementSelectable()
    {
        --_currentSelectable;
        if (_currentSelectable < 0) _currentSelectable = destinationEntries.Count - 1;
    }

    private void SelectButton()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(_selectables[_currentSelectable].gameObject);
        
        // Debug.Log(destinationEntries[_currentSelectable].destinationEntryInstance.transform.localPosition);
        
        listContainer.localPosition = new Vector3(0, (_currentSelectable + 2) * gridItemHeight + (_currentSelectable + 1) * gridItemSpacing, 0);
    }

    private void ButtonClicked(DestinationEntry entry)
    {
        Debug.Log("Setting " + entry.destination.name + " as destination");
        // untarget the previous destination and target the new destination
        // get the index where the current target is in the entry list

        bool deselectedCurrentTarget = false;

        for (int i = 0; i < destinationEntries.Count; ++i)
        {
            var tempEntry = destinationEntries[i];
            
            if (tempEntry.targeted)
            {
                var colors = tempEntry.button.colors;
                colors.normalColor = Color.white;
                tempEntry.button.colors = colors;
            }
            
            // check if the temp entry is the same as the entry of the button
            if (tempEntry.destinationEntryInstance == entry.destinationEntryInstance)
            {
                // if it is the same button, flip it's targeted bool
                tempEntry.targeted = !tempEntry.targeted;

                // now, if the entry is targeted, that means we changed it from not targeted to targeted
                // and we need to highlight the button
                if (tempEntry.targeted)
                {
                    var colors = tempEntry.button.colors;
                    colors.normalColor = Color.cyan;
                    tempEntry.button.colors = colors;
                    deselectedCurrentTarget = false;
                }
                // if the entry is not targeted now, then we changed it from targeted to not targeted
                // and we need to return the button to the default state (we already put it there so just leave it
                // as it is and set a flag to send null to the GPS controller)
                else deselectedCurrentTarget = true;
            }
            // if the temp entry is not the same as the button entry, then just set it to untargeted
            else tempEntry.targeted = false;

            // put the temp entry back into the array
            destinationEntries[i] = tempEntry;
        }

        // if we decided that we were deselecting and already selected button, then send null as the GPS target
        // otherwise, we selected an unselected button and we need to send the target destination to the GPS
        if (deselectedCurrentTarget) setTargetDestinationEvent.Raise(null);
        else setTargetDestinationEvent.Raise(entry.destination);
    }
}
