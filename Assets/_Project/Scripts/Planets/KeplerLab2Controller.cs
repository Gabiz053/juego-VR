using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Kepler 2nd-Law lab #2 controller. Drives a button-triggered "capture window":
    /// the player presses a button (Space by default, or any control wired to
    /// ToggleCapture()) to start every planet sweeping simultaneously, then presses
    /// the same button again to stop. The instant capture stops, every orbiter is
    /// frozen via KeplerLabOrbiter.Pause() (which leaves Time.timeScale = 1 so VR
    /// locomotion keeps responding) and an on-screen panel reports the integrated
    /// area for each sweep grouped by orbit -- demonstrating Kepler's 2nd Law:
    /// for the same dt the area dA is the same regardless of where on the orbit
    /// the planet was.
    ///
    /// On scene start the controller also rotates the player's rig so the camera
    /// is horizontally facing the Sun, and creates a world-space TMP message so
    /// the student knows what to do without having to alt-tab to docs.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Kepler Lab 2 Controller")]
    public sealed class KeplerLab2Controller : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[KeplerLab2Controller]";
        private const float MIN_FALLOFF = 0.001f;
        private const string TMP_FONT_RESOURCE_PATH = "Fonts & Materials/LiberationSans SDF";

        #endregion

        #region Inspector Types -------------------------------------------------

        [Serializable]
        public class LabPlanet
        {
            [Tooltip("Orbiter for this planet (Pause/Resume + orbit math).")]
            public KeplerLabOrbiter _orbiter;

            [Tooltip("Visualiser that draws this planet's quesito. " +
                     "If left blank we GetComponent<KeplerAreaVisualizer>() on the orbiter.")]
            public KeplerAreaVisualizer _visualizer;

            [Tooltip("Short label used in debug logs and the area summary.")]
            public string _label = "Planet";

            [Tooltip("Group name used to bucket sweeps in the area summary " +
                     "(e.g. \"Inner\" / \"Outer\"). Same group = expected to have equal areas.")]
            public string _orbitGroup = "Default";

            [Tooltip("Colour applied to this planet's quesito.")]
            public Color _sweepColor = new Color(1f, 0.55f, 0.2f, 0.55f);
        }

        private enum LabState
        {
            Idle,        // Awaiting first toggle.
            Capturing,   // Sweeps in progress.
            Complete     // Sweeps captured, orbiters frozen, area summary displayed.
        }

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Planets")]
        [Tooltip("Every planet listed here sweeps a quesito SIMULTANEOUSLY when capture starts.")]
        [SerializeField] private List<LabPlanet> _planets = new();

        [Header("Capture")]
        [Tooltip("Keyboard key that toggles capture (Idle -> Capturing -> Complete -> Idle...). " +
                 "Wire ToggleCapture() to a UI button or controller binding for VR.")]
        [SerializeField] private Key _toggleKey = Key.Space;

        [Tooltip("If true, every orbiter is paused immediately when capture stops. Pause is " +
                 "per-orbiter -- Time.timeScale is NOT touched, so VR locomotion keeps working.")]
        [SerializeField] private bool _pauseOnCaptureStop = true;

        [Header("Player Setup")]
        [Tooltip("If true, on Start the XR Rig is rotated horizontally so the main camera faces the Sun.")]
        [SerializeField] private bool _faceSunOnStart = true;

        [Tooltip("Sun reference used by both the face-sun setup and the area summary header. " +
                 "Auto-found by name on Start if left blank.")]
        [SerializeField] private Transform _sunTransform;

        [Header("On-Screen Message")]
        [Tooltip("If true, a world-space TMP message is created on Start.")]
        [SerializeField] private bool _showMessagePanel = true;

        [Tooltip("World-space distance (m) from the camera at which the message panel is placed.")]
        [SerializeField] private float _messageDistance = 4f;

        [Tooltip("Vertical offset (m) added to the message panel position.")]
        [SerializeField] private float _messageHeightOffset = 0.3f;

        [Tooltip("Width x height (m) of the world-space message panel.")]
        [SerializeField] private Vector2 _messagePanelSize = new Vector2(3f, 1.6f);

        [Tooltip("Font size used in the message panel.")]
        [SerializeField] private float _messageFontSize = 0.16f;

        [Tooltip("Optional pre-built TMP font asset. If left blank we Resources.Load the " +
                 "default LiberationSans SDF that ships with TextMesh Pro.")]
        [SerializeField] private TMP_FontAsset _messageFont;

        [Header("Message Strings")]
        [TextArea(2, 5)]
        [SerializeField] private string _msgIdle =
            "Press SPACE to start capturing equal-area sweeps.\n" +
            "(All planets will sweep simultaneously.)";

        [TextArea(2, 5)]
        [SerializeField] private string _msgCapturing =
            "Capturing...\n" +
            "Press SPACE again to stop and freeze the simulation.";

        #endregion

        #region Events ----------------------------------------------------------
        #endregion

        #region State -----------------------------------------------------------

        private LabState   _state = LabState.Idle;
        private Renderer   _sunRenderer;

        // -- Message panel runtime objects.
        private GameObject _messagePanelGo;
        private TextMeshProUGUI _messageText;

        #endregion

        #region Public API ------------------------------------------------------

        /// <summary>True while sweeps are being captured.</summary>
        public bool IsCapturing => _state == LabState.Capturing;

        /// <summary>True after a capture has completed.</summary>
        public bool IsComplete => _state == LabState.Complete;

        /// <summary>How many planets are registered.</summary>
        public int RegisteredPlanetCount => _planets != null ? _planets.Count : 0;

        /// <summary>
        /// Cycles through the lab states: Idle -> Capturing -> Complete -> Idle.
        /// Wire this to a UI button or an InputAction binding for VR.
        /// </summary>
        [ContextMenu("Toggle Capture")]
        public void ToggleCapture()
        {
            switch (_state)
            {
                case LabState.Idle:     StartCapture(); break;
                case LabState.Capturing: StopCapture(); break;
                case LabState.Complete:  ResetLab();     break;
            }
        }

        /// <summary>Starts an open-ended capture on every registered planet.</summary>
        [ContextMenu("Start Capture")]
        public void StartCapture()
        {
            if (_state != LabState.Idle)
            {
                Debug.LogWarning($"{LOG_TAG} StartCapture ignored -- current state is {_state}.");
                return;
            }
            if (_planets == null || _planets.Count == 0)
            {
                Debug.LogWarning($"{LOG_TAG} No planets registered -- StartCapture aborted.", this);
                return;
            }

            int started = 0;
            for (int i = 0; i < _planets.Count; i++)
            {
                var p = _planets[i];
                if (!IsPlanetUsable(p)) continue;
                p._visualizer.BeginSweep(p._sweepColor, p._label);
                started++;
            }

            _state = LabState.Capturing;
            UpdateMessage(_msgCapturing);
            Debug.Log($"{LOG_TAG} Capture started -- {started} simultaneous sweeps.");
        }

        /// <summary>Stops capture, finalises every quesito, freezes the orbiters and shows the area summary.</summary>
        [ContextMenu("Stop Capture")]
        public void StopCapture()
        {
            if (_state != LabState.Capturing)
            {
                Debug.LogWarning($"{LOG_TAG} StopCapture ignored -- current state is {_state}.");
                return;
            }

            for (int i = 0; i < _planets.Count; i++)
            {
                var p = _planets[i];
                if (!IsPlanetUsable(p)) continue;
                p._visualizer.EndSweep();
                if (_pauseOnCaptureStop && p._orbiter != null)
                    p._orbiter.Pause();
            }

            _state = LabState.Complete;
            UpdateMessage(BuildAreaSummary());
            Debug.Log($"{LOG_TAG} Capture stopped -- orbiters paused, areas calculated.");
        }

        /// <summary>Clears all sweeps, resumes every orbiter and returns to the Idle state.</summary>
        [ContextMenu("Reset Lab")]
        public void ResetLab()
        {
            if (_planets != null)
            {
                for (int i = 0; i < _planets.Count; i++)
                {
                    var p = _planets[i];
                    if (p == null) continue;
                    if (p._visualizer != null) p._visualizer.ClearSweeps();
                    if (p._orbiter != null)    p._orbiter.Resume();
                }
            }

            _state = LabState.Idle;
            UpdateMessage(_msgIdle);
            Debug.Log($"{LOG_TAG} Lab reset -- back to Idle.");
        }

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            TryResolveSunReference();
            CacheSunRenderer();
            ResolveMissingVisualizers();
            ValidateReferences();

            if (_faceSunOnStart)
                FacePlayerTowardSun();

            if (_showMessagePanel)
                CreateMessagePanel();

            UpdateMessage(_msgIdle);
        }

        private void Update()
        {
            // New Input System -- the Project Settings have activeInputHandler = 1
            // (Input System Package only), so we don't need an #if branch.
            if (Keyboard.current != null && Keyboard.current[_toggleKey].wasPressedThisFrame)
                ToggleCapture();
        }

        private void OnDestroy()
        {
            if (_messagePanelGo != null)
                Destroy(_messagePanelGo);
        }

        #endregion

        #region Internals -- Sun + Player ---------------------------------------

        private void TryResolveSunReference()
        {
            if (_sunTransform != null) return;

            Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name.ToLowerInvariant();
                if (n == "sun" || n == "sol" || n.Contains("sun") || n.Contains("sol"))
                {
                    _sunTransform = all[i];
                    Debug.Log($"{LOG_TAG} Auto-assigned _sunTransform: {_sunTransform.name}.");
                    return;
                }
            }
        }

        private void CacheSunRenderer()
        {
            _sunRenderer = _sunTransform != null ? _sunTransform.GetComponentInChildren<Renderer>() : null;
        }

        private Vector3 GetSunWorldPos()
        {
            if (_sunTransform == null) return Vector3.zero;
            if (_sunRenderer == null) CacheSunRenderer();
            return _sunRenderer != null ? _sunRenderer.bounds.center : _sunTransform.position;
        }

        private void FacePlayerTowardSun()
        {
            if (Camera.main == null)
            {
                Debug.LogWarning($"{LOG_TAG} No main camera -- cannot face the player toward the Sun.", this);
                return;
            }
            if (_sunTransform == null)
            {
                Debug.LogWarning($"{LOG_TAG} No Sun reference -- skipping face-sun setup.", this);
                return;
            }

            // Walk up the camera's hierarchy to find the rig root.
            Transform rig = Camera.main.transform.root;

            // Project both vectors onto the horizontal plane so we don't tilt the rig.
            Vector3 toSun = GetSunWorldPos() - Camera.main.transform.position;
            toSun.y = 0f;
            Vector3 camFwd = Camera.main.transform.forward;
            camFwd.y = 0f;
            if (toSun.sqrMagnitude < MIN_FALLOFF || camFwd.sqrMagnitude < MIN_FALLOFF) return;

            float currentAngle = Mathf.Atan2(camFwd.x, camFwd.z) * Mathf.Rad2Deg;
            float targetAngle  = Mathf.Atan2(toSun.x, toSun.z) * Mathf.Rad2Deg;
            float deltaAngle   = Mathf.DeltaAngle(currentAngle, targetAngle);

            rig.rotation = rig.rotation * Quaternion.Euler(0f, deltaAngle, 0f);
            Debug.Log($"{LOG_TAG} Rig rotated by {deltaAngle:F1}° to face the Sun.");
        }

        #endregion

        #region Internals -- Message Panel --------------------------------------

        private void CreateMessagePanel()
        {
            if (Camera.main == null)
            {
                Debug.LogWarning($"{LOG_TAG} No main camera -- cannot place the message panel.", this);
                return;
            }

            // Pick a font: explicit > resources lookup > TMP_Settings default.
            TMP_FontAsset font = _messageFont;
            if (font == null)
                font = Resources.Load<TMP_FontAsset>(TMP_FONT_RESOURCE_PATH);
            if (font == null && TMP_Settings.instance != null)
                font = TMP_Settings.defaultFontAsset;

            if (font == null)
            {
                Debug.LogWarning($"{LOG_TAG} No TMP font available -- message panel skipped.", this);
                return;
            }

            // Place the panel a few meters in front of the camera at eye level.
            Camera cam = Camera.main;
            Vector3 panelPos = cam.transform.position
                             + cam.transform.forward * _messageDistance
                             + Vector3.up * _messageHeightOffset;
            Quaternion panelRot = Quaternion.LookRotation(panelPos - cam.transform.position, Vector3.up);

            _messagePanelGo = new GameObject("HUD_KeplerLab2_Message");
            _messagePanelGo.transform.position = panelPos;
            _messagePanelGo.transform.rotation = panelRot;

            var canvas = _messagePanelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            // World-space canvases need a sensible RectTransform size and a small
            // localScale so we can specify font size in metres rather than pixels.
            var canvasRect = _messagePanelGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(_messagePanelSize.x * 100f, _messagePanelSize.y * 100f);
            canvasRect.localScale = Vector3.one * 0.01f;

            // Background dimmer so text is readable against the starfield.
            var bgGo = new GameObject("Img_PanelBackground");
            bgGo.transform.SetParent(_messagePanelGo.transform, worldPositionStays: false);
            var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Text itself.
            var textGo = new GameObject("Txt_PanelMessage");
            textGo.transform.SetParent(_messagePanelGo.transform, worldPositionStays: false);
            _messageText = textGo.AddComponent<TextMeshProUGUI>();
            _messageText.font = font;
            _messageText.fontSize = _messageFontSize * 100f;
            _messageText.alignment = TextAlignmentOptions.Center;
            _messageText.color = Color.white;
            _messageText.enableWordWrapping = true;
            _messageText.text = string.Empty;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 20);
            textRect.offsetMax = new Vector2(-20, -20);
        }

        private void UpdateMessage(string message)
        {
            if (_messageText != null)
                _messageText.text = message;
        }

        #endregion

        #region Internals -- Area Summary ---------------------------------------

        private string BuildAreaSummary()
        {
            // Group sweeps by their _orbitGroup so we can show the equal-area
            // result clearly: same group -> same dt -> equal areas (Kepler 2).
            var byGroup = new Dictionary<string, List<(string label, float area)>>();
            for (int i = 0; i < _planets.Count; i++)
            {
                var p = _planets[i];
                if (!IsPlanetUsable(p) || !p._visualizer.HasSweep) continue;

                string group = string.IsNullOrEmpty(p._orbitGroup) ? "Default" : p._orbitGroup;
                if (!byGroup.TryGetValue(group, out var list))
                {
                    list = new List<(string, float)>();
                    byGroup[group] = list;
                }
                list.Add((p._label, p._visualizer.LastSweptArea));
            }

            var sb = new StringBuilder();
            sb.AppendLine("<size=120%><b>Equal-area sweeps</b></size>");
            sb.AppendLine("<size=80%>(Kepler's 2nd Law: same dt -> same dA)</size>");
            sb.AppendLine();

            foreach (var kvp in byGroup)
            {
                float min = float.PositiveInfinity, max = float.NegativeInfinity, sum = 0f;
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    float a = kvp.Value[i].area;
                    if (a < min) min = a;
                    if (a > max) max = a;
                    sum += a;
                }
                float avg = sum / Mathf.Max(kvp.Value.Count, 1);
                float spread = max > 0f ? (max - min) / max * 100f : 0f;

                sb.Append("<b>").Append(kvp.Key).Append(" orbit:</b>  ");
                sb.Append("avg = ").Append(avg.ToString("F2"));
                sb.Append("  spread ").Append(spread.ToString("F1")).Append("%\n");
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    sb.Append("  · ").Append(kvp.Value[i].label).Append(": ");
                    sb.Append(kvp.Value[i].area.ToString("F2")).Append("\n");
                }
                sb.AppendLine();
            }

            sb.Append("<size=80%>Press SPACE to reset and capture again.</size>");
            return sb.ToString();
        }

        #endregion

        #region Internals -- Lookup helpers -------------------------------------

        private static bool IsPlanetUsable(LabPlanet p)
        {
            if (p == null) return false;
            if (p._orbiter == null) return false;
            if (p._visualizer == null) return false;
            return true;
        }

        private void ResolveMissingVisualizers()
        {
            if (_planets == null) return;
            for (int i = 0; i < _planets.Count; i++)
            {
                var p = _planets[i];
                if (p == null) continue;
                if (p._visualizer != null) continue;
                if (p._orbiter == null) continue;
                p._visualizer = p._orbiter.GetComponent<KeplerAreaVisualizer>();
                if (p._visualizer != null)
                    Debug.Log($"{LOG_TAG} Resolved visualizer for '{p._label}' from orbiter GameObject.");
            }
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_planets == null || _planets.Count == 0)
            {
                Debug.LogWarning($"{LOG_TAG} _planets list is empty -- lab will be a no-op.", this);
                return;
            }

            for (int i = 0; i < _planets.Count; i++)
            {
                var p = _planets[i];
                if (p == null)
                {
                    Debug.LogWarning($"{LOG_TAG} _planets[{i}] is null.", this);
                    continue;
                }
                if (p._orbiter == null)
                    Debug.LogWarning($"{LOG_TAG} _planets[{i}].'{p._label}' has no orbiter assigned.", this);
                if (p._visualizer == null)
                    Debug.LogWarning($"{LOG_TAG} _planets[{i}].'{p._label}' has no visualizer assigned.", this);
            }
        }

        #endregion
    }
}
