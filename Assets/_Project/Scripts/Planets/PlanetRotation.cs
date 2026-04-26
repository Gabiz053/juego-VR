using UnityEngine;

public class PlanetRotation : MonoBehaviour
{
    public float rotationSpeed = 30f; // grados por segundo
    public Vector3 rotationAxis = Vector3.up;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
