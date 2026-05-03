using System.Collections;
using _Project.Scripts.Planets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Shows a world-space VR title when a planet scene is loaded.
    /// Attach to the SceneManager GameObject in each planet scene alongside PlanetSceneSetup.
    /// Assign the same PlanetConfig used by PlanetSceneSetup; the canvas is built at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Gravity HUD Display")]
    public class GravityHUDDisplay : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG         = "[GravityHUDDisplay]";
        private const float  UNITS_PER_METRE = 1000f; // canvas internal units: 1 unit = 1 mm
        private const int    CANVAS_SORT_ORDER = 20;

        #endregion

        #region Inspector

        [Header("Data Source")]
        [Tooltip("Planet config asset — same one used by PlanetSceneSetup in this scene. " +
                 "Leave empty to auto-detect from PlanetSceneSetup or fall back to Physics.gravity.")]
        [SerializeField] private PlanetConfig _config;

        [Header("HUD Layout")]
        [Tooltip("Distance in front of the camera (metres) where the panel floats.")]
        [SerializeField] private float _distanceFromCamera = 2f;

        [Tooltip("Vertical offset from the camera centre (metres). Positive = above eye level.")]
        [SerializeField] private float _verticalOffset = 0.2f;

        [Tooltip("Physical width of the panel in metres.")]
        [SerializeField] private float _panelWidth = 0.7f;

        [Tooltip("Physical height of the panel in metres.")]
        [SerializeField] private float _panelHeight = 0.22f;

        [Header("Timing")]
        [Tooltip("Seconds the HUD takes to fade in.")]
        [SerializeField] private float _fadeInDuration = 0.8f;

        [Tooltip("Seconds the HUD stays fully visible. Set 0 to keep it on screen forever.")]
        [SerializeField] private float _holdDuration = 5f;

        [Tooltip("Seconds the HUD takes to fade out. Ignored when Hold Duration is 0.")]
        [SerializeField] private float _fadeOutDuration = 1.2f;

        [Header("Colours")]
        [Tooltip("Background panel colour including alpha.")]
        [SerializeField] private Color _backgroundColour = new Color(0f, 0f, 0f, 0.65f);

        [Tooltip("Colour of the planet name text.")]
        [SerializeField] private Color _planetNameColour = Color.white;

        [Tooltip("Colour of the gravity value text.")]
        [SerializeField] private Color _gravityColour = new Color(1f, 0.92f, 0.35f, 1f);

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private CanvasGroup _canvasGroup;
        private WaitForSeconds _holdWait;

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            if (_holdDuration > 0f)
                _holdWait = new WaitForSeconds(_holdDuration);

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
            {
                Debug.LogWarning($"{LOG_TAG} No Main Camera found -- HUD not shown.", this);
                yield break;
            }

            BuildHUD(cam);
            StartCoroutine(AnimateHUD());
        }

        private void BuildHUD(Camera cam)
        {
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

            TextMeshProUGUI nameLabel = CreateTMP(hudRoot.transform, "TxtPlanetName");
            RectTransform nameRT = nameLabel.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0f, 0.48f);
            nameRT.anchorMax = new Vector2(1f, 1f);
            nameRT.offsetMin = new Vector2(20f,  8f);
            nameRT.offsetMax = new Vector2(-20f, -8f);
            nameLabel.fontSize  = 90f;
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.alignment = TextAlignmentOptions.Center;
            nameLabel.color     = _planetNameColour;

            TextMeshProUGUI gravLabel = CreateTMP(hudRoot.transform, "TxtGravity");
            RectTransform gravRT = gravLabel.GetComponent<RectTransform>();
            gravRT.anchorMin = new Vector2(0f, 0f);
            gravRT.anchorMax = new Vector2(1f, 0.52f);
            gravRT.offsetMin = new Vector2(20f,  6f);
            gravRT.offsetMax = new Vector2(-20f, -6f);
            gravLabel.fontSize  = 65f;
            gravLabel.alignment = TextAlignmentOptions.Center;
            gravLabel.color     = _gravityColour;

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
                planetName = "Planet";
                gravityY   = Physics.gravity.y;
            }

            nameLabel.text = planetName;
            gravLabel.text = $"Gravity: {Mathf.Abs(gravityY):F2} m/s²";

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
            for (float t = 0f; t < _fadeInDuration; t += Time.deltaTime)
            {
                _canvasGroup.alpha = t / _fadeInDuration;
                yield return null;
            }
            _canvasGroup.alpha = 1f;

            if (_holdDuration <= 0f) yield break;

            yield return _holdWait;

            for (float t = 0f; t < _fadeOutDuration; t += Time.deltaTime)
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
                Debug.LogWarning($"{LOG_TAG} _config is not assigned -- will try auto-detect or fall back to Physics.gravity.", this);
        }

        #endregion
    }
}
