using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AttitudeIndicatorController : MonoBehaviour
{
    public InstrumentInput instrumentInputs;
    
    public RectTransform pitchRect;
    public RectTransform bankRect;

    public float pitchRectOffset = 0;

    [Header("Output")] 
    public TextMeshProUGUI bankAngleReading;
    public TextMeshProUGUI pitchAngleReading;

    [Header("Readings")] 
    public float bankAngle = 0f;
    public float pitchAngle = 0f;

    private float offsetPerDegree = .15f / 10;
    

    // Update is called once per frame
    void FixedUpdate()
    {
        // make sure the heli transform is set before we update
        if (instrumentInputs.HeliTransform == null) return;
        
        // pitchRect.anchoredPosition = pitchStartingRectTrans.anchoredPosition;
        // pitchRect.rot
        Vector3 eulers = instrumentInputs.HeliTransform.localRotation.eulerAngles;
        
        // rotate the x/z plane to face the same way as the transform
        // think of it like the pilot is sitting at the origin and the line out to the horizon is the z axis and the x
        // axis is the line to the horizon on his right
        Vector3 localRight = Quaternion.Euler(0, eulers.y, 0) * Vector3.right; 
        Vector3 localForward = Quaternion.Euler(0, eulers.y, 0) * Vector3.forward;
        
        // the bank angle will be the angle between the transform.right and the right direction on the x/z plane
        // the angles will rotate around the transform.forward axis
        bankAngle = Vector3.SignedAngle(instrumentInputs.HeliTransform.right, localRight, instrumentInputs.HeliTransform.forward);
        bankAngleReading.text = "Bank: " + bankAngle.ToString("n2");
        
        // the pitch angle will be the angle between the transform.forward and the forward direction on the x/z plane
        // the angles will rotate around the transform.right axis
        pitchAngle = Vector3.SignedAngle(instrumentInputs.HeliTransform.forward, localForward, instrumentInputs.HeliTransform.right);
        pitchAngleReading.text = "Pitch: " + pitchAngle.ToString("n2");

        // rotate the bank and pitch by the same amount
        bankRect.localRotation = Quaternion.Euler(0, 0, bankAngle); // invert the angle because it will be backwards
        // pitchRect.rotation = Quaternion.Euler(0, 0, -bankAngle); // we will need to figure out the point to rotate around

        // set the pitch based on the degrees
        Vector3 pitchRectPos = pitchRect.anchoredPosition;
        pitchRectPos.y = -pitchAngle * offsetPerDegree + 0.5f + pitchRectOffset;
        pitchRect.anchoredPosition = pitchRectPos;
        
    }
}
