using UnityEngine;
using UnityEngine.Splines;
using System.Collections;

[RequireComponent(typeof(LineRenderer))]
public class MoonOrbitRenderer : MonoBehaviour
{
    public SplineContainer splineContainer;
    public int segments = 64;
    private LineRenderer lineRenderer;
    private bool _ready = false;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.useWorldSpace = true;
        StartCoroutine(WaitAndDraw());
    }

    IEnumerator WaitAndDraw()
    {
        while (splineContainer == null || splineContainer.Spline.Count == 0)
            yield return null;
        yield return null;
        _ready = true;
    }

    void Update()
    {
        if (!_ready) return;
        if (splineContainer == null || splineContainer.Spline.Count == 0) return;

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            splineContainer.Evaluate(t, out var pos, out var tan, out var up);
            lineRenderer.SetPosition(i, new Vector3(pos.x, pos.y, pos.z));
        }
    }
}