using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Generates a Keplerian elliptical spline on a SplineContainer.
    /// Used by OrbitLineRenderer to draw the orbit and by SplineAnimate to move planets.
    /// Set generateOnStart to true for the SolarSystem scene; false for KeplerLab.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineContainer))]
    [AddComponentMenu("ProyectoVR/Planets/Orbital Spline Generator")]
    public class OrbitalSplineGenerator : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[OrbitalSplineGenerator]";
        private const int MIN_RESOLUTION = 8;
        private const float MIN_SEMI_MAJOR_AXIS = 0.01f;

        #endregion

        #region Inspector

        [Header("Orbit Parameters")]
        [Tooltip("Semi-major axis of the ellipse in world units.")]
        [SerializeField] private float semiMajorAxis = 10f;

        [Tooltip("Orbital eccentricity (0 = circle, approaching 1 = very elongated).")]
        [SerializeField, Range(0f, 0.99f)] private float eccentricity = 0.2f;

        [Tooltip("Number of knots that define the orbit spline. Higher = smoother ellipse.")]
        [SerializeField] private int resolution = 64;

        [Tooltip("If true, generates the orbit automatically on Start (SolarSystem scene). " +
                 "Set false for KeplerLab where the orbit is generated on planet release.")]
        [SerializeField] private bool generateOnStart = true;

        [Header("References")]
        [Tooltip("Transform of the Sun. Its world position is used as the orbit focus.")]
        [SerializeField] private Transform sun;

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private SplineContainer _splineContainer;
        private SplineAnimate   _splineAnimate;
        private Renderer _sunRenderer;
        private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        #endregion

        #region Public API

        /// <summary>
        /// Recomputes the orbit spline with new semi-major axis and eccentricity.
        /// Call from OrbitalLauncher when the player releases a planet.
        /// </summary>
        public void UpdateOrbit(float newA, float newE)
        {
            semiMajorAxis = newA;
            eccentricity  = Mathf.Clamp(newE, 0f, 0.99f);
            if (_splineContainer == null)
                _splineContainer = GetComponent<SplineContainer>();
            GenerateOrbit();
        }

        /// <summary>Overload that accepts a periapsis direction (currently unused by this generator).</summary>
        public void UpdateOrbit(float newA, float newE, Vector3 periapsisDir)
        {
            UpdateOrbit(newA, newE);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _splineContainer = GetComponent<SplineContainer>();
            TryResolveSunReference();
            CacheSunRenderer();

            _splineAnimate = GetComponentInChildren<SplineAnimate>();
            if (_splineAnimate != null)
                _splineAnimate.enabled = false;
        }

        private void Start()
        {
            ValidateReferences();
            if (generateOnStart)
                StartCoroutine(GenerateOrbitNextFrame());
        }

        #endregion

        #region Internals

        private IEnumerator GenerateOrbitNextFrame()
        {
            yield return _waitForEndOfFrame;
            GenerateOrbit();

            if (_splineAnimate != null)
                _splineAnimate.enabled = true;
        }

        private void GenerateOrbit()
        {
            if (_splineContainer == null)
            {
                Debug.LogWarning($"{LOG_TAG} _splineContainer is not assigned.", this);
                return;
            }

            int knotCount = Mathf.Max(resolution, MIN_RESOLUTION);
            float e = Mathf.Clamp(eccentricity, 0f, 0.99f);
            float a = Mathf.Max(semiMajorAxis, MIN_SEMI_MAJOR_AXIS);
            float b = a * Mathf.Sqrt(1f - e * e);
            float c = a * e;

            Vector3 sunPos = GetSunFocusPosition();

            var spline = _splineContainer.Spline;
            spline.Clear();

            for (int i = 0; i < knotCount; i++)
            {
                float angle    = (float)i / knotCount * 2f * Mathf.PI;
                float x        = Mathf.Cos(angle) * a - c;
                float z        = Mathf.Sin(angle) * b;
                var   position = new float3(sunPos.x + x, sunPos.y, sunPos.z + z);
                spline.Add(new BezierKnot(position), TangentMode.AutoSmooth);
            }

            spline.Closed = true;
        }

        private void TryResolveSunReference()
        {
            if (sun != null)
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
                    sun = candidate;
                    Debug.Log($"{LOG_TAG} Auto-assigned sun reference: {sun.name}.");
                    return;
                }

                if (fallback == null)
                    fallback = candidate;
            }

            if (fallback != null)
            {
                sun = fallback;
                Debug.Log($"{LOG_TAG} Auto-assigned fallback sun reference: {sun.name}.");
            }
        }

        private void CacheSunRenderer()
        {
            _sunRenderer = sun != null ? sun.GetComponentInChildren<Renderer>() : null;
        }

        private Vector3 GetSunFocusPosition()
        {
            if (sun == null)
                return Vector3.zero;

            if (_sunRenderer == null)
                CacheSunRenderer();

            return _sunRenderer != null ? _sunRenderer.bounds.center : sun.position;
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (sun == null)
                Debug.LogWarning($"{LOG_TAG} sun is not assigned -- orbit will be centered at world origin.", this);
            if (resolution < MIN_RESOLUTION)
                Debug.LogWarning($"{LOG_TAG} resolution is below {MIN_RESOLUTION} -- it will be clamped at runtime.", this);
        }

        #endregion
    }
}
