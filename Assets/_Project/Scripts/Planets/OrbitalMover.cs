using UnityEngine;

/// <summary>
/// Mueve un planeta en orbita kepleriana alrededor del Sol.
/// Pasa la direccion del periapsis a OrbitalSplineGenerator para que
/// la elipse dibujada coincida con la orbita real.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("ProyectoVR/Interaction/Orbital Mover")]
public sealed class OrbitalMover : MonoBehaviour
{
    #region Constants

    private const float TWO_PI = 2f * Mathf.PI;
    private const float KEPLER_TOLERANCE = 1e-6f;
    private const int KEPLER_MAX_ITERATIONS = 50;

    #endregion

    #region Inspector

    [Header("References")]
    [Tooltip("Transform del Sol (foco de la orbita).")]
    [SerializeField] private Transform _sunTransform;

    [Tooltip("Genera la geometria del spline al soltar el planeta.")]
    [SerializeField] private OrbitalSplineGenerator _splineGenerator;

    [Tooltip("Dibuja la linea de orbita.")]
    [SerializeField] private OrbitLineRenderer _orbitLineRenderer;

    #endregion

    #region State

    private float _semiMajorAxis;
    private float _eccentricity;
    private float _orbitalPeriod;
    private float _meanMotion;
    private float _trueAnomalyAtLaunch;
    private float _timeAtLaunch;
    private Vector3 _orbitNormal;
    private Vector3 _periapsisDirection;
    private bool _isOrbiting;

    #endregion

    #region Public API

    public void SetOrbitalElements(
        float semiMajorAxis,
        float eccentricity,
        float orbitalPeriod,
        float trueAnomalyAtLaunch,
        Vector3 orbitNormal,
        Vector3 periapsisDirection)
    {
        _semiMajorAxis = semiMajorAxis;
        _eccentricity = Mathf.Clamp(eccentricity, 0f, 0.99f);
        _orbitalPeriod = Mathf.Max(orbitalPeriod, 0.1f);
        _meanMotion = TWO_PI / _orbitalPeriod;
        _trueAnomalyAtLaunch = trueAnomalyAtLaunch;
        _orbitNormal = orbitNormal.normalized;
        _periapsisDirection = periapsisDirection.normalized;
        _timeAtLaunch = Time.time;
        _isOrbiting = true;

        // Pasar periapsisDirection al spline para que la elipse dibujada coincida
        if (_splineGenerator != null)
            _splineGenerator.UpdateOrbit(_semiMajorAxis, _eccentricity, _periapsisDirection);

        if (_orbitLineRenderer != null)
            _orbitLineRenderer.Redraw();

        Debug.Log($"[OrbitalMover] Orbit set -- a={_semiMajorAxis:F2} e={_eccentricity:F3} T={_orbitalPeriod:F1}s.");
    }

    public void StopOrbit()
    {
        _isOrbiting = false;
        if (_orbitLineRenderer != null)
            _orbitLineRenderer.Hide();
        Debug.Log("[OrbitalMover] Orbit stopped.");
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        ValidateReferences();
    }

    private void Update()
    {
        if (!_isOrbiting) return;
        transform.position = ComputePositionAtTime(Time.time - _timeAtLaunch);
    }

    #endregion

    #region Internals

    private Vector3 ComputePositionAtTime(float deltaTime)
    {
        float m0 = TrueToMeanAnomaly(_trueAnomalyAtLaunch, _eccentricity);
        float m = (m0 + _meanMotion * deltaTime) % TWO_PI;
        if (m < 0f) m += TWO_PI;

        float E = SolveKepler(m, _eccentricity);
        float nu = EccentricToTrueAnomaly(E, _eccentricity);

        float p = _semiMajorAxis * (1f - _eccentricity * _eccentricity);
        float radius = p / (1f + _eccentricity * Mathf.Cos(nu));

        // La direccion en el plano orbital: nu es el angulo desde el periapsis
        Vector3 radialDir = Mathf.Cos(nu) * _periapsisDirection
                          + Mathf.Sin(nu) * Vector3.Cross(_orbitNormal, _periapsisDirection);

        return _sunTransform.position + radialDir * radius;
    }

    private static float SolveKepler(float M, float e)
    {
        float E = M;
        for (int i = 0; i < KEPLER_MAX_ITERATIONS; i++)
        {
            float dE = (E - e * Mathf.Sin(E) - M) / (1f - e * Mathf.Cos(E));
            E -= dE;
            if (Mathf.Abs(dE) < KEPLER_TOLERANCE) break;
        }
        return E;
    }

    private static float TrueToMeanAnomaly(float nu, float e)
    {
        float E = 2f * Mathf.Atan(Mathf.Sqrt((1f - e) / (1f + e)) * Mathf.Tan(nu * 0.5f));
        return E - e * Mathf.Sin(E);
    }

    private static float EccentricToTrueAnomaly(float E, float e)
    {
        float cosNu = (Mathf.Cos(E) - e) / (1f - e * Mathf.Cos(E));
        float nu = Mathf.Acos(Mathf.Clamp(cosNu, -1f, 1f));
        if (E > Mathf.PI) nu = TWO_PI - nu;
        return nu;
    }

    #endregion

    #region Validation

    private void ValidateReferences()
    {
        if (_sunTransform == null)
            Debug.LogWarning("[OrbitalMover] _sunTransform is not assigned.", this);
        if (_splineGenerator == null)
            Debug.LogWarning("[OrbitalMover] _splineGenerator is not assigned.", this);
        if (_orbitLineRenderer == null)
            Debug.LogWarning("[OrbitalMover] _orbitLineRenderer is not assigned.", this);
    }

    #endregion
}
