using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Visualises a single Kepler 2nd-Law area sweep ("quesito") between the Sun and a planet.
    /// One call to RecordSweep() captures three points (Sun, planet at t0, planet at t0+T)
    /// and builds a triangle Mesh by code with a transparent emissive material so the
    /// swept area is visible in 3D space. Multiple sweeps can be queued -- each one creates
    /// a new child GameObject with its own Mesh.
    ///
    /// Designed for KeplerLab 2: dropping this on a service GameObject lets the
    /// KeplerLab2Controller (or the wrist menu) trigger sweeps at any orbit point so the
    /// student can compare a long-thin "quesito" near aphelion to a short-wide one near
    /// perihelion -- both with identical area.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Kepler Area Visualizer")]
    public sealed class KeplerAreaVisualizer : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[KeplerAreaVisualizer]";
        private const float MIN_SWEEP_DURATION = 0.05f;
        private const float MIN_VECTOR_SQR_MAGNITUDE = 1e-6f;
        private const string SWEEP_PARENT_NAME = "AreaSweepRoot";
        private const string SHADER_NAME = "Universal Render Pipeline/Unlit";
        private const string SHADER_FALLBACK_NAME = "Unlit/Color";

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("References")]
        [Tooltip("Transform of the Sun (orbit focus / triangle apex).")]
        [SerializeField] private Transform _sunTransform;

        [Tooltip("Transform of the planet whose swept area we are visualising.")]
        [SerializeField] private Transform _planetTransform;

        [Header("Sweep Settings")]
        [Tooltip("Real-time seconds between the two planet samples that close the triangle. " +
                 "Same value must be used for every sweep so areas can be compared.")]
        [SerializeField] private float _sweepDuration = 2f;

        [Tooltip("If true, each sweep created at runtime is parented under a single root " +
                 "GameObject for easy clean-up via ClearSweeps().")]
        [SerializeField] private bool _groupSweepsUnderSingleRoot = true;

        [Tooltip("If true, draws both faces of the triangle (front + back). Recommended for VR " +
                 "so the area is visible from any viewing angle.")]
        [SerializeField] private bool _drawDoubleSided = true;

        [Tooltip("Number of arc segments in the FIXED-DURATION (RecordSweep) mode. The visualiser " +
                 "samples the planet this many times along its real orbital path during the sweep " +
                 "duration and builds a triangle fan from the Sun to those samples. Higher = smoother " +
                 "arc on the outer edge of the quesito.")]
        [Range(1, 128)]
        [SerializeField] private int _arcSegments = 24;

        [Tooltip("Real-time seconds between samples in OPEN-ENDED (BeginSweep) mode. Smaller values " +
                 "give a smoother arc but cap the maximum sweep length the mesh can hold.")]
        [SerializeField] private float _sampleSpacing = 0.05f;

        [Tooltip("Maximum number of arc samples kept in memory during an open-ended sweep. " +
                 "Acts as a safety cap so a forgotten StopCapture cannot allocate without bound.")]
        [SerializeField] private int _maxArcSamples = 4096;

        [Header("Default Appearance")]
        [Tooltip("Default colour used when no override is passed to RecordSweep(). " +
                 "Alpha < 1 produces a translucent quesito.")]
        [SerializeField] private Color _defaultSweepColor = new Color(1f, 0.85f, 0.2f, 0.55f);

        #endregion

        #region Events ----------------------------------------------------------
        #endregion

        #region Cached Components -----------------------------------------------

        private Renderer _sunRenderer;
        private Transform _sweepRoot;

        // -- Open-ended (BeginSweep / EndSweep) sample state.
        private System.Collections.Generic.List<Vector3> _liveSamples = new();
        private Color _pendingColor;
        private string _pendingLabel;
        private Coroutine _activeSweep;
        private float _sweepStartTime;

        #endregion

        #region Public API ------------------------------------------------------

        /// <summary>True while a sweep is currently being recorded.</summary>
        public bool IsRecording { get; private set; }

        /// <summary>The duration in real-time seconds between the two planet samples (legacy fixed-duration mode).</summary>
        public float SweepDuration => Mathf.Max(_sweepDuration, MIN_SWEEP_DURATION);

        /// <summary>True once the visualiser has produced at least one sweep.</summary>
        public bool HasSweep { get; private set; }

        /// <summary>The integrated area of the most recently produced sweep (world units squared).</summary>
        public float LastSweptArea { get; private set; }

        /// <summary>The wall-clock duration (real-time seconds) of the most recently produced sweep.</summary>
        public float LastSweepDuration { get; private set; }

        /// <summary>
        /// Records one area sweep using the configured sweep duration and the default colour.
        /// </summary>
        public void RecordSweep()
        {
            RecordSweep(_defaultSweepColor, SweepDuration, null);
        }

        /// <summary>Records one area sweep using the configured duration and a custom colour.</summary>
        public void RecordSweep(Color color)
        {
            RecordSweep(color, SweepDuration, null);
        }

        /// <summary>Records one area sweep using a custom colour and duration.</summary>
        public void RecordSweep(Color color, float sweepDuration)
        {
            RecordSweep(color, sweepDuration, null);
        }

        /// <summary>
        /// Records one area sweep with full control over colour, duration and label.
        /// Returns silently if already recording another sweep.
        /// </summary>
        public void RecordSweep(Color color, float sweepDuration, string label)
        {
            if (IsRecording)
            {
                Debug.LogWarning($"{LOG_TAG} Sweep already in progress -- ignoring new request.", this);
                return;
            }

            if (_sunTransform == null || _planetTransform == null)
            {
                Debug.LogWarning($"{LOG_TAG} Missing Sun or Planet reference -- sweep aborted.", this);
                return;
            }

            float duration = Mathf.Max(sweepDuration, MIN_SWEEP_DURATION);
            _activeSweep = StartCoroutine(FixedDurationSweepCoroutine(color, duration, label));
        }

        /// <summary>
        /// Begins an OPEN-ENDED sweep that keeps sampling the planet's position every
        /// _sampleSpacing seconds until EndSweep() is called. This is the API used by
        /// KeplerLab2 when the player drives the start/stop with a button.
        /// Returns silently if a sweep is already in progress.
        /// </summary>
        public void BeginSweep(Color color, string label)
        {
            if (IsRecording)
            {
                Debug.LogWarning($"{LOG_TAG} Sweep already in progress -- BeginSweep ignored.", this);
                return;
            }
            if (_sunTransform == null || _planetTransform == null)
            {
                Debug.LogWarning($"{LOG_TAG} Missing Sun or Planet reference -- BeginSweep aborted.", this);
                return;
            }

            _pendingColor = color;
            _pendingLabel = label;
            _activeSweep  = StartCoroutine(OpenEndedSweepCoroutine());
        }

        /// <summary>
        /// Stops an open-ended sweep and finalises the mesh with whatever samples have
        /// been collected. Safe to call when no sweep is running -- it's a no-op.
        /// Returns the area of the swept mesh (also stored in LastSweptArea).
        /// </summary>
        public float EndSweep()
        {
            if (!IsRecording)
                return LastSweptArea;

            // Stop the coroutine and finalise.
            if (_activeSweep != null)
            {
                StopCoroutine(_activeSweep);
                _activeSweep = null;
            }
            FinalizeSweep(_pendingColor, _pendingLabel);
            return LastSweptArea;
        }

        /// <summary>Removes all previously generated sweep meshes from the scene.</summary>
        public void ClearSweeps()
        {
            // Re-resolve in case nothing has been swept yet on this visualiser but
            // another visualiser already created the shared QuesitoRoot.
            if (_sweepRoot == null)
            {
                GameObject existing = GameObject.Find(SWEEP_PARENT_NAME);
                if (existing == null) return;
                _sweepRoot = existing.transform;
            }

            for (int i = _sweepRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = _sweepRoot.GetChild(i);
                if (child != null) Destroy(child.gameObject);
            }

            Debug.Log($"{LOG_TAG} Cleared all sweeps.");
        }

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            TryResolveSunReference();
            TryResolvePlanetReference();
            CacheSunRenderer();
            EnsureSweepRoot();
            ValidateReferences();
        }

        #endregion

        #region Internals -------------------------------------------------------

        private IEnumerator FixedDurationSweepCoroutine(Color color, float duration, string label)
        {
            IsRecording = true;
            _pendingColor = color;
            _pendingLabel = label;
            _liveSamples.Clear();

            int segments = Mathf.Max(_arcSegments, 1);
            int sampleCount = segments + 1;

            _liveSamples.Add(_planetTransform.position);

            _sweepStartTime = Time.time;
            float startTime = _sweepStartTime;
            for (int i = 1; i < sampleCount; i++)
            {
                float targetTime = startTime + duration * i / segments;
                while (Time.time < targetTime)
                    yield return null;

                _liveSamples.Add(_planetTransform.position);
            }

            FinalizeSweep(color, label);
            _activeSweep = null;
        }

        private IEnumerator OpenEndedSweepCoroutine()
        {
            IsRecording = true;
            _liveSamples.Clear();
            _liveSamples.Add(_planetTransform.position);

            _sweepStartTime = Time.time;
            float spacing = Mathf.Max(_sampleSpacing, 0.005f);
            int   maxSamples = Mathf.Max(_maxArcSamples, 8);

            // Sample the planet's position at fixed intervals until EndSweep()
            // is called (which stops this coroutine and runs FinalizeSweep).
            while (_liveSamples.Count < maxSamples)
            {
                float nextSampleTime = _sweepStartTime + _liveSamples.Count * spacing;
                while (Time.time < nextSampleTime)
                    yield return null;

                _liveSamples.Add(_planetTransform.position);
            }

            // Hit the safety cap -- finalise to avoid running forever.
            Debug.LogWarning($"{LOG_TAG} Open-ended sweep hit max sample count ({maxSamples}) -- auto-finalising.", this);
            FinalizeSweep(_pendingColor, _pendingLabel);
            _activeSweep = null;
        }

        private void FinalizeSweep(Color color, string label)
        {
            // Always end up with IsRecording = false even on early-out.
            IsRecording = false;

            if (_liveSamples == null || _liveSamples.Count < 2)
            {
                Debug.LogWarning($"{LOG_TAG} Sweep finalised with too few samples -- mesh skipped.", this);
                return;
            }

            Vector3 sunPos = GetSunFocusPosition();
            Vector3[] arr = _liveSamples.ToArray();

            BuildSweepFanMesh(sunPos, arr, color, label);

            LastSweptArea     = ComputeFanArea(sunPos, arr);
            LastSweepDuration = Time.time - _sweepStartTime;
            HasSweep          = true;
        }

        private void BuildSweepFanMesh(Vector3 sunWorld, Vector3[] arcWorldSamples, Color color, string label)
        {
            if (arcWorldSamples == null || arcWorldSamples.Length < 2)
            {
                Debug.LogWarning($"{LOG_TAG} Sweep needs at least 2 arc samples -- mesh skipped.", this);
                return;
            }

            // Reject if the arc completely collapses on the Sun (degenerate).
            int validRadial = 0;
            for (int i = 0; i < arcWorldSamples.Length; i++)
            {
                if ((arcWorldSamples[i] - sunWorld).sqrMagnitude >= MIN_VECTOR_SQR_MAGNITUDE)
                    validRadial++;
            }
            if (validRadial < 2)
            {
                Debug.LogWarning($"{LOG_TAG} Sweep degenerate (planet too close to Sun) -- mesh skipped.", this);
                return;
            }

            EnsureSweepRoot();

            int siblingIndex = _sweepRoot != null ? _sweepRoot.childCount + 1 : 0;
            string objectName = string.IsNullOrEmpty(label)
                ? $"VFX_Quesito_{siblingIndex}"
                : $"VFX_Quesito_{label}";

            var sweep = new GameObject(objectName);
            // Anchor the mesh in world space at the Sun. Parenting under the shared
            // scene-root QuesitoRoot keeps the hierarchy tidy without making the
            // mesh follow any moving GameObject (the planet).
            if (_sweepRoot != null)
                sweep.transform.SetParent(_sweepRoot, worldPositionStays: true);
            sweep.transform.position = sunWorld;
            sweep.transform.rotation = Quaternion.identity;

            // Vertex layout: index 0 = Sun (apex), indices 1..N = planet samples
            // along the orbital arc. Triangle fan uses (0, i, i+1) for each
            // segment so adjacent planet samples form the curved outer edge.
            int sampleCount = arcWorldSamples.Length;
            int vertexCount = sampleCount + 1;
            int segmentCount = sampleCount - 1;
            int trianglesPerFace = segmentCount;

            Vector3[] vertices = new Vector3[vertexCount];
            Color[]   colors   = new Color[vertexCount];
            Vector3[] normals  = new Vector3[vertexCount];

            // Convert to local space (anchored at Sun) so the mesh is robust
            // against future world-transform changes.
            vertices[0] = Vector3.zero;
            colors[0]   = color;
            normals[0]  = Vector3.up;

            for (int i = 0; i < sampleCount; i++)
            {
                vertices[i + 1] = sweep.transform.InverseTransformPoint(arcWorldSamples[i]);
                colors[i + 1]   = color;
                normals[i + 1]  = Vector3.up;
            }

            // Front-face fan + (optional) back-face fan with reversed winding.
            int triangleIndexCount = trianglesPerFace * 3 * (_drawDoubleSided ? 2 : 1);
            int[] triangles = new int[triangleIndexCount];
            int t = 0;
            for (int i = 0; i < segmentCount; i++)
            {
                int a = 0;
                int b = i + 1;
                int c = i + 2;
                triangles[t++] = a;
                triangles[t++] = b;
                triangles[t++] = c;
            }
            if (_drawDoubleSided)
            {
                for (int i = 0; i < segmentCount; i++)
                {
                    int a = 0;
                    int b = i + 1;
                    int c = i + 2;
                    triangles[t++] = a;
                    triangles[t++] = c;
                    triangles[t++] = b;
                }
            }

            Mesh mesh = new Mesh
            {
                name = objectName + "_Mesh"
            };

            // Indices may exceed 65535 when _arcSegments is high; opt into 32-bit.
            mesh.indexFormat = vertexCount > 65535 || triangleIndexCount > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

            mesh.vertices  = vertices;
            mesh.triangles = triangles;
            mesh.normals   = normals;
            mesh.colors    = colors;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.UploadMeshData(false);

            var meshFilter   = sweep.AddComponent<MeshFilter>();
            var meshRenderer = sweep.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = CreateTransparentMaterial(color);
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            float radius0 = (arcWorldSamples[0] - sunWorld).magnitude;
            float radius1 = (arcWorldSamples[sampleCount - 1] - sunWorld).magnitude;
            float arcLen  = ComputeArcLength(arcWorldSamples);
            float area    = ComputeFanArea(sunWorld, arcWorldSamples);

            Debug.Log(
                $"{LOG_TAG} Sweep recorded -- label='{label ?? "(none)"}' segments={segmentCount} " +
                $"r0={radius0:F2} r1={radius1:F2} arcLen={arcLen:F2} area={area:F2}.");
        }

        private static Material CreateTransparentMaterial(Color color)
        {
            Shader shader = Shader.Find(SHADER_NAME);
            if (shader == null) shader = Shader.Find(SHADER_FALLBACK_NAME);

            var material = new Material(shader)
            {
                name = "M_KeplerArea_Runtime"
            };

            // URP/Unlit: configure transparency through standard properties when
            // they exist. These property names are stable across URP versions.
            if (material.HasProperty("_BaseColor"))     material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))         material.SetColor("_Color", color);
            if (material.HasProperty("_Surface"))       material.SetFloat("_Surface", 1f);    // 1 = Transparent
            if (material.HasProperty("_Blend"))         material.SetFloat("_Blend", 0f);      // 0 = Alpha
            if (material.HasProperty("_AlphaClip"))     material.SetFloat("_AlphaClip", 0f);
            if (material.HasProperty("_SrcBlend"))      material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))      material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))        material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_Cull"))          material.SetFloat("_Cull", (float)CullMode.Off);

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_OFF");

            material.renderQueue = (int)RenderQueue.Transparent;
            return material;
        }

        private static float ComputeFanArea(Vector3 apex, Vector3[] arc)
        {
            // Sum the area of each (apex, arc[i], arc[i+1]) sub-triangle.
            // For Kepler 2nd law this should be ~constant across sweeps of the
            // same duration, which is exactly the educational point.
            if (arc == null || arc.Length < 2) return 0f;

            float total = 0f;
            for (int i = 0; i < arc.Length - 1; i++)
                total += 0.5f * Vector3.Cross(arc[i] - apex, arc[i + 1] - apex).magnitude;

            return total;
        }

        private static float ComputeArcLength(Vector3[] arc)
        {
            if (arc == null || arc.Length < 2) return 0f;

            float total = 0f;
            for (int i = 0; i < arc.Length - 1; i++)
                total += (arc[i + 1] - arc[i]).magnitude;

            return total;
        }

        private void EnsureSweepRoot()
        {
            if (_sweepRoot != null) return;

            // CRITICAL: the sweep root must be a SCENE-ROOT GameObject (no parent),
            // NOT a child of this visualiser. The visualiser lives on the moving
            // planet, so parenting under it would drag every previously-drawn
            // quesito along the orbit. We anchor the meshes in world space so they
            // stay frozen at the moment they were captured.
            if (!_groupSweepsUnderSingleRoot)
            {
                // null parent + sweeps placed at world coordinates by BuildSweepFanMesh.
                _sweepRoot = null;
                return;
            }

            // Look up (or lazily create) a single shared scene-root GameObject so
            // every visualiser writes to the same place and ClearSweeps() can wipe
            // them all. Find by name walks all scene roots, which is fine here -- we
            // only do this once per visualiser at sweep time.
            GameObject existing = GameObject.Find(SWEEP_PARENT_NAME);
            if (existing != null)
            {
                _sweepRoot = existing.transform;
                return;
            }

            var rootGo = new GameObject(SWEEP_PARENT_NAME);
            // Intentionally NOT calling SetParent -- this is a scene root.
            _sweepRoot = rootGo.transform;
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

        private void TryResolvePlanetReference()
        {
            if (_planetTransform != null)
                return;

            // Prefer any active OrbitalMover in the scene as our target planet.
            OrbitalMover[] movers = FindObjectsByType<OrbitalMover>(FindObjectsSortMode.None);
            if (movers.Length > 0)
            {
                _planetTransform = movers[0].transform;
                Debug.Log($"{LOG_TAG} Auto-assigned _planetTransform: {_planetTransform.name}.");
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

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_sunTransform == null)
                Debug.LogWarning($"{LOG_TAG} _sunTransform is not assigned.", this);
            if (_planetTransform == null)
                Debug.LogWarning($"{LOG_TAG} _planetTransform is not assigned.", this);
            if (_sweepDuration < MIN_SWEEP_DURATION)
                Debug.LogWarning($"{LOG_TAG} _sweepDuration is below {MIN_SWEEP_DURATION} -- it will be clamped at runtime.", this);
        }

        #endregion
    }
}
