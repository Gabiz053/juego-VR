using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Generates a Keplerian elliptical spline in world space from release position.
    /// Replaces OrbitalSplineGenerator for KeplerLab spawned planets.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Kepler Orbit Spline Generator")]
    [RequireComponent(typeof(SplineContainer))]
    public sealed class KeplerOrbitSplineGenerator : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[KeplerOrbitSplineGenerator]";
        private const int RESOLUTION = 64;
        private const float MIN_AXIS = 0.01f;

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Transform del Sol (foco de la orbita).")]
        [SerializeField] private Transform _sunTransform;

        [Tooltip("OrbitLineRenderer para redibujar la linea de orbita.")]
        [SerializeField] private OrbitLineRenderer _orbitLineRenderer;

        #endregion

        #region Cached Components

        private SplineContainer _splineContainer;

        #endregion

        #region Public API

        /// <summary>
        /// Genera el spline de la orbita en world space a partir de los elementos keplerianos
        /// y la posicion/direccion de periapsis en el momento del release.
        /// </summary>
        public void GenerateOrbit(
            float semiMajorAxis,
            float eccentricity,
            Vector3 periapsisDirection,
            Vector3 orbitNormal)
        {
            if (_splineContainer == null)
                _splineContainer = GetComponent<SplineContainer>();

            float a = Mathf.Max(semiMajorAxis, MIN_AXIS);
            float e = Mathf.Clamp(eccentricity, 0f, 0.99f);
            float b = a * Mathf.Sqrt(1f - e * e);
            float c = a * e; // distancia foco-centro

            Vector3 sunPos = GetSunPosition();
            Vector3 periDir = periapsisDirection.sqrMagnitude > 0.001f
                                 ? periapsisDirection.normalized
                                 : Vector3.right;
            Vector3 normal = orbitNormal.sqrMagnitude > 0.001f
                                 ? orbitNormal.normalized
                                 : Vector3.up;

            // Eje perpendicular en el plano orbital
            Vector3 semiLatDir = Vector3.Cross(normal, periDir).normalized;

            // Centro de la elipse = foco + c * direccion_periapsis
            Vector3 ellipseCenter = sunPos + periDir * c;

            var spline = _splineContainer.Spline;
            spline.Clear();

            for (int i = 0; i < RESOLUTION; i++)
            {
                float angle = (float)i / RESOLUTION * 2f * Mathf.PI;
                // Posicion en world space
                Vector3 worldPos = ellipseCenter
                                 + periDir * (Mathf.Cos(angle) * a)
                                 + semiLatDir * (Mathf.Sin(angle) * b);

                // Convertir a local del SplineContainer
                Vector3 localPos = _splineContainer.transform.InverseTransformPoint(worldPos);
                spline.Add(new BezierKnot((float3)localPos), TangentMode.AutoSmooth);
            }

            spline.Closed = true;

            _orbitLineRenderer?.Redraw();

            Debug.Log($"{LOG_TAG} Orbit generated -- a={a:F2} e={e:F3} center={ellipseCenter}.");
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _splineContainer = GetComponent<SplineContainer>();
            TryResolveSun();
        }

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Internals

        private Vector3 GetSunPosition()
        {
            if (_sunTransform == null) return Vector3.zero;
            Renderer r = _sunTransform.GetComponentInChildren<Renderer>();
            return r != null ? r.bounds.center : _sunTransform.position;
        }

        private void TryResolveSun()
        {
            if (_sunTransform != null) return;

            foreach (Transform t in FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                string n = t.name.ToLowerInvariant();
                if ((n == "sun" || n == "sol" || n.Contains("sun") || n.Contains("sol"))
                    && t.GetComponentInChildren<Renderer>() != null)
                {
                    _sunTransform = t;
                    Debug.Log($"{LOG_TAG} Auto-assigned sun: {t.name}.");
                    return;
                }
            }
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_sunTransform == null)
                Debug.LogWarning($"{LOG_TAG} _sunTransform is not assigned.", this);
            if (_orbitLineRenderer == null)
                Debug.LogWarning($"{LOG_TAG} _orbitLineRenderer is not assigned.", this);
        }

        #endregion
    }
}