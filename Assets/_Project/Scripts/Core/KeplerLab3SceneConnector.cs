using System;
using System.Collections;
using _Project.Scripts.Planets;
using _Project.Scripts.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Conector especifico para KeplerLab 3.
    /// Igual que KeplerSceneConnector pero activa OrbitalDataCard al soltar el planeta,
    /// y muestra un panel de introduccion y un panel de explicacion de la 3a Ley de Kepler.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Kepler Lab3 Scene Connector")]
    public sealed class KeplerLab3SceneConnector : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[KeplerLab3SceneConnector]";
        private const float MIN_SPAWN_DISTANCE = 0.1f;
        private const string TMP_FONT_RESOURCE_PATH = "Fonts & Materials/LiberationSans SDF";

        #endregion

        #region Inspector

        [Header("Dependencies")]
        [Tooltip("WristMenuController on the XR Rig.")]
        [SerializeField] private WristMenuControllerKepler _wristMenuController;

        [Tooltip("Orbital pause controller in the scene.")]
        [SerializeField] private OrbitalPauseController _orbitalPauseController;

        [Tooltip("Orbit line visibility controller in the scene.")]
        [SerializeField] private OrbitVisibilityController _orbitVisibilityController;

        [Header("Scene Settings")]
        [Tooltip("Exact name of the main menu scene.")]
        [SerializeField] private string _mainMenuSceneName = "Main_VR";

        [Tooltip("Index fijo para KeplerLab 3.")]
        [SerializeField] private int _currentSceneIndex = 2;

        [Header("Spawn Settings")]
        [Tooltip("Prefab del planeta a instanciar.")]
        [SerializeField] private GameObject _planetPrefab;

        [Tooltip("Transform de la camara del jugador.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Distancia frente al jugador donde aparece el planeta.")]
        [SerializeField] private float _spawnDistance = 1.5f;

        [Tooltip("Escala del planeta al spawnear.")]
        [SerializeField] private float _spawnScale = 0.5f;

        [Header("Lab 3 -- Data Card")]
        [Tooltip("OrbitalDataCard que muestra la 3a ley de Kepler al soltar el planeta.")]
        [SerializeField] private OrbitalDataCard _dataCard;

        // -----------------------------------------------------------------------
        [Header("Intro Panel")]
        [Tooltip("Si esta activado, al entrar en la escena aparece un panel con instrucciones de uso.")]
        [SerializeField] private bool _showIntroPanel = true;

        [Tooltip("Segundos que tarda el panel de intro en aparecer (fade in).")]
        [SerializeField] private float _introFadeInDuration = 1.0f;

        [Tooltip("Segundos que el panel de intro permanece completamente visible.")]
        [SerializeField] private float _introHoldDuration = 4.0f;

        [Tooltip("Segundos que tarda el panel de intro en desaparecer (fade out).")]
        [SerializeField] private float _introFadeOutDuration = 1.5f;

        [TextArea(4, 12)]
        [Tooltip("Texto que aparece al iniciar la escena explicando como usar el lab.")]
        [SerializeField] private string _introText =
            "<size=120%><b>KeplerLab 3</b></size>\n" +
            "<size=80%>(Tercera Ley de Kepler)</size>\n\n" +
            "1. Pulsa <b>Spawn Planet</b> para crear un planeta.\n" +
            "2. <b>Agarralo</b> y colócalo en cualquier punto del espacio.\n" +
            "3. Al soltarlo, orbita alrededor del Sol y aparece una <b>tarjeta de datos</b> " +
            "con su <b>semieje mayor (a)</b> y su <b>periodo orbital (T)</b>.\n" +
            "4. Comprueba que <b>T² / a³</b> es la misma constante para todos los planetas.\n\n" +
            "<size=80%>Pausa la simulación para leer la explicación de la ley.</size>";

        // -----------------------------------------------------------------------
        [Header("Explanation Panel (Pause)")]
        [Tooltip("Si esta activado, al pausar aparece un panel con la explicacion de la 3a Ley.")]
        [SerializeField] private bool _showExplanationOnPause = true;

        [Tooltip("If enabled, the pause explanation card appears above the Sun instead of on the wrist.")]
        [SerializeField] private bool _placePausePanelAboveSun = true;

        [Tooltip("World-space offset from Sun center for the pause explanation card.")]
        [SerializeField] private Vector3 _pausePanelSunOffset = new Vector3(0f, 2.2f, 0f);

        [Tooltip("Transform del mando derecho. Si esta vacio se auto-resuelve buscando 'Right Controller'.")]
        [SerializeField] private Transform _rightControllerAnchor;

        [Tooltip("Offset local (m) respecto al mando derecho donde se ancla el panel.")]
        [SerializeField] private Vector3 _panelLocalOffset = new Vector3(0f, 0.18f, 0.05f);

        [Tooltip("Rotacion local (grados) respecto al mando derecho.")]
        [SerializeField] private Vector3 _panelLocalEuler = new Vector3(45f, 0f, 0f);

        [Tooltip("Escala local del panel cuando se ancla al mando.")]
        [SerializeField] private float _panelControllerScale = 0.08f;

        [Tooltip("Distancia (m) desde la camara cuando no se encuentra el mando (fallback).")]
        [SerializeField] private float _messageDistance = 4f;

        [Tooltip("Offset vertical (m) del fallback de panel frente a camara.")]
        [SerializeField] private float _messageHeightOffset = 0.3f;

        [Tooltip("Tamaño (m) del panel: ancho x alto.")]
        [SerializeField] private Vector2 _messagePanelSize = new Vector2(3f, 1.6f);

        [Tooltip("Tamaño (m) de fuente del panel.")]
        [SerializeField] private float _messageFontSize = 0.16f;

        [Tooltip("Asset de fuente TMP. Si esta vacio se carga LiberationSans SDF de Resources.")]
        [SerializeField] private TMP_FontAsset _messageFont;

        [TextArea(4, 12)]
        [Tooltip("Texto que aparece en el panel cuando se pausa la simulacion.")]
        [SerializeField] private string _explanationText =
            "<size=120%><b>Tercera Ley de Kepler</b></size>\n" +
            "<size=80%>(Ley de los periodos)</size>\n\n" +
            "El cuadrado del <b>periodo orbital (T²)</b> es proporcional al cubo del " +
            "<b>semieje mayor (a³)</b> de la órbita:\n\n" +
            "          <b>T² / a³ = constante</b>\n\n" +
            "● <b>T</b>: tiempo que tarda el planeta en dar una vuelta completa.\n" +
            "● <b>a</b>: distancia media al Sol (semieje mayor de la elipse).\n" +
            "● La constante vale <b>4π² / (G·M☉)</b> y es igual para todos los planetas.\n\n" +
            "<size=80%>Pulsa play/pausa de nuevo para reanudar.</size>";

        #endregion

        #region Constants — Kepler Scenes

        private static readonly string[] KEPLER_SCENES =
        {
            "KeplerLab 1",
            "KeplerLab 2",
            "KeplerLab 3"
        };

        #endregion

        #region State

        private GameObject _spawnedPlanet;

        // Intro panel
        private GameObject    _introPanelGo;
        private TextMeshProUGUI _introLabel;
        private CanvasGroup   _introCanvasGroup;
        private Coroutine     _introFadeCoroutine;

        // Explanation panel
        private GameObject      _explanationPanelGo;
        private TextMeshProUGUI _explanationLabel;
        private Transform       _sunTransform;
        private Renderer        _sunRenderer;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            _spawnDistance = Mathf.Max(MIN_SPAWN_DISTANCE, _spawnDistance);
            TryResolveRightControllerAnchor();
            ValidateReferences();
            SubscribeEvents();

            if (_wristMenuController != null)
                _wristMenuController.SetSpawnButtonInteractable(true);

            if (_showIntroPanel)
                ShowIntroPanel();

            Debug.Log($"{LOG_TAG} Initialized -- KeplerLab3, events subscribed.");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            UnsubscribePlanetEvents();

            if (_introPanelGo != null)
                Destroy(_introPanelGo);
            if (_explanationPanelGo != null)
                Destroy(_explanationPanelGo);
        }

        #endregion

        #region Internals — Events

        private void SubscribeEvents()
        {
            if (_wristMenuController == null) return;
            _wristMenuController.OnBackPressed         += HandleBackPressed;
            _wristMenuController.OnPausePressed        += HandlePausePressed;
            _wristMenuController.OnToggleOrbitsPressed += HandleToggleOrbitsPressed;
            _wristMenuController.OnSpawnPlanetPressed  += HandleSpawnPlanetPressed;
            _wristMenuController.OnNextLawPressed      += HandleNextLawPressed;
        }

        private void UnsubscribeEvents()
        {
            if (_wristMenuController == null) return;
            _wristMenuController.OnBackPressed         -= HandleBackPressed;
            _wristMenuController.OnPausePressed        -= HandlePausePressed;
            _wristMenuController.OnToggleOrbitsPressed -= HandleToggleOrbitsPressed;
            _wristMenuController.OnSpawnPlanetPressed  -= HandleSpawnPlanetPressed;
            _wristMenuController.OnNextLawPressed      -= HandleNextLawPressed;
        }

        private void SubscribePlanetEvents()
        {
            if (_spawnedPlanet == null) return;

            OrbitalLauncher launcher = _spawnedPlanet.GetComponentInChildren<OrbitalLauncher>();
            if (launcher == null)
            {
                Debug.LogWarning($"{LOG_TAG} OrbitalLauncher not found on spawned planet.", this);
                return;
            }

            launcher.OnOrbitLaunched += HandleOrbitLaunched;
            Debug.Log($"{LOG_TAG} Subscribed to OrbitalLauncher.OnOrbitLaunched.");
        }

        private void UnsubscribePlanetEvents()
        {
            if (_spawnedPlanet == null) return;

            OrbitalLauncher launcher = _spawnedPlanet.GetComponentInChildren<OrbitalLauncher>();
            if (launcher != null)
                launcher.OnOrbitLaunched -= HandleOrbitLaunched;
        }

        private void HandleBackPressed()
        {
            if (SceneController.Instance == null) return;
            SceneController.Instance.LoadScene(_mainMenuSceneName, GameState.MainMenu);
        }

        private void HandlePausePressed()
        {
            if (_orbitalPauseController == null) return;
            _orbitalPauseController.TogglePause();
            _wristMenuController.SetPauseIcon(_orbitalPauseController.IsPaused);

            // Mostramos / ocultamos el panel de explicacion de la 3a Ley al pausar,
            // igual que KeplerSceneConnector hace para las leyes 1 y 2.
            if (_showExplanationOnPause)
            {
                if (_orbitalPauseController.IsPaused)
                    ShowExplanationPanel();
                else
                    HideExplanationPanel();
            }

            Debug.Log($"{LOG_TAG} Pause toggled -- paused: {_orbitalPauseController.IsPaused}.");
        }

        private void HandleToggleOrbitsPressed()
        {
            if (_orbitVisibilityController == null) return;
            _orbitVisibilityController.ToggleVisibility();
            _wristMenuController.SetOrbitIcon(_orbitVisibilityController.IsVisible);
        }

        private void HandleSpawnPlanetPressed()
        {
            if (_planetPrefab == null)
            {
                Debug.LogWarning($"{LOG_TAG} _planetPrefab is not assigned.", this);
                return;
            }

            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            Vector3 spawnPos = _cameraTransform.position
                             + _cameraTransform.forward * _spawnDistance;

            UnsubscribePlanetEvents();

            _spawnedPlanet = Instantiate(_planetPrefab, spawnPos, Quaternion.identity);
            _spawnedPlanet.transform.localScale = Vector3.one * _spawnScale;

            SubscribePlanetEvents();

            Debug.Log($"{LOG_TAG} Planet spawned at {spawnPos}.");
        }

        private void HandleNextLawPressed()
        {
            if (SceneController.Instance == null) return;

            int nextIndex = _currentSceneIndex + 1;
            if (nextIndex >= KEPLER_SCENES.Length)
            {
                SceneController.Instance.LoadScene(_mainMenuSceneName, GameState.MainMenu);
                return;
            }

            SceneController.Instance.LoadScene(KEPLER_SCENES[nextIndex], GameState.KeplerLab);
        }

        private void HandleOrbitLaunched(float semiMajorAxis, float orbitalPeriod)
        {
            if (_spawnedPlanet == null) return;

            OrbitalDataCard card = _spawnedPlanet.GetComponentInChildren<OrbitalDataCard>();
            if (card == null)
            {
                Debug.LogWarning($"{LOG_TAG} OrbitalDataCard not found on spawned planet.", this);
                return;
            }

            card.ShowOrbitalData(semiMajorAxis, orbitalPeriod);
            Debug.Log($"{LOG_TAG} DataCard shown -- a={semiMajorAxis:F2} T={orbitalPeriod:F2}.");
        }

        #endregion

        #region Internals — Intro Panel

        private void ShowIntroPanel()
        {
            if (_introPanelGo == null)
                CreateIntroPanel();
            if (_introPanelGo == null) return;

            if (_introLabel != null)
                _introLabel.text = _introText;

            if (_introFadeCoroutine != null)
                StopCoroutine(_introFadeCoroutine);

            _introPanelGo.SetActive(true);
            _introFadeCoroutine = StartCoroutine(IntroPanelFadeSequence());
        }

        private void CreateIntroPanel()
        {
            TMP_FontAsset font = ResolvePanelFont();
            if (font == null)
            {
                Debug.LogWarning($"{LOG_TAG} No TMP font available -- intro panel skipped.", this);
                return;
            }

            _introPanelGo = new GameObject("HUD_KeplerLab3_Intro");

            _introCanvasGroup = _introPanelGo.AddComponent<CanvasGroup>();
            _introCanvasGroup.alpha = 0f;

            if (!TryPlacePanel(_introPanelGo))
            {
                Destroy(_introPanelGo);
                _introPanelGo = null;
                return;
            }

            _introLabel = BuildPanelVisuals(_introPanelGo, font, "Txt_Intro");
            if (_introLabel != null)
                _introLabel.text = _introText;
        }

        /// <summary>Fade in → hold → fade out. Timing driven by inspector fields.</summary>
        private IEnumerator IntroPanelFadeSequence()
        {
            float fadeIn  = Mathf.Max(0f, _introFadeInDuration);
            float hold    = Mathf.Max(0f, _introHoldDuration);
            float fadeOut = Mathf.Max(0f, _introFadeOutDuration);

            // Fade In
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                if (_introCanvasGroup != null)
                    _introCanvasGroup.alpha = Mathf.Clamp01(elapsed / Mathf.Max(fadeIn, 0.001f));
                yield return null;
            }
            if (_introCanvasGroup != null) _introCanvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(hold);

            // Fade Out
            elapsed = 0f;
            while (elapsed < fadeOut)
            {
                elapsed += Time.deltaTime;
                if (_introCanvasGroup != null)
                    _introCanvasGroup.alpha = Mathf.Clamp01(1f - elapsed / Mathf.Max(fadeOut, 0.001f));
                yield return null;
            }
            if (_introCanvasGroup != null) _introCanvasGroup.alpha = 0f;

            if (_introPanelGo != null)
                _introPanelGo.SetActive(false);

            _introFadeCoroutine = null;
            Debug.Log($"{LOG_TAG} Intro panel fade sequence complete.");
        }

        #endregion

        #region Internals — Explanation Panel

        private void TryResolveRightControllerAnchor()
        {
            if (_rightControllerAnchor != null) return;

            Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            Transform exact = null;
            Transform fuzzy = null;
            for (int i = 0; i < all.Length; i++)
            {
                string n     = all[i].name;
                string lower = n.ToLowerInvariant();
                if (string.Equals(n, "Right Controller", StringComparison.Ordinal))
                {
                    exact = all[i];
                    break;
                }
                if (fuzzy == null
                    && (lower.Contains("rightcontroller")
                        || lower.Contains("right controller")
                        || lower.Contains("righthand")
                        || lower.Contains("right hand")))
                {
                    fuzzy = all[i];
                }
            }

            _rightControllerAnchor = exact != null ? exact : fuzzy;
            if (_rightControllerAnchor != null)
                Debug.Log($"{LOG_TAG} Auto-assigned _rightControllerAnchor: {_rightControllerAnchor.name}.");
        }

        private void ShowExplanationPanel()
        {
            if (_explanationPanelGo == null)
                CreateExplanationPanel();
            if (_explanationPanelGo == null) return;

            if (ShouldPlacePausePanelOverSun())
                TryPlacePanelOverSun(_explanationPanelGo);

            _explanationPanelGo.SetActive(true);
            if (_explanationLabel != null)
                _explanationLabel.text = _explanationText;
        }

        private void HideExplanationPanel()
        {
            if (_explanationPanelGo != null)
                _explanationPanelGo.SetActive(false);
        }

        private void CreateExplanationPanel()
        {
            TMP_FontAsset font = ResolvePanelFont();
            if (font == null)
            {
                Debug.LogWarning($"{LOG_TAG} No TMP font available -- explanation panel skipped.", this);
                return;
            }

            _explanationPanelGo = new GameObject("HUD_KeplerLab3_Explanation");
            if (!TryPlacePanel(_explanationPanelGo, isPauseExplanationPanel: true))
            {
                Destroy(_explanationPanelGo);
                _explanationPanelGo = null;
                return;
            }

            _explanationLabel = BuildPanelVisuals(_explanationPanelGo, font, "Txt_Explanation");
            if (_explanationLabel != null)
                _explanationLabel.text = _explanationText;
        }

        #endregion

        #region Internals — Panel Helpers

        private TMP_FontAsset ResolvePanelFont()
        {
            TMP_FontAsset font = _messageFont;
            if (font == null)
                font = Resources.Load<TMP_FontAsset>(TMP_FONT_RESOURCE_PATH);
            if (font == null && TMP_Settings.instance != null)
                font = TMP_Settings.defaultFontAsset;
            return font;
        }

        private bool TryPlacePanel(GameObject panelGo, bool isPauseExplanationPanel = false)
        {
            if (isPauseExplanationPanel && ShouldPlacePausePanelOverSun())
                return TryPlacePanelOverSun(panelGo);

            return TryPlacePanelNearControllerOrCamera(panelGo);
        }

        private bool TryPlacePanelNearControllerOrCamera(GameObject panelGo)
        {
            if (_rightControllerAnchor != null)
            {
                panelGo.transform.SetParent(_rightControllerAnchor, worldPositionStays: false);
                panelGo.transform.localPosition = _panelLocalOffset;
                panelGo.transform.localRotation = Quaternion.Euler(_panelLocalEuler);
                return true;
            }

            if (Camera.main == null)
            {
                Debug.LogWarning($"{LOG_TAG} No main camera and no right controller -- cannot place panel.", this);
                return false;
            }

            Camera cam     = Camera.main;
            Vector3 pos    = cam.transform.position
                           + cam.transform.forward * _messageDistance
                           + Vector3.up * _messageHeightOffset;
            panelGo.transform.position = pos;
            panelGo.transform.rotation =
                Quaternion.LookRotation(pos - cam.transform.position, Vector3.up);
            return true;
        }

        private bool ShouldPlacePausePanelOverSun()
        {
            return _placePausePanelAboveSun;
        }

        private bool TryPlacePanelOverSun(GameObject panelGo)
        {
            if (panelGo == null)
                return false;

            if (!TryResolveSunReference())
            {
                Debug.LogWarning($"{LOG_TAG} Sun reference not found -- cannot place pause panel above Sun.", this);
                return false;
            }

            Vector3 panelPosition = GetSunWorldPosition() + _pausePanelSunOffset;
            panelGo.transform.SetParent(null, worldPositionStays: true);
            panelGo.transform.position = panelPosition;

            if (Camera.main != null)
                panelGo.transform.rotation = Quaternion.LookRotation(panelPosition - Camera.main.transform.position, Vector3.up);
            else
                panelGo.transform.rotation = Quaternion.identity;

            return true;
        }

        private bool TryResolveSunReference()
        {
            if (_sunTransform != null)
                return true;

            Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            Transform fallback = null;
            for (int i = 0; i < all.Length; i++)
            {
                Transform candidate = all[i];
                string lower = candidate.name.ToLowerInvariant();
                bool isLikelySun = lower == "sun"
                    || lower == "sol"
                    || lower.Contains("sun")
                    || lower.Contains("sol");

                if (!isLikelySun)
                    continue;

                if (candidate.GetComponentInChildren<Renderer>() != null)
                {
                    _sunTransform = candidate;
                    CacheSunRenderer();
                    return true;
                }

                if (fallback == null)
                    fallback = candidate;
            }

            _sunTransform = fallback;
            CacheSunRenderer();
            return _sunTransform != null;
        }

        private void CacheSunRenderer()
        {
            _sunRenderer = _sunTransform != null ? _sunTransform.GetComponentInChildren<Renderer>() : null;
        }

        private Vector3 GetSunWorldPosition()
        {
            if (_sunTransform == null)
                return Vector3.zero;

            if (_sunRenderer == null)
                CacheSunRenderer();

            return _sunRenderer != null ? _sunRenderer.bounds.center : _sunTransform.position;
        }

        private TextMeshProUGUI BuildPanelVisuals(GameObject panelGo, TMP_FontAsset font, string textObjectName)
        {
            var canvas = panelGo.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var canvasRect = panelGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(_messagePanelSize.x * 100f, _messagePanelSize.y * 100f);
            bool anchoredToController = _rightControllerAnchor != null
                && panelGo.transform.parent == _rightControllerAnchor;
            float baseScale = anchoredToController ? _panelControllerScale * 0.01f : 0.01f;
            canvasRect.localScale = Vector3.one * baseScale;

            var bgGo = new GameObject("Img_PanelBackground");
            bgGo.transform.SetParent(panelGo.transform, worldPositionStays: false);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var textGo = new GameObject(textObjectName);
            textGo.transform.SetParent(panelGo.transform, worldPositionStays: false);
            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.font             = font;
            label.fontSize         = _messageFontSize * 100f;
            label.alignment        = TextAlignmentOptions.Center;
            label.color            = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 20);
            textRect.offsetMax = new Vector2(-20, -20);

            return label;
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_wristMenuController == null)
                Debug.LogWarning($"{LOG_TAG} _wristMenuController is not assigned.", this);
            if (_orbitalPauseController == null)
                Debug.LogWarning($"{LOG_TAG} _orbitalPauseController is not assigned.", this);
            if (_orbitVisibilityController == null)
                Debug.LogWarning($"{LOG_TAG} _orbitVisibilityController is not assigned.", this);
            if (_planetPrefab == null)
                Debug.LogWarning($"{LOG_TAG} _planetPrefab is not assigned.", this);
            if (_dataCard == null)
                Debug.LogWarning($"{LOG_TAG} _dataCard is not assigned.", this);
        }

        #endregion
    }
}
