using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HeadingIndicatorController : MonoBehaviour
{
    public InstrumentInput instrumentInputs;

    public RectTransform headingIndicator;
    
    [Header("Output")] 
    public TextMeshProUGUI headingReading;

    // Update is called once per frame
    void FixedUpdate()
    {
        float heading = instrumentInputs.HeliTransform.rotation.eulerAngles.y;
        headingIndicator.localRotation = Quaternion.Euler(0, 0, heading);
        headingReading.text = "Heading: " + heading.ToString("n2");
    }
}
