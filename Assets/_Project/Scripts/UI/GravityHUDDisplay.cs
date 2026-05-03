using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows a world-space VR title when a planet scene is loaded from the Gravity Lab.
/// The panel floats in front of the player's camera and displays the planet name
/// and its surface gravity, then fades out after a configurable hold time.
///
/// SETUP
/// ─────
/// 1. Add this component to the SceneManager GameObject in each planet scene
///    (same object that already has LocalGravityModifier).
/// 2. Assign the same PlanetConfig asset used by LocalGravityModifier.
/// 3. (Optional) Tweak the timing and layout fields in the Inspector.
///
/// The script creates its own Canvas at runtime — no prefab needed.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("ProyectoVR/UI/Gravity HUD Display")]
public class GravityHUDDisplay : MonoBehaviour
{
    #region Inspector

    [Header("Data Source")]
    [Tooltip("Planet config asset — same one used by LocalGravityModifier in this scene. " +
             "Leave empty to auto-detect from LocalGravityModifier or fall back to Physics.gravity.")]
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
    [SerializeField] private Color _backgroundColour = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color _planetNameColour = Color.white;
    [SerializeField] private Color _gravityColour    = new Color(1f, 0.92f, 0.35f, 1f); // warm yellow

    #endregion

    #region Private State

    private CanvasGroup _canvasGroup;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Wait one frame so the XR Camera is fully initialised and
        // LocalGravityModifier has already applied gravity in Awake().
        StartCoroutine(InitNextFrame());
    }

    #endregion

    #region Initialisation

    private IEnumerator InitNextFrame()
    {
        yield return null;

        // ── Resolve config ───────────────────────────────────────────────
        if (_config == null)
        {
            PlanetSceneSetup setup = FindFirstObjectByType<PlanetSceneSetup>();
            if (setup != null) _config = setup.Config;
        }

        // ── Resolve camera ───────────────────────────────────────────────
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("[GravityHUDDisplay] No Main Camera found — HUD not shown.", this);
            yield break;
        }

        // ── Build and animate the HUD ────────────────────────────────────
        BuildHUD(cam);
        StartCoroutine(AnimateHUD());
    }

    private void BuildHUD(Camera cam)
    {
        // ── Root & Canvas ────────────────────────────────────────────────
        // Internal units: 1 unit = 1 mm (scale = 0.001)
        // So panel size in units = metres × 1000
        float unitsPerMetre = 1000f;
        float canvasW = _panelWidth  * unitsPerMetre;
        float canvasH = _panelHeight * unitsPerMetre;

        GameObject hudRoot = new GameObject("GravityHUD_Canvas");
        hudRoot.transform.SetParent(cam.transform, false);
        hudRoot.transform.localPosition = new Vector3(0f, _verticalOffset, _distanceFromCamera);
        hudRoot.transform.localRotation = Quaternion.identity;
        hudRoot.transform.localScale    = Vector3.one * 0.001f;

        Canvas canvas = hudRoot.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        _canvasGroup                  = hudRoot.AddComponent<CanvasGroup>();
        _canvasGroup.alpha            = 0f;
        _canvasGroup.interactable     = false;
        _canvasGroup.blocksRaycasts   = false;

        RectTransform canvasRT = hudRoot.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(canvasW, canvasH);

        // ── Background panel ─────────────────────────────────────────────
        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(hudRoot.transform, false);

        Image bg = bgGO.AddComponent<Image>();
        bg.color = _backgroundColour;

        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // ── Planet name label ────────────────────────────────────────────
        TextMeshProUGUI nameLabel = CreateTMP(hudRoot.transform, "TxtPlanetName");
        RectTransform nameRT = nameLabel.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0f, 0.48f);
        nameRT.anchorMax = new Vector2(1f, 1f);
        nameRT.offsetMin = new Vector2(20f,  8f);
        nameRT.offsetMax = new Vector2(-20f, -8f);

        nameLabel.fontSize   = 90f;
        nameLabel.fontStyle  = FontStyles.Bold;
        nameLabel.alignment  = TextAlignmentOptions.Center;
        nameLabel.color      = _planetNameColour;

        // ── Gravity label ────────────────────────────────────────────────
        TextMeshProUGUI gravLabel = CreateTMP(hudRoot.transform, "TxtGravity");
        RectTransform gravRT = gravLabel.GetComponent<RectTransform>();
        gravRT.anchorMin = new Vector2(0f, 0f);
        gravRT.anchorMax = new Vector2(1f, 0.52f);
        gravRT.offsetMin = new Vector2(20f,  6f);
        gravRT.offsetMax = new Vector2(-20f, -6f);

        gravLabel.fontSize  = 65f;
        gravLabel.alignment = TextAlignmentOptions.Center;
        gravLabel.color     = _gravityColour;

        // ── Populate text ────────────────────────────────────────────────
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
            planetName = "Planeta";
            gravityY   = Physics.gravity.y;
        }

        nameLabel.text = planetName;
        gravLabel.text = $"Gravedad: {Mathf.Abs(gravityY):F2} m/s²";

        Debug.Log($"[GravityHUDDisplay] HUD built — {planetName} | {gravityY:F2} m/s²");
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string goName)
    {
        GameObject go = new GameObject(goName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.AddComponent<TextMeshProUGUI>();
    }

    #endregion

    #region Animation

    private IEnumerator AnimateHUD()
    {
        // Fade in
        for (float t = 0f; t < _fadeInDuration; t += Time.deltaTime)
        {
            _canvasGroup.alpha = t / _fadeInDuration;
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        if (_holdDuration <= 0f) yield break; // stay on screen permanently

        // Hold
        yield return new WaitForSeconds(_holdDuration);

        // Fade out
        for (float t = 0f; t < _fadeOutDuration; t += Time.deltaTime)
        {
            _canvasGroup.alpha = 1f - t / _fadeOutDuration;
            yield return null;
        }
        _canvasGroup.alpha = 0f;
        _canvasGroup.gameObject.SetActive(false);
    }

    #endregion
}
