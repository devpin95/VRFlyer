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
    public InstrumentInput heliInstrumentInput;

    public GameObject XRRig;

    public Camera remoteCamera;

    public float bladeRotationMultiplier = 2000;
    [SerializeField] private float _engineSpeed = 0;
    private float _throttleSpeed = 1;

    [Header("Control Events")] 
    public CEvent_Bool engineStateChange;
    public CEvent_Bool cruiseStateChange;

    [Header("Helicopter States")]
    [SerializeField] private bool _engineOn = false;
    [SerializeField] private bool _hovering = false;

    public bool EngineState => _engineOn;
    public bool CruiseState => _hovering;
    
    private float _engineRampSlerp = 0;
    private bool _engineRamping = false;

    [Header("Engine Multipliers")] 
    private float _engineSpeedMultiplier;
    private float _targetEngineSpeed = 1;
    [SerializeField] private float _speedUpMultiplier = 1.01f;
    [SerializeField] private float _speedDownMultiplier = 1.01f;

    private GameObject heliPrefabInstance;
    private Transform heliBladePivot;
    private Transform prefabForceMoment;
    private Transform prefabXRRigAnchor;
    private Transform prefabRemoteCameraAnchor;
    
    private Quaternion _lastRotation;
    private Vector3 _rotationVelocity;

    private Rigidbody _rb;
    private BoxCollider _bc;

    private float _mainThrottle;
    private float _rotationForce;
    private float _rollForce;
    private float _tiltForce;

    private bool _leveling = false;
    private float _hoveringHeight;
    private const float _hoveringVelocityThreshold = 0.03f;
    private float _hoverTransitionTime = 0f;
    private float _hoverTransitionDuration = 5f;
    private float _hoverTransitionStartTime;
    private float _hoverAngleLimit = 60f;
    private Vector3 _acceleration;
    private Vector3 _lastVelocity;

    [SerializeField] private bool _helicopterActive = false;
    
    
    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        // _bc = GetComponent<BoxCollider>();

        // make the rigidbody kinematic until the game ready event is called
        // so that physics doesnt effect the helicopter while the game is loading
        _rb.isKinematic = true;
        
        // recenter the hmd at the beginning of time
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices( devices );
        if ( devices.Count != 0 )
        {
            devices[0].subsystem.TryRecenter();
        }

        // instantiate and set the position/rotation of the helicopter prefab
        heliPrefabInstance = Instantiate(original: helicopterAttributes.helicopterPrefab, parent: transform);
        heliPrefabInstance.transform.SetParent(transform);
        heliPrefabInstance.transform.localPosition = helicopterAttributes.offset;
        heliPrefabInstance.transform.localEulerAngles = helicopterAttributes.rotation;
        
        // get the helicopter blade pivot
        heliBladePivot = heliPrefabInstance.transform.Find("Model").transform.Find("Blade Pivot").transform;

        // set some of the debug UI elements
        heliPrefabInstance.transform.Find("UI").GetComponent<HelicopterHUDController>().heliTransform = transform;
        heliPrefabInstance.transform.Find("UI").GetComponent<HelicopterHUDController>().heliRigidBody = _rb;
        
        // set instrument inputs for indicators to access
        heliInstrumentInput.HeliTransform = transform;
        heliInstrumentInput.HeliRigidBody = transform.GetComponent<Rigidbody>();

        // find the xrrig anchor point and the transform for applying forces
        prefabXRRigAnchor = heliPrefabInstance.transform.Find("XRRig Anchor");
        prefabRemoteCameraAnchor = heliPrefabInstance.transform.Find("Remote Camera Anchor");
        prefabForceMoment = heliPrefabInstance.transform.Find("Force Moment");

        XRRig.transform.SetParent(prefabXRRigAnchor);
        XRRig.transform.position = prefabXRRigAnchor.position;
        XRRig.transform.rotation = prefabXRRigAnchor.rotation;
        remoteCamera.transform.position = prefabRemoteCameraAnchor.position;
        remoteCamera.transform.rotation = prefabRemoteCameraAnchor.rotation;
        
        // set the rigid body and box collider values
        _rb.mass = helicopterAttributes.mass;
        // _bc.center = helicopterAttributes.boxColliderCenter;
        // _bc.size = helicopterAttributes.boxColliderSize;
        
        _lastRotation = transform.rotation;
        _rotationVelocity = Vector3.zero;

        _lastVelocity = _rb.velocity;
    }

    private void Update()
    {
        remoteCamera.transform.position = prefabRemoteCameraAnchor.position;
        remoteCamera.transform.rotation = prefabRemoteCameraAnchor.rotation;
        
        heliBladePivot.RotateAround(heliBladePivot.transform.position, heliBladePivot.transform.up, _engineSpeed * _throttleSpeed * bladeRotationMultiplier * Time.deltaTime);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!_helicopterActive) return;
        _acceleration = (_rb.velocity - _lastVelocity) / Time.fixedDeltaTime;

        if (_engineOn)
        {
            ApplyThrottle();
            ApplyTorques();
            ApplyDrag();
        }
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
                cruiseStateChange.Raise(false);
                _hovering = false;
                return;
            }

            angle = Math.Abs(angle);
            angle = Math.Clamp(angle, 0, 45);

            float yVel = _rb.velocity.y;
            float gravityMod = _rb.mass * Math.Abs(Physics.gravity.y); // the force needed to hover
            float correctionMod = 100 * yVel * angle * 2; // the force needed to stop any upward/downward velocity
            float directionMod = (yVel < -_hoveringVelocityThreshold ? -1 : 1) * correctionMod; // flip the correctionMod depending on the vertical velocity direction
            float force = (yVel < -_hoveringVelocityThreshold ? gravityMod + directionMod : gravityMod - directionMod) * transitionStep; // combine the hovering modifiers with the smoothstep transition
            Vector3 hoverForce = Vector3.up * force; // set the force in the upward direction
            
            _rb.AddRelativeForce(hoverForce, ForceMode.Force);
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
        _throttleSpeed = val + 1f; // shift the value up to [1, 2]
        _hovering = false;
        cruiseStateChange.Raise(false);
    }

    public void Descend(InputAction.CallbackContext ctx)
    {
        float val = ctx.ReadValue<float>();
        _mainThrottle = val * helicopterAttributes.descendPower;
        _hovering = false;
        cruiseStateChange.Raise(false);
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
            
            cruiseStateChange.Raise(_hovering);
        }
    }

    public bool Engine()
    {
        if (_engineRamping) return false;
        if (_hovering) return false;
        
        _engineRamping = true;
        
        if (_engineOn)
        {
            // we need to ramp down the engine
            StartCoroutine(EngineRampDown());
        }
        else
        {
            // we need to ramp up the engine
            StartCoroutine(EngineRampUp());
        }
        
        return true;
    }

    private IEnumerator EngineRampUp()
    {
        _engineSpeed = 0.001f;
        while (_engineSpeed < 1)
        {
            yield return new WaitForFixedUpdate();

            _engineSpeed *= 1.01f;

            if (_engineSpeed > 1) _engineSpeed = 1;
        }
        
        _engineOn = true;
        _engineRamping = false;
        
        engineStateChange.Raise(_engineOn);
    }

    private IEnumerator EngineRampDown()
    {
        while (_engineSpeed > 0)
        {
            yield return new WaitForFixedUpdate();

            _engineSpeed *= 0.99f;

            if (_engineSpeed < 0.01f) _engineSpeed = 0;
        }
        
        _engineOn = false;
        _engineRamping = false;
        
        engineStateChange.Raise(_engineOn);
    }

    public void Cruise()
    {
        _hovering = !_hovering;
        cruiseStateChange.Raise(_hovering);
    }
    
    public void Reposition(Vector3 v3)
    {
        transform.position += v3;
        if (remoteCamera != null)
        {
            remoteCamera.transform.position = prefabRemoteCameraAnchor.position;
            remoteCamera.transform.rotation = prefabRemoteCameraAnchor.rotation;
        }
    }

    public void GameReadyEvent()
    {
        Debug.Log("Helicopter ready to fly");
        _rb.isKinematic = false;
        _helicopterActive = true;
    }

    public void SpawnPointNotification(Vector3 spawnPoint)
    {
        Vector3 heliAttachPoint = heliPrefabInstance.transform.Find("Landing Point").transform.position;
        Vector3 attachPointToTransPos = transform.position - heliAttachPoint;
        transform.position = spawnPoint + attachPointToTransPos;
    }
}
