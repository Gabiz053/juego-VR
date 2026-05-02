using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MoonOrbitRenderer : MonoBehaviour
{
    public Transform earth;
    public float orbitRadius = 2.5f;
    public int segments = 64;

    private LineRenderer lineRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>(); // Obtener el componente LineRenderer
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.useWorldSpace = true;
    }

    // Update is called once per frame
    void Update()
    {
        float worldRadius = orbitRadius * earth.lossyScale.x;
        for (int i = 0; i < segments; i++)
        {
            float angle = (360f / segments) * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * worldRadius;
            float z = Mathf.Sin(angle) * worldRadius;
            lineRenderer.SetPosition(i, earth.position + new Vector3(x, 0, z));
        }
    }
}
