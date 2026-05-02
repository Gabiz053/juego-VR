using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(LineRenderer))]
public class OrbitLineRenderer : MonoBehaviour
{
    public SplineContainer splineContainer;
    public int resolution = 128;
    public float lineWidth = 0.05f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(WaitAndDraw());
    }

    // Update is called once per frame
    // void Update()
    // {
    //     Debug.Log($"Posición Tierra: {transform.position}");
    // }

    IEnumerator WaitAndDraw()
    {
        while (splineContainer == null || splineContainer.Spline.Count == 0)
        {
            yield return null;
        }
        yield return null; // un frame extra de seguridad
        DrawOrbit();
    }

    void DrawOrbit()
    {
        if (splineContainer == null) return;
        if (splineContainer.Spline.Count == 0) return;

        var lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.positionCount = resolution;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / resolution;
            splineContainer.Evaluate(t, out var pos, out var tan, out var up);
            lr.SetPosition(i, new Vector3(pos.x, pos.y, pos.z));
        }

        // Añade esto al final
        Vector3 firstPoint = lr.GetPosition(0);
        Debug.Log($"Primera posición de órbita: {firstPoint}");
    }
}
