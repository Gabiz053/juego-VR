using UnityEngine;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Self-contained Keplerian orbiter built for KeplerLab 2.
    /// On scene start it auto-launches the orbit using inspector-configured Keplerian
    /// elements, automatically creates a sphere child for visualisation (if none exists)
    /// and draws its own orbit line as a "painted trajectory" so the student can see
    /// the shape the planet is going to follow.
    ///
    /// Pause()/Resume() freeze ONLY this orbiter -- they do NOT touch Time.timeScale --
    /// so the VR rig and locomotion keep responding while the simulation is stopped for
    /// the area-comparison phase of the lab.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Kepler Lab Orbiter")]
    public sealed class KeplerLabOrbiter : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[KeplerLabOrbiter]";
        private const float TWO_PI = 2f * Mathf.PI;
        private const float KEPLER_TOLERANCE = 1e-6f;
        private const int   KEPLER_MAX_ITERATIONS = 50;
        private const float MIN_SEMI_MAJOR_AXIS = 0.05f;
        private const float MIN_PERIOD = 0.1f;
        private const float MIN_VECTOR_SQR_MAGNITUDE = 1e-6f;
        private const int   MIN_LINE_SEGMENTS = 16;
        private const string ORBIT_LINE_NAME = "OrbitLine";
        private const string SPHERE_CHILD_NAME = "PlanetMesh";
        private const string LINE_SHADER_NAME = "Universal Render Pipeline/Unlit";
        private const string LINE_SHADER_FALLBACK = "Unlit/Color";
        private const string SPHERE_SHADER_NAME = "Universal Render Pipeline/Lit";
        private const string SPHERE_SHADER_FALLBACK = "Standard";

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("References")]
        [Tooltip("Transform of the Sun (orbit focus). If left blank we try to auto-find " +
                 "any GameObject named like 'Sun' / 'Sol' in the scene.")]
        [SerializeField] private Transform _sunTransform;

        [Header("Orbital Elements")]
        [Tooltip("Semi-major axis of the ellipse in world units.")]
        [SerializeField] private float _semiMajorAxis = 4f;

        [Tooltip("Orbital eccentricity (0 = circle, approaching 1 = very elongated). " +
                 "A value of 0.4-0.7 looks great for the equal-areas demo.")]
        [Range(0f, 0.95f)]
        [SerializeField] private float _eccentricity = 0.55f;

        [Tooltip("Orbital period in real-time seconds. Smaller = faster orbit.")]
        [SerializeField] private float _orbitalPeriod = 18f;

        [Tooltip("Initial position along the orbit, expressed as a fraction of the period " +
                 "(0 = perihelion, 0.5 = aphelion). Useful so two planets are not synced.")]
        [Range(0f, 1f)]
        [SerializeField] private float _initialPhase01 = 0f;

        [Tooltip("Direction perpendicular to the orbital plane (typically Vector3.up).")]
        [SerializeField] private Vector3 _orbitNormal = Vector3.up;

        [Tooltip("Direction from the focus toward the orbit's perihelion (closest approach). " +
                 "Will be projected onto the orbit plane.")]
        [SerializeField] private Vector3 _periapsisDirection = Vector3.right;

        [Header("Visuals")]
        [Tooltip("If true, a built-in sphere child is created on Awake when this GameObject " +
                 "has no existing MeshRenderer. Disable if you parent your own mesh.")]
        [SerializeField] private bool _autoCreatePlanetMesh = true;

        [Tooltip("Diameter of the auto-created sphere (only used when Auto Create Planet " +
                 "Mesh is enabled).")]
        [SerializeField] private float _planetDiameter = 0.6f;

        [Tooltip("Tint applied to the auto-created sphere and to the orbit line.")]
        [SerializeField] private Color _planetColor = new Color(0.9f, 0.65f, 0.3f, 1f);

        [Header("Orbit Line")]
        [Tooltip("If true, an orbit line ('painted trajectory') is drawn as a child LineRenderer.")]
        [SerializeField] private bool _drawOrbitLine = true;

        [Tooltip("Number of segments in the orbit line. Higher = smoother ellipse.")]
        [SerializeField] private int _orbitLineSegments = 96;

        [Tooltip("Width of the orbit line in world units.")]
        [SerializeField] private float _orbitLineWidth = 0.03f;

        [Tooltip("Tint applied to the orbit line. Alpha < 1 looks great.")]
        [SerializeField] private Color _orbitLineColor = new Color(0.7f, 0.85f, 1f, 0.7f);

        #endregion

        #region Events ----------------------------------------------------------
        #endregion

        #region Cached Components -----------------------------------------------

        private LineRenderer _orbitLine;
        private Transform    _planetMeshChild;
        private Renderer     _sunRenderer;

        #endregion

        #region State -----------------------------------------------------------

        private float   _meanMotion;
        private float   _meanAnomalyAtLaunch;
        private float   _timeAtLaunch;
        private bool    _isOrbiting;
        private bool    _isPaused;
        private float   _pausedDeltaTime;

        #endregion

        #region Public API ------------------------------------------------------

        /// <summary>True while the orbiter is paused (NOT global time-scale paused).</summary>
        public bool IsPaused => _isPaused;

        /// <summary>True once the orbit has been launched and not stopped.</summary>
        public bool IsOrbiting => _isOrbiting;

        /// <summary>The semi-major axis configured for this orbit.</summary>
        public float SemiMajorAxis => _semiMajorAxis;

        /// <summary>The eccentricity configured for this orbit.</summary>
        public float Eccentricity => _eccentricity;

        /// <summary>The orbital period (real-time seconds) configured for this orbit.</summary>
        public float OrbitalPeriod => _orbitalPeriod;

        /// <summary>World-space position of the Sun's renderer bounds centre (or transform).</summary>
        public Vector3 SunFocusPosition
        {
            get
            {
                if (_sunTransform == null) return Vector3.zero;
                if (_sunRenderer == null)  CacheSunRenderer();
                return _sunRenderer != null ? _sunRenderer.bounds.center : _sunTransform.position;
            }
        }

        /// <summary>Distance from the planet to the Sun focus -- used for aphelion/perihelion detection.</summary>
        public float CurrentDistanceToSun => Vector3.Distance(transform.position, SunFocusPosition);

        /// <summary>
        /// Pauses the orbiter without touching Time.timeScale.
        /// Belt-and-suspenders: also flips this component's `enabled` flag so Update()
        /// cannot run even if the _isPaused gate is bypassed somehow.
        /// </summary>
        public void Pause()
        {
            if (_isPaused) return;

            _pausedDeltaTime = Time.time - _timeAtLaunch;
            _isPaused = true;
            // Stops Unity from calling Update() on this component at all.
            this.enabled = false;
            Debug.Log($"{LOG_TAG} '{name}' paused at t={_pausedDeltaTime:F2}s.");
        }

        /// <summary>Resumes the orbiter from where it was paused.</summary>
        public void Resume()
        {
            if (!_isPaused) return;

            // Shift the launch time so the orbit picks up at the same point.
            _timeAtLaunch = Time.time - _pausedDeltaTime;
            _isPaused = false;
            this.enabled = true;
            Debug.Log($"{LOG_TAG} '{name}' resumed at t={_pausedDeltaTime:F2}s.");
        }

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Awake()
        {
            EnsurePlanetMesh();
            EnsureOrbitLine();
        }

        private void Start()
        {
            TryResolveSunReference();
            CacheSunRenderer();
            ValidateReferences();
            LaunchOrbit();
            RedrawOrbitLine();
        }

        private void Update()
        {
            if (!_isOrbiting || _isPaused) return;
            if (_sunTransform == null) return;

            transform.position = ComputePositionAtTime(Time.time - _timeAtLaunch);
        }

        #endregion

        #region Internals -- Orbit Launch and Math ------------------------------

        private void LaunchOrbit()
        {
            float a = Mathf.Max(_semiMajorAxis, MIN_SEMI_MAJOR_AXIS);
            float e = Mathf.Clamp(_eccentricity, 0f, 0.95f);
            float t = Mathf.Max(_orbitalPeriod, MIN_PERIOD);

            _semiMajorAxis = a;
            _eccentricity  = e;
            _orbitalPeriod = t;
            _meanMotion    = TWO_PI / t;

            // Convert phase (0..1) directly to mean anomaly (0..2pi).
            _meanAnomalyAtLaunch = Mathf.Clamp01(_initialPhase01) * TWO_PI;
            _timeAtLaunch        = Time.time;
            _isOrbiting          = true;

            if (_orbitNormal.sqrMagnitude < MIN_VECTOR_SQR_MAGNITUDE)
                _orbitNormal = Vector3.up;
            _orbitNormal = _orbitNormal.normalized;

            Vector3 projectedPeriapsis = Vector3.ProjectOnPlane(_periapsisDirection, _orbitNormal);
            if (projectedPeriapsis.sqrMagnitude < MIN_VECTOR_SQR_MAGNITUDE)
                projectedPeriapsis = GetFallbackPeriapsisDirection(_orbitNormal);
            _periapsisDirection = projectedPeriapsis.normalized;

            // Snap to the launch position so we don't see the planet teleport on first frame.
            transform.position = ComputePositionAtTime(0f);

            Debug.Log($"{LOG_TAG} '{name}' launched -- a={a:F2} e={e:F3} T={t:F1}s phase={_initialPhase01:F2}.");
        }

        private Vector3 ComputePositionAtTime(float deltaTime)
        {
            float m  = (_meanAnomalyAtLaunch + _meanMotion * deltaTime) % TWO_PI;
            if (m < 0f) m += TWO_PI;

            float E  = SolveKepler(m, _eccentricity);
            float nu = EccentricToTrueAnomaly(E, _eccentricity);

            float   p         = _semiMajorAxis * (1f - _eccentricity * _eccentricity);
            float   radius    = p / (1f + _eccentricity * Mathf.Cos(nu));
            Vector3 radialDir = Mathf.Cos(nu) * _periapsisDirection
                              + Mathf.Sin(nu) * Vector3.Cross(_orbitNormal, _periapsisDirection);

            return SunFocusPosition + radialDir * radius;
        }

        private static float SolveKepler(float meanAnomaly, float e)
        {
            float E = meanAnomaly;
            for (int i = 0; i < KEPLER_MAX_ITERATIONS; i++)
            {
                float dE = (E - e * Mathf.Sin(E) - meanAnomaly) / (1f - e * Mathf.Cos(E));
                E -= dE;
                if (Mathf.Abs(dE) < KEPLER_TOLERANCE) break;
            }
            return E;
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

        #endregion

        #region Internals -- Visuals --------------------------------------------

        private void EnsurePlanetMesh()
        {
            if (!_autoCreatePlanetMesh) return;
            if (GetComponent<MeshRenderer>() != null) return;

            // Already created on a previous Awake?
            Transform existing = transform.Find(SPHERE_CHILD_NAME);
            if (existing != null)
            {
                _planetMeshChild = existing;
                return;
            }

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = SPHERE_CHILD_NAME;
            sphere.transform.SetParent(transform, worldPositionStays: false);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale    = Vector3.one * Mathf.Max(_planetDiameter, 0.01f);

            // Strip the auto-added collider -- a planet doesn't need to bump into things here.
            Collider col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = sphere.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = CreatePlanetMaterial(_planetColor);

            _planetMeshChild = sphere.transform;
        }

        private void EnsureOrbitLine()
        {
            if (!_drawOrbitLine)
            {
                if (_orbitLine != null) _orbitLine.enabled = false;
                return;
            }

            if (_orbitLine != null) return;

            Transform existing = transform.Find(ORBIT_LINE_NAME);
            if (existing != null)
            {
                _orbitLine = existing.GetComponent<LineRenderer>();
                if (_orbitLine != null) return;
            }

            var lineGo = new GameObject(ORBIT_LINE_NAME);
            // Parent under the SCENE (worldPositionStays=true) so the line is drawn in
            // world space relative to the Sun, not following the planet around its orbit.
            lineGo.transform.SetParent(transform.parent, worldPositionStays: true);
            lineGo.transform.position = Vector3.zero;
            lineGo.transform.rotation = Quaternion.identity;

            _orbitLine = lineGo.AddComponent<LineRenderer>();
            _orbitLine.useWorldSpace = true;
            _orbitLine.loop          = true;
            _orbitLine.startWidth    = _orbitLineWidth;
            _orbitLine.endWidth      = _orbitLineWidth;
            _orbitLine.numCapVertices    = 4;
            _orbitLine.numCornerVertices = 4;
            _orbitLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _orbitLine.receiveShadows    = false;

            _orbitLine.sharedMaterial = CreateOrbitLineMaterial(_orbitLineColor);
            _orbitLine.startColor     = _orbitLineColor;
            _orbitLine.endColor       = _orbitLineColor;
        }

        private void RedrawOrbitLine()
        {
            if (!_drawOrbitLine || _orbitLine == null) return;

            int segments = Mathf.Max(_orbitLineSegments, MIN_LINE_SEGMENTS);
            _orbitLine.positionCount = segments;
            float a = _semiMajorAxis;
            float e = _eccentricity;

            // Walk one period worth of mean-anomaly samples so the spacing follows
            // the same parameterisation as Update() and aphelion/perihelion show
            // up exactly opposite each other.
            for (int i = 0; i < segments; i++)
            {
                float meanAnomaly = (i / (float)segments) * TWO_PI;
                float E  = SolveKepler(meanAnomaly, e);
                float nu = EccentricToTrueAnomaly(E, e);

                float   p         = a * (1f - e * e);
                float   radius    = p / (1f + e * Mathf.Cos(nu));
                Vector3 radialDir = Mathf.Cos(nu) * _periapsisDirection
                                  + Mathf.Sin(nu) * Vector3.Cross(_orbitNormal, _periapsisDirection);

                _orbitLine.SetPosition(i, SunFocusPosition + radialDir * radius);
            }
        }

        private static Material CreatePlanetMaterial(Color color)
        {
            Shader shader = Shader.Find(SPHERE_SHADER_NAME);
            if (shader == null) shader = Shader.Find(SPHERE_SHADER_FALLBACK);

            var mat = new Material(shader)
            {
                name = "M_KeplerPlanet_Runtime"
            };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            return mat;
        }

        private static Material CreateOrbitLineMaterial(Color color)
        {
            Shader shader = Shader.Find(LINE_SHADER_NAME);
            if (shader == null) shader = Shader.Find(LINE_SHADER_FALLBACK);

            var mat = new Material(shader)
            {
                name = "M_KeplerOrbitLine_Runtime"
            };

            // Set up additive-friendly transparent blend so the line glows over space.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            if (mat.HasProperty("_Surface"))   mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))     mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip")) mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend"))  mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend"))  mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))    mat.SetFloat("_ZWrite", 0f);

            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return mat;
        }

        #endregion

        #region Internals -- Sun Resolution -------------------------------------

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

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_sunTransform == null)
                Debug.LogWarning($"{LOG_TAG} '{name}' has no _sunTransform assigned.", this);
            if (_semiMajorAxis < MIN_SEMI_MAJOR_AXIS)
                Debug.LogWarning($"{LOG_TAG} '{name}' semi-major axis below {MIN_SEMI_MAJOR_AXIS} -- clamped at runtime.", this);
            if (_orbitalPeriod < MIN_PERIOD)
                Debug.LogWarning($"{LOG_TAG} '{name}' orbital period below {MIN_PERIOD} -- clamped at runtime.", this);
        }

        #endregion
    }
}
