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

        [Tooltip("Orbital inclination in degrees relative to the ecliptic (XZ) plane. " +
                 "Real values: Mercury 7°, Venus 3.4°, Mars 1.85°, Jupiter 1.3°, Saturn 2.49°, Moon 5.1°.")]
        [SerializeField, Range(0f, 90f)] private float _inclination = 0f;

        [Tooltip("Number of knots that define the orbit spline. Higher = smoother ellipse.")]
        [SerializeField] private int resolution = 64;

        [Tooltip("If true, generates the orbit automatically on Start (SolarSystem scene). " +
                 "Set false for KeplerLab where the orbit is generated on planet release.")]
        [SerializeField] private bool generateOnStart = true;

        [Tooltip("If true, re-enables the child SplineAnimate after generating the orbit.")]
        [SerializeField] private bool _enableSplineAnimateOnGenerate = true;

        [Header("References")]
        [Tooltip("Transform of the orbit focus. Use the Sun for planets and Earth for the Moon.")]
        [SerializeField] private Transform sun;

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private SplineContainer  _splineContainer;
        private SplineAnimate    _splineAnimate;
        private OrbitLineRenderer _orbitLineRenderer;
        private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

        // Direccion del periapsis en LOCAL space del SplineContainer.
        // Por defecto +X para preservar el comportamiento del SolarSystem original
        // (donde el periapsis siempre apuntaba a +X). Cuando el OrbitalLauncher
        // suelta un planeta llama al overload UpdateOrbit(a, e, dir) y este vector
        // se reorienta para que la elipse pase por el punto de soltado.
        private Vector3 _periapsisDirLocal = Vector3.right;

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
            _orbitLineRenderer?.Redraw();
        }

        /// <summary>
        /// Overload que ademas orienta la elipse para que su periapsis quede en la
        /// direccion indicada (en world space). Esto hace que el planeta arranque
        /// la orbita exactamente desde donde el jugador la solto.
        /// </summary>
        public void UpdateOrbit(float newA, float newE, Vector3 periapsisDir)
        {
            // Convertimos la direccion world->local del SplineContainer y la
            // proyectamos al plano horizontal (la elipse se traza en XZ y luego
            // se inclina segun _inclination).
            Vector3 dirLocal = transform.InverseTransformDirection(periapsisDir);
            dirLocal.y = 0f;
            if (dirLocal.sqrMagnitude < 1e-6f)
                dirLocal = Vector3.right;
            _periapsisDirLocal = dirLocal.normalized;

            UpdateOrbit(newA, newE);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _splineContainer   = GetComponent<SplineContainer>();
            _orbitLineRenderer = GetComponent<OrbitLineRenderer>();
            TryResolveSunReference();

            _splineAnimate = GetComponentInChildren<SplineAnimate>();
            if (_splineAnimate != null)
                _splineAnimate.enabled = false;
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
                return;

            if (_splineContainer == null)
                _splineContainer = GetComponent<SplineContainer>();

            if (_orbitLineRenderer == null)
                _orbitLineRenderer = GetComponent<OrbitLineRenderer>();

            TryResolveSunReference();

            if (_splineAnimate == null)
                _splineAnimate = GetComponentInChildren<SplineAnimate>();

            GenerateOrbit();

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

            int knotCount = _splineContainer != null ? _splineContainer.Spline.Count : 0;
            Debug.Log($"{LOG_TAG} Generated orbit -- knots: {knotCount}, GO: {gameObject.name}.");

            if (_orbitLineRenderer != null)
                _orbitLineRenderer.Redraw();

            if (_enableSplineAnimateOnGenerate && _splineAnimate != null && knotCount >= MIN_RESOLUTION)
            {
                _splineAnimate.enabled = true;
                Debug.Log($"{LOG_TAG} SplineAnimate enabled -- knots validated.");
            }
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

            // Convert focus (Sun/Earth) to LOCAL space of this SplineContainer.
            // For planets: SplineContainer is at origin → focusLocal ≈ (0,0,0), no change.
            // For Moon: SplineContainer is child of Earth at local (0,0,0) → focusLocal = (0,0,0),
            // so the orbit is generated centred at Earth and moves with it as Earth orbits the Sun.
            Vector3 focusLocal = sun != null
                ? transform.InverseTransformPoint(GetSunFocusPosition())
                : Vector3.zero;

            // Base ortonormal en el plano XZ local: periDir es la direccion del
            // periapsis, perpDir es 90deg CCW dentro del plano. La elipse se
            // genera con la formula parametrica clasica:
            //   p(E) = focus + (a*cos(E) - c) * periDir + b*sin(E) * perpDir
            Vector3 periDir = _periapsisDirLocal.sqrMagnitude > 1e-6f
                            ? new Vector3(_periapsisDirLocal.x, 0f, _periapsisDirLocal.z).normalized
                            : Vector3.right;
            // perpDir = up x periDir → rota periDir 90 grados CCW en el plano XZ.
            Vector3 perpDir = new Vector3(-periDir.z, 0f, periDir.x);

            float incRad = _inclination * Mathf.Deg2Rad;
            float sinInc = Mathf.Sin(incRad);
            float cosInc = Mathf.Cos(incRad);

            // Inclinacion: pivotamos el eje "perpendicular al periapsis" hacia
            // arriba. Para inclinacion = 0, perpDirInclined == perpDir (orbita en
            // el plano horizontal). Para inclinacion > 0, la orbita se inclina
            // alrededor del eje del periapsis (que es como rotaba el codigo
            // original, pero ahora generalizado a cualquier direccion de periapsis).
            Vector3 perpDirInclined = perpDir * cosInc + Vector3.up * sinInc;

            var spline = _splineContainer.Spline;
            spline.Clear();

            for (int i = 0; i < knotCount; i++)
            {
                float angle  = (float)i / knotCount * 2f * Mathf.PI;
                float along  = Mathf.Cos(angle) * a - c;
                float across = Mathf.Sin(angle) * b;

                Vector3 posLocal = focusLocal + periDir * along + perpDirInclined * across;
                spline.Add(new BezierKnot(new float3(posLocal.x, posLocal.y, posLocal.z)),
                           TangentMode.AutoSmooth);
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

        private Vector3 GetSunFocusPosition() => sun != null ? sun.position : Vector3.zero;

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
