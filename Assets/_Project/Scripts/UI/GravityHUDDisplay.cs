using System.Collections;
using _Project.Scripts.Planets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Shows a world-space VR panel when a planet scene loads: planet name, surface gravity,
    /// and a short instruction line. Creates its own Canvas at runtime — no prefab needed.
    /// Attach to the SceneManager GameObject in each planet scene.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Gravity HUD Display")]
    public class GravityHUDDisplay : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG          = "[GravityHUDDisplay]";
        private const float  UNITS_PER_METRE  = 1000f;
        private const int    CANVAS_SORT_ORDER = 20;

        #endregion

        #region Inspector

        [Header("Data Source")]
        [Tooltip("Planet config asset — same one used by PlanetSceneSetup in this scene. " +
                 "Leave empty to auto-detect or fall back to scene name + Physics.gravity.")]
        [SerializeField] private PlanetConfig _config;

        [Header("Instruction Text")]
        [Tooltip("Short hint shown below the gravity value. Leave empty to hide the hint row.")]
        [SerializeField] private string _instructionText = "Recoge una muestra del planeta y juega con ella. ¡Cuidado no te caigas!";

        [Header("HUD Layout")]
        [Tooltip("Distance in front of the camera (metres) where the panel floats.")]
        [SerializeField] private float _distanceFromCamera = 2f;

        [Tooltip("Vertical offset from the camera centre (metres). Positive = above eye level.")]
        [SerializeField] private float _verticalOffset = 0.35f;

        private readonly float _panelWidth  = 1.20f;
        private readonly float _panelHeight = 0.58f;

        [Header("Timing")]
        [Tooltip("Seconds the HUD takes to fade in.")]
        [SerializeField] private float _fadeInDuration = 0.8f;

        [Tooltip("Seconds the HUD stays fully visible. Set 0 to keep it on screen forever.")]
        [SerializeField] private float _holdDuration = 6f;

        [Tooltip("Seconds the HUD takes to fade out. Ignored when Hold Duration is 0.")]
        [SerializeField] private float _fadeOutDuration = 1.2f;

        [Header("Colours")]
        [Tooltip("Background panel colour including alpha.")]
        [SerializeField] private Color _backgroundColour = new Color(0f, 0f, 0f, 0.70f);

        [Tooltip("Colour of the planet name text.")]
        [SerializeField] private Color _planetNameColour = Color.white;

        [Tooltip("Colour of the gravity value text.")]
        [SerializeField] private Color _gravityColour = new Color(1f, 0.92f, 0.35f, 1f);

        [Tooltip("Colour of the instruction hint text.")]
        [SerializeField] private Color _instructionColour = new Color(0.75f, 0.90f, 1f, 1f);

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private CanvasGroup _canvasGroup;

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            StartCoroutine(InitNextFrame());
        }

        #endregion

        #region Internals

        private IEnumerator InitNextFrame()
        {
            yield return null;

            if (_config == null)
            {
                PlanetSceneSetup setup = FindFirstObjectByType<PlanetSceneSetup>();
                if (setup != null) _config = setup.Config;
            }

            Camera cam = Camera.main;
            if (cam == null)
                cam = FindFirstObjectByType<Camera>();

            if (cam == null)
            {
                Debug.LogWarning($"{LOG_TAG} No camera found -- HUD not shown.", this);
                yield break;
            }

            BuildHUD(cam);
            StartCoroutine(AnimateHUD());
        }

        private void BuildHUD(Camera cam)
        {
            bool hasInstructions = !string.IsNullOrWhiteSpace(_instructionText);
            float canvasW = _panelWidth  * UNITS_PER_METRE;
            float canvasH = _panelHeight * UNITS_PER_METRE;

            GameObject hudRoot = new GameObject("GravityHUD_Canvas");
            hudRoot.transform.SetParent(cam.transform, false);
            hudRoot.transform.localPosition = new Vector3(0f, _verticalOffset, _distanceFromCamera);
            hudRoot.transform.localRotation = Quaternion.identity;
            hudRoot.transform.localScale    = Vector3.one * 0.001f;

            Canvas canvas = hudRoot.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.WorldSpace;
            canvas.sortingOrder = CANVAS_SORT_ORDER;

            _canvasGroup                = hudRoot.AddComponent<CanvasGroup>();
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            RectTransform canvasRT = hudRoot.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(canvasW, canvasH);

            GameObject bgGO = new GameObject("Background", typeof(RectTransform));
            bgGO.transform.SetParent(hudRoot.transform, false);
            Image bg = bgGO.AddComponent<Image>();
            bg.color = _backgroundColour;
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            // Layout (with instructions): name top 32%, gravity middle 26%, large gap, hint bottom 20%
            float nameBottom = hasInstructions ? 0.68f : 0.45f;
            float gravTop    = hasInstructions ? 0.66f : 0.55f;
            float gravBottom = hasInstructions ? 0.42f : 0f;
            float hintTop    = 0.22f;

            TextMeshProUGUI nameLabel = CreateTMP(hudRoot.transform, "TxtPlanetName");
            RectTransform nameRT = nameLabel.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, nameBottom);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.offsetMin = new Vector2(20f, 8f);
            nameRT.offsetMax = new Vector2(-20f, -8f);
            nameLabel.fontSize  = 100f;
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.color     = _planetNameColour;

            TextMeshProUGUI gravLabel = CreateTMP(hudRoot.transform, "TxtGravity");
            RectTransform gravRT = gravLabel.GetComponent<RectTransform>();
            gravRT.anchorMin = new Vector2(0f, gravBottom);
            gravRT.anchorMax = new Vector2(1f, gravTop);
            gravRT.offsetMin = new Vector2(20f, 6f);
            gravRT.offsetMax = new Vector2(-20f, -6f);
            gravLabel.fontSize  = 68f;
            gravLabel.alignment = TextAlignmentOptions.Center;
            gravLabel.color     = _gravityColour;

            if (hasInstructions)
            {
                TextMeshProUGUI hintLabel = CreateTMP(hudRoot.transform, "TxtInstructions");
                RectTransform hintRT = hintLabel.GetComponent<RectTransform>();
                hintRT.anchorMin = new Vector2(0f, 0f);
                hintRT.anchorMax = new Vector2(1f, hintTop);
                hintRT.offsetMin = new Vector2(20f, 10f);
                hintRT.offsetMax = new Vector2(-20f, -10f);
                hintLabel.fontSize           = 44f;
                hintLabel.alignment          = TextAlignmentOptions.Center;
                hintLabel.color              = _instructionColour;
                hintLabel.fontStyle          = FontStyles.Italic;
                hintLabel.textWrappingMode   = TextWrappingModes.Normal;
                hintLabel.text               = _instructionText;
            }

            string planetName;
            float  gravityY;

            if (_config != null)
            {
                planetName = !string.IsNullOrWhiteSpace(_config._displayNameEs)
                    ? _config._displayNameEs
                    : _config._displayNameEn;
                gravityY = _config._gravityY;
            }
            else
            {
                planetName = SceneManager.GetActiveScene().name;
                gravityY   = Physics.gravity.y;
            }

            nameLabel.text = planetName;
            gravLabel.text = $"Gravedad: {Mathf.Abs(gravityY):F2} m/s²";

            Debug.Log($"{LOG_TAG} HUD built -- {planetName} | {gravityY:F2} m/s².");
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string goName)
        {
            GameObject go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private IEnumerator AnimateHUD()
        {
            for (float t = 0f; t < _fadeInDuration; t += Time.unscaledDeltaTime)
            {
                _canvasGroup.alpha = t / _fadeInDuration;
                yield return null;
            }
            _canvasGroup.alpha = 1f;

            if (_holdDuration <= 0f) yield break;

            float held = 0f;
            while (held < _holdDuration)
            {
                held += Time.unscaledDeltaTime;
                yield return null;
            }

            for (float t = 0f; t < _fadeOutDuration; t += Time.unscaledDeltaTime)
            {
                _canvasGroup.alpha = 1f - t / _fadeOutDuration;
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            _canvasGroup.gameObject.SetActive(false);
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_config == null)
                Debug.LogWarning($"{LOG_TAG} _config is not assigned -- will auto-detect or use scene name as fallback.", this);
        }

        #endregion
    }
}
