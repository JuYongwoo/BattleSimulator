using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RockingBoat : MonoBehaviour
{
    [SerializeField]
    protected float angleSpeed1  = 0;
    [SerializeField]
    protected float angleSpeed2  = 0;
    [SerializeField]
    protected float amplitude = 0.1f;
    [SerializeField]
    protected float rotationSpeed = 0.1f;
    [SerializeField]
    protected float period = 1.0f;

    void Update()
    {
    	transform.RotateAround(Vector3.forward, Vector3.up, (angleSpeed1 * Time.deltaTime)/100);
    	transform.RotateAround(Vector3.forward, Vector3.right, (angleSpeed2 * Time.deltaTime)/100);
    	transform.position += new Vector3(0, 0, (Mathf.Sin(Time.time * period) * amplitude)/100);

        transform.Rotate(Vector3.up, rotationSpeed * Mathf.Cos(Time.time * period) * 0.1f);
        transform.Rotate(Vector3.forward, rotationSpeed * Mathf.Cos(Time.time * period) * 0.1f);
    }
}
