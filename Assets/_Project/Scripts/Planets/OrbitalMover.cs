using UnityEngine;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Moves a planet in a Keplerian orbit around the Sun using numerically solved orbital mechanics.
    /// Delegates spline geometry to OrbitalSplineGenerator and line drawing to OrbitLineRenderer.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Orbital Mover")]
    public sealed class OrbitalMover : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[OrbitalMover]";
        private const float TWO_PI = 2f * Mathf.PI;
        private const float KEPLER_TOLERANCE = 1e-6f;
        private const int KEPLER_MAX_ITERATIONS = 50;
        private const float MIN_SEMI_MAJOR_AXIS = 0.01f;
        private const float MIN_VECTOR_SQR_MAGNITUDE = 1e-6f;

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Transform of the Sun (orbit focus).")]
        [SerializeField] private Transform _sunTransform;

        [Tooltip("Generates the orbit spline geometry when the planet is released.")]
        [SerializeField] private OrbitalSplineGenerator _splineGenerator;

        [Tooltip("Draws the orbit line.")]
        [SerializeField] private OrbitLineRenderer _orbitLineRenderer;

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private Renderer _sunRenderer;

        #endregion

        #region Public API

        /// <summary>
        /// Sets all Keplerian orbital elements and starts the orbit simulation.
        /// Also triggers spline and line-renderer updates.
        /// </summary>
        public void SetOrbitalElements(
            float semiMajorAxis,
            float eccentricity,
            float orbitalPeriod,
            float trueAnomalyAtLaunch,
            Vector3 orbitNormal,
            Vector3 periapsisDirection)
        {
            _semiMajorAxis       = Mathf.Max(semiMajorAxis, MIN_SEMI_MAJOR_AXIS);
            _eccentricity        = Mathf.Clamp(eccentricity, 0f, 0.99f);
            _orbitalPeriod       = Mathf.Max(orbitalPeriod, 0.1f);
            _meanMotion          = TWO_PI / _orbitalPeriod;
            _trueAnomalyAtLaunch = trueAnomalyAtLaunch;
            _orbitNormal         = orbitNormal.sqrMagnitude > MIN_VECTOR_SQR_MAGNITUDE
                ? orbitNormal.normalized
                : Vector3.up;

            Vector3 projectedPeriapsis = Vector3.ProjectOnPlane(periapsisDirection, _orbitNormal);
            if (projectedPeriapsis.sqrMagnitude < MIN_VECTOR_SQR_MAGNITUDE)
                projectedPeriapsis = GetFallbackPeriapsisDirection(_orbitNormal);

            _periapsisDirection = projectedPeriapsis.normalized;
            _timeAtLaunch        = Time.time;
            _isOrbiting          = true;
            _hasLoggedMissingSun = false;

            if (_splineGenerator != null)
                _splineGenerator.UpdateOrbit(_semiMajorAxis, _eccentricity, _periapsisDirection);

            if (_orbitLineRenderer != null)
                _orbitLineRenderer.Redraw();

            Debug.Log($"{LOG_TAG} Orbit set -- a={_semiMajorAxis:F2} e={_eccentricity:F3} T={_orbitalPeriod:F1}s.");
        }

        /// <summary>Stops the orbit simulation and hides the orbit line.</summary>
        public void StopOrbit()
        {
            _isOrbiting = false;
            _hasLoggedMissingSun = false;
            if (_orbitLineRenderer != null)
                _orbitLineRenderer.Hide();
            Debug.Log($"{LOG_TAG} Orbit stopped.");
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            TryResolveSunReference();
            CacheSunRenderer();
            ValidateReferences();
        }

        private void Update()
        {
            if (!_isOrbiting) return;

            if (_sunTransform == null)
            {
                if (!_hasLoggedMissingSun)
                {
                    Debug.LogWarning($"{LOG_TAG} _sunTransform is not assigned -- orbit update skipped.", this);
                    _hasLoggedMissingSun = true;
                }

                return;
            }

            transform.position = ComputePositionAtTime(Time.time - _timeAtLaunch);
        }

        #endregion

        #region Internals

        private float   _semiMajorAxis;
        private float   _eccentricity;
        private float   _orbitalPeriod;
        private float   _meanMotion;
        private float   _trueAnomalyAtLaunch;
        private float   _timeAtLaunch;
        private Vector3 _orbitNormal;
        private Vector3 _periapsisDirection;
        private bool    _isOrbiting;
        private bool    _hasLoggedMissingSun;

        private Vector3 ComputePositionAtTime(float deltaTime)
        {
            float m0 = TrueToMeanAnomaly(_trueAnomalyAtLaunch, _eccentricity);
            float m  = (m0 + _meanMotion * deltaTime) % TWO_PI;
            if (m < 0f) m += TWO_PI;

            float E  = SolveKepler(m, _eccentricity);
            float nu = EccentricToTrueAnomaly(E, _eccentricity);

            float   p         = _semiMajorAxis * (1f - _eccentricity * _eccentricity);
            float   radius    = p / (1f + _eccentricity * Mathf.Cos(nu));
            Vector3 radialDir = Mathf.Cos(nu) * _periapsisDirection
                              + Mathf.Sin(nu) * Vector3.Cross(_orbitNormal, _periapsisDirection);

            return GetSunFocusPosition() + radialDir * radius;
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
            float nu    = Mathf.Acos(Mathf.Clamp(cosNu, -1f, 1f));
            if (E > Mathf.PI) nu = TWO_PI - nu;
            return nu;
        }

        private static Vector3 GetFallbackPeriapsisDirection(Vector3 orbitNormal)
        {
            Vector3 axis = Mathf.Abs(orbitNormal.y) < 0.9f ? Vector3.up : Vector3.right;
            return Vector3.Cross(orbitNormal, axis).normalized;
        }

        private void TryResolveSunReference()
        {
            if (_sunTransform != null)
                return;

            Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            Transform fallback = null;

            for (int i = 0; i < sceneTransforms.Length; i++)
            {
                Transform candidate = sceneTransforms[i];
                string candidateName = candidate.name.ToLowerInvariant();
                bool isLikelySun =
                    candidateName == "sun" ||
                    candidateName == "sol" ||
                    candidateName.Contains("sun") ||
                    candidateName.Contains("sol");

                if (!isLikelySun)
                    continue;

                if (candidate.GetComponentInChildren<Renderer>() != null)
                {
                    _sunTransform = candidate;
                    Debug.Log($"{LOG_TAG} Auto-assigned _sunTransform: {_sunTransform.name}.");
                    return;
                }

                if (fallback == null)
                    fallback = candidate;
            }

            if (fallback != null)
            {
                _sunTransform = fallback;
                Debug.Log($"{LOG_TAG} Auto-assigned fallback _sunTransform: {_sunTransform.name}.");
            }
        }

        private void CacheSunRenderer()
        {
            _sunRenderer = _sunTransform != null ? _sunTransform.GetComponentInChildren<Renderer>() : null;
        }

        private Vector3 GetSunFocusPosition()
        {
            if (_sunTransform == null)
                return Vector3.zero;

            if (_sunRenderer == null)
                CacheSunRenderer();

            return _sunRenderer != null ? _sunRenderer.bounds.center : _sunTransform.position;
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_sunTransform == null)
                Debug.LogWarning($"{LOG_TAG} _sunTransform is not assigned.", this);
            if (_splineGenerator == null)
                Debug.LogWarning($"{LOG_TAG} _splineGenerator is not assigned.", this);
            if (_orbitLineRenderer == null)
                Debug.LogWarning($"{LOG_TAG} _orbitLineRenderer is not assigned.", this);
        }

        #endregion
    }
}
