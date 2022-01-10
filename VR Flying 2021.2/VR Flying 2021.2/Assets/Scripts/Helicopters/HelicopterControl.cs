using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;

public class HelicopterControl : MonoBehaviour
{
    public HelicopterAttributes helicopterAttributes;

    public GameObject XRRig;

    private GameObject prefabInstance;
    private Transform prefabForceMoment;
    private Transform prefabXRRigAnchor;
    
    private Quaternion _lastRotation;
    private Vector3 _rotationVelocity;

    private Rigidbody _rb;
    private BoxCollider _bc;

    private float _mainThrottle;
    private float _rotationForce;
    private float _rollForce;
    private float _tiltForce;

    private bool _leveling = false;
    private bool _hovering = true;
    private float _hoveringHeight;
    private const float _hoveringVelocityThreshold = 0.03f;
    private float _hoverTransitionTime = 0f;
    private float _hoverTransitionDuration = 5f;
    private float _hoverTransitionStartTime;
    private float _hoverAngleLimit = 60f;
    private Vector3 _acceleration;
    private Vector3 _lastVelocity;

    private int _fixedUpdateCounter = 0;

    public TextMeshProUGUI hover;
    public TextMeshProUGUI up;
    public TextMeshProUGUI down;
    public TextMeshProUGUI level;
    public Color current;
    public Color def;
    
    // Start is called before the first frame update
    void Start()
    {
        if ( _hovering ) hover.color = Color.green;
        else hover.color = Color.red;
        
        _rb = GetComponent<Rigidbody>();
        _bc = GetComponent<BoxCollider>();
        
        // recenter the hmd at the beginning of time
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices( devices );
        if ( devices.Count != 0 )
        {
            devices[0].subsystem.TryRecenter();
        }

        // instantiate and set the position/rotation of the helicopter prefab
        prefabInstance = Instantiate(original: helicopterAttributes.helicopterPrefab, parent: transform);
        prefabInstance.transform.SetParent(transform);
        prefabInstance.transform.localPosition = helicopterAttributes.offset;
        prefabInstance.transform.localEulerAngles = helicopterAttributes.rotation;

        prefabInstance.transform.Find("UI").GetComponent<HelicopterHUDController>().heliTransform = transform;
        prefabInstance.transform.Find("UI").GetComponent<HelicopterHUDController>().heliRigidBody = _rb;

        // find the xrrig anchor point and the transform for applying forces
        prefabXRRigAnchor = prefabInstance.transform.Find("XRRig Anchor");
        prefabForceMoment = prefabInstance.transform.Find("Force Moment");

        XRRig.transform.SetParent(prefabXRRigAnchor);
        XRRig.transform.position = prefabXRRigAnchor.position;
        XRRig.transform.rotation = prefabXRRigAnchor.rotation;
        
        // set the rigid body and box collider values
        _rb.mass = helicopterAttributes.mass;
        _bc.center = helicopterAttributes.boxColliderCenter;
        _bc.size = helicopterAttributes.boxColliderSize;
        
        _lastRotation = transform.rotation;
        _rotationVelocity = Vector3.zero;

        _lastVelocity = _rb.velocity;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _acceleration = (_rb.velocity - _lastVelocity) / Time.fixedDeltaTime;
        
        ApplyThrottle();
        ApplyTorques();
        ApplyDrag();

        ++_fixedUpdateCounter;
    }

    private void ApplyThrottle()
    {
        // F = m * a
        Vector3 F = _rb.mass * _acceleration;
        F.x = _acceleration.x;
        F.z = _acceleration.z;
        
        Vector3 ascendForce = prefabForceMoment.up;
        
        if (_hovering)
        {
            // find the time since hover mode was activated
            _hoverTransitionTime = Time.fixedTime;
            float deltaTime = _hoverTransitionTime - _hoverTransitionStartTime;
            float transitionStep = Mathf.SmoothStep(0, 1, deltaTime / _hoverTransitionDuration);

            float angle = Vector3.Angle(Vector3.up, prefabForceMoment.up);

            if (angle > _hoverAngleLimit)
            {
                _hovering = false;
                return;
            }

            angle = Math.Abs(angle);
            angle = Math.Clamp(angle, 0, 45);
            hover.text = angle.ToString("n2");

            float yVel = _rb.velocity.y;
            float gravityMod = _rb.mass * Math.Abs(Physics.gravity.y); // the force needed to hover
            float correctionMod = 100 * yVel * angle * 2; // the force needed to stop any upward/downward velocity
            float directionMod = (yVel < -_hoveringVelocityThreshold ? -1 : 1) * correctionMod; // flip the correctionMod depending on the vertical velocity direction
            float force = (yVel < -_hoveringVelocityThreshold ? gravityMod + directionMod : gravityMod - directionMod) * transitionStep; // combine the hovering modifiers with the smoothstep transition
            Vector3 hoverForce = Vector3.up * force; // set the force in the upward direction
            
            _rb.AddRelativeForce(hoverForce, ForceMode.Force);

            // if (yVel < -_hoveringVelocityThreshold)
            // {
            //     _rb.AddRelativeForce(Vector3.up * ((_rb.mass * Math.Abs(Physics.gravity.y) + 100 * -yVel * angle * 2)) * transitionStep, ForceMode.Force);
            // }
            // else if (yVel > _hoveringVelocityThreshold)
            // {
            //     _rb.AddRelativeForce(Vector3.up * ((_rb.mass * Math.Abs(Physics.gravity.y) - 100 * yVel * angle * 2))  * transitionStep, ForceMode.Force);
            // }
            // else
            // {
            //     ascendForce *= _rb.mass * Math.Abs(Physics.gravity.y);
            //     _rb.AddForce(ascendForce, ForceMode.Force);
            // }
        }
        else
        {
            _rb.AddRelativeForce(Vector3.up * (helicopterAttributes.upwardLift + _mainThrottle), ForceMode.Force);
        }
    }

    private void ApplyTorques()
    {
        Vector3 rotateForce = Vector3.up;
        Vector3 rollForce = Vector3.forward;
        Vector3 tiltForce = Vector3.right;

        if (_leveling)
        {
            
        }
        else
        {
            rotateForce *= _rotationForce * helicopterAttributes.rotationPower;
            rollForce *= _rollForce * -helicopterAttributes.rollPower;
            tiltForce *= _tiltForce * helicopterAttributes.tiltPower;
        }
        
        // horizontal plane rotation
        _rb.AddRelativeTorque(rotateForce, ForceMode.Force);
        _rb.AddRelativeTorque(rollForce, ForceMode.Force);
        _rb.AddRelativeTorque(tiltForce, ForceMode.Force);
    }

    private void ApplyDrag()
    {
        Vector3 vel = _rb.velocity;
        // drag forces
        if (vel.magnitude > helicopterAttributes.maxVelocity)
        {
            _rb.AddForce(-vel * (vel.magnitude - helicopterAttributes.maxVelocity), ForceMode.Impulse);
        }
    }

    public void Ascend(InputAction.CallbackContext ctx)
    {
        float val = ctx.ReadValue<float>();
        _mainThrottle = val * helicopterAttributes.ascendPower;
        _hovering = false;
    }

    public void Descend(InputAction.CallbackContext ctx)
    {
        float val = ctx.ReadValue<float>();
        _mainThrottle = val * helicopterAttributes.descendPower;
        _hovering = false;
    }

    public void BodyRotate(InputAction.CallbackContext ctx)
    {
        float val = ctx.ReadValue<float>();
        _rotationForce = helicopterAttributes.rotationPower * val;
    }

    public void BodyRoll(InputAction.CallbackContext ctx)
    {
        float val = ctx.ReadValue<float>();
        _rollForce = val * helicopterAttributes.rollPower;
    }
    
    public void BodyTilt(InputAction.CallbackContext ctx)
    {
        float val = ctx.ReadValue<float>();
        _tiltForce = val * helicopterAttributes.tiltPower;
    }

    public void SelectHoverMode(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            _hovering = !_hovering;
            _hoveringHeight = transform.position.y;
            _hoverTransitionStartTime = Time.fixedTime;
        }
    }

    public void Reposition(Vector3 v3)
    {
        transform.position += v3;
    }
}
