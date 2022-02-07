using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlatUIController : MonoBehaviour
{
    [Header("UI Objects")] 
    public RenderTexture removeCameraRenderTexture;
    public RawImage remoteCameraFeed;
    public GameObject instrumentGroup;

    // Start is called before the first frame update
    void Start()
    {
        if (removeCameraRenderTexture == null) return;
        removeCameraRenderTexture.width = Screen.currentResolution.width;
        removeCameraRenderTexture.height = Screen.currentResolution.height;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleRemoteCamera(bool state)
    {
        remoteCameraFeed.gameObject.SetActive(state);
    }

    public void ToggleInstruments(bool state)
    {
        instrumentGroup.SetActive(state);
    }
}
