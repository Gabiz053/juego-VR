using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class OrbitalSplineGenerator : MonoBehaviour
{
    [Header("Parámetros de la órbita")]
    public float semiMajorAxis = 10f;  // radio mayor (a)
    public float eccentricity = 0.2f;  // 0 = círculo, 1 = parábola
    public int resolution = 64;

    [Header("Referencias")]
    public Transform sun;

    private SplineContainer _splineContainer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _splineContainer = GetComponent<SplineContainer>();
        GenerateOrbit();
    }

    // Update is called once per frame
    void Update()
    {
        
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
