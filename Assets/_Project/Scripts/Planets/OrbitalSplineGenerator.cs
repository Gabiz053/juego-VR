using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections;

[RequireComponent(typeof(SplineContainer))]
public class OrbitalSplineGenerator : MonoBehaviour
{
    [Header("Parametros de la orbita")]
    public float semiMajorAxis = 10f;
    public float eccentricity = 0.2f;
    public int resolution = 64;
    [Tooltip("True en SolarSystem (genera al inicio). False en KeplerLab (genera al soltar el planeta).")]
    public bool generateOnStart = true;

    [Header("Referencias")]
    public Transform sun;

    private SplineContainer _splineContainer;
    private SplineAnimate _splineAnimate;

    void Awake()
    {
        _splineContainer = GetComponent<SplineContainer>();

        // Desactivar SplineAnimate hasta que la spline este lista
        _splineAnimate = GetComponentInChildren<SplineAnimate>();
        if (_splineAnimate != null)
            _splineAnimate.enabled = false;
    }

    void Start()
    {
        if (generateOnStart)
            StartCoroutine(GenerateOrbitNextFrame());
    }

    IEnumerator GenerateOrbitNextFrame()
    {
        yield return new WaitForEndOfFrame();
        GenerateOrbit();

        // Activar SplineAnimate ahora que la spline tiene knots
        if (_splineAnimate != null)
            _splineAnimate.enabled = true;
    }

    public void UpdateOrbit(float newA, float newE)
    {
        semiMajorAxis = newA;
        eccentricity = Mathf.Clamp(newE, 0f, 0.99f);
        if (_splineContainer == null)
            _splineContainer = GetComponent<SplineContainer>();
        GenerateOrbit();
    }

    public void UpdateOrbit(float newA, float newE, Vector3 periapsisDir)
    {
        UpdateOrbit(newA, newE);
    }

    void GenerateOrbit()
    {
        float a = semiMajorAxis;
        float b = a * Mathf.Sqrt(1f - eccentricity * eccentricity);
        float c = a * eccentricity;

        Vector3 sunPos = sun != null ? sun.localPosition : Vector3.zero;

        var spline = _splineContainer.Spline;
        spline.Clear();

        for (int i = 0; i < resolution; i++)
        {
            float angle = (float)i / resolution * 2f * Mathf.PI;
            float x = Mathf.Cos(angle) * a - c;
            float z = Mathf.Sin(angle) * b;
            var position = new float3(sunPos.x + x, sunPos.y, sunPos.z + z);
            spline.Add(new BezierKnot(position), TangentMode.AutoSmooth);
        }

        spline.Closed = true;
    }
}