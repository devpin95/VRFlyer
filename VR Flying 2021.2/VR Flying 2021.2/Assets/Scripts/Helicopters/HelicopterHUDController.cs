using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HelicopterHUDController : MonoBehaviour
{
    public Transform heliTransform;
    public Rigidbody heliRigidBody;

    public LayerMask layerMask;

    public TextMeshProUGUI airspeed;
    public TextMeshProUGUI verticalspeed;
    public TextMeshProUGUI altitude;
    public TextMeshProUGUI terrainAltitude;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (transform == null) return;

        Vector3 velocity = heliRigidBody.velocity;
        airspeed.text = velocity.magnitude.ToString("n2");
        verticalspeed.text = velocity.y.ToString("n2");

        altitude.text = transform.position.y.ToString("n2");

        RaycastHit hit;
        bool hasHit = Physics.Raycast(transform.position, Vector3.down, out hit);
        if (hasHit) terrainAltitude.text = hit.distance.ToString("n2");
        else terrainAltitude.text = "--";
    }
}
