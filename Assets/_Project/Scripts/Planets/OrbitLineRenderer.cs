using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(LineRenderer))]
public class OrbitLineRenderer : MonoBehaviour
{
    public SplineContainer splineContainer;
    public int resolution = 128;
    public float lineWidth = 0.05f;

    private LineRenderer _lr;

    void Start()
    {
        _lr = GetComponent<LineRenderer>();
        StartCoroutine(WaitAndDraw());
    }

    public void Redraw()
    {
        if (splineContainer == null || splineContainer.Spline.Count == 0) return;
        StartCoroutine(RedrawNextFrame());
    }

    IEnumerator RedrawNextFrame()
    {
        yield return null;
        if (_lr == null) _lr = GetComponent<LineRenderer>();
        _lr.enabled = true;
        DrawOrbit();
    }

    public void Hide()
    {
        if (_lr == null) _lr = GetComponent<LineRenderer>();
        _lr.enabled = false;
    }

    IEnumerator WaitAndDraw()
    {
        while (splineContainer == null || splineContainer.Spline.Count == 0)
            yield return null;

        yield return null;
        DrawOrbit();
    }

    void DrawOrbit()
    {
        if (splineContainer == null) return;
        if (splineContainer.Spline.Count == 0) return;

        _lr.loop = true;
        _lr.useWorldSpace = true;
        _lr.positionCount = resolution;
        _lr.startWidth = lineWidth;
        _lr.endWidth = lineWidth;

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / resolution;
            splineContainer.Evaluate(t, out var pos, out var tan, out var up);
            _lr.SetPosition(i, new Vector3(pos.x, pos.y, pos.z));
        }

        Debug.Log($"[OrbitLineRenderer] Orbit drawn -- first point: {_lr.GetPosition(0)}.");
    }
}
