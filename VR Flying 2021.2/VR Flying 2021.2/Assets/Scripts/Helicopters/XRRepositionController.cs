using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class XRRepositionController : MonoBehaviour
{
    [Header("Helicopter Transform")] 
    public Rigidbody heliRb;
    private Vector3 _savedVelocity;

        [Header("Movement")] 
    public float planeMovementSteps = 0.1f;
    public float elevationSteps = 0.1f;
    public float rotationSteps = 5f;
    
    [Header("Action Map")]
    public string actionMapName;

    public Action closeCallback;
    
    private PlayerInput playerInput;
    private InputActionMap repositionControls;

    private bool elevating = false;
    private float elevationDirection;

    private bool rotating = false;
    private float rotationDirection;

    private Transform cameraTrans;

    private void Awake()
    {
        playerInput = FindObjectOfType<PlayerInput>();
        repositionControls = playerInput.actions.FindActionMap(actionMapName);

        repositionControls["Forward"].performed += MoveForward;
        repositionControls["Backward"].performed += MoveBackward;
        repositionControls["Left"].performed += MoveLeft;
        repositionControls["Right"].performed += MoveRight;
        repositionControls["Up"].performed += MoveUp;
        repositionControls["Down"].performed += MoveDown;
        repositionControls["Rotate"].performed += Rotate;
        repositionControls["Continue"].performed += Continue;
        repositionControls["Cancel"].performed += Cancel;

        cameraTrans = transform.Find("Camera Offset").Find("XR Main Camera");
    }

    private void Update()
    {
        if (elevating) transform.localPosition += new Vector3(0, elevationDirection, 0);
        if (rotating) transform.RotateAround(cameraTrans.position, Vector3.up, rotationDirection);
    }

    private void OnDestroy()
    {
        repositionControls["Forward"].performed -= MoveForward;
        repositionControls["Backward"].performed -= MoveBackward;
        repositionControls["Left"].performed -= MoveLeft;
        repositionControls["Right"].performed -= MoveRight;
        repositionControls["Up"].performed -= MoveUp;
        repositionControls["Down"].performed -= MoveDown;
        repositionControls["Rotate"].performed -= Rotate;
        repositionControls["Continue"].performed -= Continue;
        repositionControls["Cancel"].performed -= Cancel;
    }

    public void MakeRepositionActive(Action callback)
    {
        _savedVelocity = heliRb.velocity;
        heliRb.isKinematic = true;
        heliRb.velocity = Vector3.zero;
        closeCallback = callback;
        
        Debug.Log("Reposition controller active!");
        playerInput.actions.FindActionMap("UI").Disable();
        playerInput.actions.FindActionMap("Standard Controls").Disable();
        playerInput.actions.FindActionMap("In-Flight Menu Controls").Disable();
        repositionControls.Enable();
    }

    public void MakeRespositionUnactive()
    {
        Debug.Log("Reposition controller disabled!");
        
        heliRb.isKinematic = false;
        heliRb.velocity = _savedVelocity;
        
        repositionControls.Disable();
        playerInput.actions.FindActionMap("UI").Enable();
        playerInput.actions.FindActionMap("Standard Controls").Enable();
        playerInput.actions.FindActionMap("In-Flight Menu Controls").Enable();
        closeCallback.Invoke();
    }

    public void MoveForward(InputAction.CallbackContext ctx)
    {
        Debug.Log("Move forward");
        transform.localPosition += new Vector3(0, 0, planeMovementSteps);
    }
    
    public void MoveBackward(InputAction.CallbackContext ctx)
    {
        Debug.Log("Move backward");
        transform.localPosition += new Vector3(0, 0, -planeMovementSteps);
    }
    
    public void MoveLeft(InputAction.CallbackContext ctx)
    {
        Debug.Log("Move left");
        transform.localPosition += new Vector3(-planeMovementSteps, 0, 0);
    }
    
    public void MoveRight(InputAction.CallbackContext ctx)
    {
        Debug.Log("Move right");
        transform.localPosition += new Vector3(planeMovementSteps, 0, 0);
    }

    public void MoveUp(InputAction.CallbackContext ctx)
    {
        Debug.Log("Move up");
        float val = ctx.ReadValue<float>();
        
        transform.localPosition += new Vector3(0, elevationSteps, 0);
    }
    
    public void MoveDown(InputAction.CallbackContext ctx)
    {
        Debug.Log("Move down");
        transform.localPosition += new Vector3(0, -elevationSteps, 0);
    }

    public void Rotate(InputAction.CallbackContext ctx)
    {
        float val = ctx.ReadValue<float>();
        if (val < -0.1f || val > 0.1f)
        {
            rotating = true;
            rotationDirection = rotationSteps * val;
        }
        else
        {
            rotating = false;
        }
    }

    public void Continue(InputAction.CallbackContext ctx)
    {
        Debug.Log("Continue");
    }
    
    public void Cancel(InputAction.CallbackContext ctx)
    {
        Debug.Log("Cancel");
        MakeRespositionUnactive();
    }
}
