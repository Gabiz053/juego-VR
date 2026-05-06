using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.UI;
using _Project.Scripts.Planets;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Connects WristMenuController events to global systems (SceneController, GameManager)
    /// within the KeplerLab 2 scene.
    /// Place on a service GameObject in the scene (e.g. Svc_SceneConnector).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Kepler Lab2 Scene Connector")]
    public sealed class KeplerLab2SceneConnector : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[KeplerLab2SceneConnector]";
        private const float MIN_SPAWN_DISTANCE = 0.1f;

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
        [Tooltip("Exact name of the main menu scene (must be in Build Settings).")]
        [SerializeField] private string _mainMenuSceneName = "Main_VR";

        [Tooltip("Index of this scene within the Kepler sequence (0 = KeplerLab1, 1 = KeplerLab2, 2 = KeplerLab3).")]
        [SerializeField] private int _currentSceneIndex = 1;

        [Header("Spawn Settings")]
        [Tooltip("Fallback prefab used when _planetPrefabs is empty or has no valid entries.")]
        [SerializeField] private GameObject _planetPrefab;

        [Tooltip("Pool of planet prefabs used for random spawn in KeplerLab 1.")]
        [SerializeField] private List<GameObject> _planetPrefabs = new();

        [Tooltip("Transform de la camara del jugador para calcular posicion de spawn.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Distancia frente al jugador donde aparece el planeta.")]
        [SerializeField] private float _spawnDistance = 1.5f;

        [Header("Explanation Panel (Pause)")]
        [Tooltip("Si esta activado, al entrar en la escena aparece un panel con instrucciones " +
                 "de uso para la Segunda Ley de Kepler.")]
        [SerializeField] private bool _showIntroPanel = false;

        [Header("Panel Style")]
        [Tooltip("If enabled, enforces the same panel style used across Kepler 1 and Kepler 2.")]
        [SerializeField] private bool _useUnifiedPanelStyle = true;

        [Header("Intro Panel — Timing")]
        [Tooltip("Seconds the intro panel takes to fade in.")]
        [SerializeField] private float _introFadeInDuration = 1.0f;

        [Tooltip("Seconds the intro panel stays fully visible before fading out.")]
        [SerializeField] private float _introHoldDuration = 4.0f;

        [Tooltip("Seconds the intro panel takes to fade out.")]
        [SerializeField] private float _introFadeOutDuration = 1.5f;

        [TextArea(4, 12)]
        [Tooltip("Texto inicial que explica como funciona la escena.")]
        [SerializeField] private string _introText =
            "<size=120%><b>KeplerLab 2</b></size>\n" +
            "<size=80%>(Segunda Ley de Kepler)</size>\n\n" +
            "1. Pulsa <b>Spawn Planet</b> para crear un planeta.\n" +
            "2. Manten pulsado <b>grip</b> (cualquier mano) para iniciar la medicion.\n" +
            "3. Suelta grip para calcular el tiempo asociado al area barrida.\n" +
            "4. Repite y compara en distintas zonas de la orbita.\n\n" +
            "<size=80%>En tiempos iguales, el radio vector barre areas iguales.</size>";

        [Tooltip("Si esta activado, al pulsar pausa aparece un panel TMP en la muneca " +
                 "izquierda con una explicacion de la 2da Ley de Kepler. " +
                 "Replicamos el patron del HUD de KeplerLab2Controller.")]
        [SerializeField] private bool _showExplanationOnPause = true;

        [Tooltip("If enabled, KeplerLab 2/3 pause explanation is placed above the Sun instead of on the wrist.")]
        [SerializeField] private bool _placeKepler2PausePanelAboveSun = true;

        [Tooltip("World-space offset from Sun center for KeplerLab 2 pause explanation panel.")]
        [SerializeField] private Vector3 _kepler2PausePanelSunOffset = new Vector3(0f, 2.2f, 0f);

        [Tooltip("Transform de la muneca / mando derecho. Si esta vacio, se autoresuelve " +
                 "buscando un transform llamado 'Right Controller' / 'RightHand' en escena.")]
        [SerializeField] private Transform _rightControllerAnchor;

        [Tooltip("Offset local (m) respecto al mando izquierdo donde se ancla el panel " +
                 "(por defecto ~18 cm por encima de la muneca).")]
        [SerializeField] private Vector3 _panelLocalOffset = new Vector3(0f, 0.18f, 0.05f);

        [Tooltip("Rotacion local (grados) respecto al mando izquierdo. Pequeño pitch " +
                 "para que el texto mire hacia el jugador.")]
        [SerializeField] private Vector3 _panelLocalEuler = new Vector3(45f, 0f, 0f);

        [Tooltip("Escala local del panel cuando se ancla al mando. 0.08 ≈ 24 cm × 13 cm " +
                 "con texto de ~1.3 cm de alto.")]
        [SerializeField] private float _panelControllerScale = 0.08f;

        [Tooltip("Distancia (m) desde la camara cuando no encontramos el mando izquierdo (fallback).")]
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
            "<size=120%><b>Segunda Ley de Kepler</b></size>\n" +
            "<size=80%>(Ley de las areas)</size>\n\n" +
            "En tiempos iguales, el radio vector planeta-Sol barre <b>areas iguales</b>.\n\n" +
            "● Cerca del Sol, el planeta se mueve mas rapido.\n" +
            "● Lejos del Sol, se mueve mas lento.\n" +
            "● Lo constante es el area barrida por unidad de tiempo.\n\n" +
            "<size=80%>Pulsa play/pausa de nuevo para reanudar.</size>";

        [Header("Unified Kepler Style Values")]
        [Tooltip("Unified panel local offset used by Kepler wrist text panels.")]
        [SerializeField] private Vector3 _unifiedPanelLocalOffset = new Vector3(0f, 0.11f, 0.05f);

        [Tooltip("Unified panel local euler used by Kepler wrist text panels.")]
        [SerializeField] private Vector3 _unifiedPanelLocalEuler = new Vector3(45f, 0f, 0f);

        [Tooltip("Unified panel wrist scale used by Kepler wrist text panels.")]
        [SerializeField] private float _unifiedPanelControllerScale = 0.08f;

        [Tooltip("Unified panel size used by Kepler wrist text panels.")]
        [SerializeField] private Vector2 _unifiedPanelSize = new Vector2(3f, 1.6f);

        [Tooltip("Unified panel font size used by Kepler wrist text panels.")]
        [SerializeField] private float _unifiedPanelFontSize = 0.16f;

        #endregion

        #region Constants — Kepler Scenes

        private const string INTRO_PANEL_NAME = "HUD_KeplerLab2_Intro";
        private const string EXPLANATION_PANEL_NAME = "HUD_KeplerLab2_Explanation";
        private static readonly string[] KEPLER_SCENES =
        {
            "KeplerLab 1",
            "KeplerLab 2",
            "KeplerLab 3"
        };

        #endregion

        #region Events
        #endregion

        #region Cached Components

        // Runtime objects para el panel de explicacion que aparece al pausar.
        private const string TMP_FONT_RESOURCE_PATH = "Fonts & Materials/LiberationSans SDF";
        private GameObject _introPanelGo;
        private TextMeshProUGUI _introLabel;
        private CanvasGroup _introCanvasGroup;
        private Coroutine _introFadeCoroutine;
        private GameObject _explanationPanelGo;
        private TextMeshProUGUI _explanationLabel;
        private readonly List<OrbitalLauncher> _spawnedLaunchers = new();
        private bool _hasPlacedPlanetInKeplerLab1;
        private Transform _sunTransform;
        private Renderer _sunRenderer;

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            ApplyUnifiedPanelStyleIfEnabled();
            CleanupRuntimePanels();
            _spawnDistance = Mathf.Max(MIN_SPAWN_DISTANCE, _spawnDistance);
            TryResolveRightControllerAnchor();
            ValidateReferences();
            SubscribeEvents();

            // Desactivar boton spawn si no estamos en KeplerLab 1
            if (_wristMenuController != null)
            {
                _wristMenuController.SetSpawnButtonInteractable(_currentSceneIndex == 0);
                _wristMenuController.SetNextLawButtonInteractable(_currentSceneIndex != 0);
            }

            if (_showIntroPanel)
                ShowIntroPanel();

            Debug.Log($"{LOG_TAG} Initialized -- scene index: {_currentSceneIndex}, events subscribed.");
        }

        private void OnDisable()
        {
            CleanupRuntimePanels();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            UnsubscribePlanetLaunchEvents();
            CleanupRuntimePanels();
        }

        #endregion

        #region Internals

        private void SubscribeEvents()
        {
            if (_wristMenuController == null) return;

            _wristMenuController.OnBackPressed          += HandleBackPressed;
            _wristMenuController.OnPausePressed         += HandlePausePressed;
            _wristMenuController.OnToggleOrbitsPressed  += HandleToggleOrbitsPressed;
            _wristMenuController.OnSpawnPlanetPressed   += HandleSpawnPlanetPressed;
            _wristMenuController.OnNextLawPressed       += HandleNextLawPressed;
        }

        private void UnsubscribeEvents()
        {
            if (_wristMenuController == null) return;

            _wristMenuController.OnBackPressed          -= HandleBackPressed;
            _wristMenuController.OnPausePressed         -= HandlePausePressed;
            _wristMenuController.OnToggleOrbitsPressed  -= HandleToggleOrbitsPressed;
            _wristMenuController.OnSpawnPlanetPressed   -= HandleSpawnPlanetPressed;
            _wristMenuController.OnNextLawPressed       -= HandleNextLawPressed;
        }

        private void HandleBackPressed()
        {
            if (SceneController.Instance == null)
            {
                Debug.LogWarning($"{LOG_TAG} SceneController.Instance is null.", this);
                return;
            }

            CleanupRuntimePanels();
            Debug.Log($"{LOG_TAG} Back pressed -- returning to main menu.");
            SceneController.Instance.LoadScene(_mainMenuSceneName, GameState.MainMenu);
        }

        private void HandlePausePressed()
        {
            if (_orbitalPauseController == null)
            {
                Debug.LogWarning($"{LOG_TAG} _orbitalPauseController is not assigned.", this);
                return;
            }

            _orbitalPauseController.TogglePause();
            _wristMenuController.SetPauseIcon(_orbitalPauseController.IsPaused);

            // Mostramos / ocultamos el panel de explicacion al estilo de
            // KeplerLab2Controller: cuando la simulacion se detiene, el alumno
            // ve la explicacion de la 1ra Ley en su muneca izquierda.
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
            if (_orbitVisibilityController == null)
            {
                Debug.LogWarning($"{LOG_TAG} _orbitVisibilityController is not assigned.", this);
                return;
            }

            _orbitVisibilityController.ToggleVisibility();
            _wristMenuController.SetOrbitIcon(_orbitVisibilityController.IsVisible);
            Debug.Log($"{LOG_TAG} Orbits visible: {_orbitVisibilityController.IsVisible}.");
        }

        private void HandleSpawnPlanetPressed()
        {
            if (_currentSceneIndex != 0)
            {
                Debug.Log($"{LOG_TAG} Spawn blocked -- only allowed in KeplerLab 1.");
                return;
            }

            GameObject planetPrefab = ResolveSpawnPrefab();
            if (planetPrefab == null)
            {
                Debug.LogWarning($"{LOG_TAG} No planet prefab available for random spawn.", this);
                return;
            }

            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            if (_cameraTransform == null)
            {
                Debug.LogWarning($"{LOG_TAG} _cameraTransform is not assigned.", this);
                return;
            }

            Vector3 spawnPos = _cameraTransform.position + _cameraTransform.forward * _spawnDistance;
            GameObject spawnedPlanet = Instantiate(planetPrefab, spawnPos, Quaternion.identity);
            SubscribePlanetLaunchEvent(spawnedPlanet);
            Debug.Log($"{LOG_TAG} Planet spawned at {spawnPos} -- prefab: {planetPrefab.name}.");
        }

        private void HandleNextLawPressed()
        {
            if (_currentSceneIndex == 0 && !_hasPlacedPlanetInKeplerLab1)
            {
                Debug.Log($"{LOG_TAG} Next law blocked -- place and release one planet first.");
                return;
            }

            if (SceneController.Instance == null)
            {
                Debug.LogWarning($"{LOG_TAG} SceneController.Instance is null.", this);
                return;
            }

            int nextIndex = _currentSceneIndex + 1;

            if (nextIndex >= KEPLER_SCENES.Length)
            {
                CleanupRuntimePanels();
                Debug.Log($"{LOG_TAG} Next law pressed -- already on last Kepler scene, returning to main menu.");
                SceneController.Instance.LoadScene(_mainMenuSceneName, GameState.MainMenu);
                return;
            }

            string nextScene = KEPLER_SCENES[nextIndex];
            CleanupRuntimePanels();
            Debug.Log($"{LOG_TAG} Next law pressed -- loading {nextScene} (index {nextIndex}).");
            SceneController.Instance.LoadScene(nextScene, GameState.KeplerLab);
        }

        private GameObject ResolveSpawnPrefab()
        {
            List<GameObject> candidates = null;
            if (_planetPrefabs != null && _planetPrefabs.Count > 0)
            {
                candidates = new List<GameObject>(_planetPrefabs.Count);
                for (int i = 0; i < _planetPrefabs.Count; i++)
                    if (_planetPrefabs[i] != null)
                        candidates.Add(_planetPrefabs[i]);
            }

            if (candidates != null && candidates.Count > 0)
            {
                int selectedIndex = UnityEngine.Random.Range(0, candidates.Count);
                return candidates[selectedIndex];
            }

            return _planetPrefab;
        }

        private bool HasAnyConfiguredSpawnPrefab()
        {
            if (_planetPrefab != null)
                return true;

            if (_planetPrefabs == null)
                return false;

            for (int i = 0; i < _planetPrefabs.Count; i++)
                if (_planetPrefabs[i] != null)
                    return true;

            return false;
        }

        private void SubscribePlanetLaunchEvent(GameObject spawnedPlanet)
        {
            if (spawnedPlanet == null)
                return;

            OrbitalLauncher launcher = spawnedPlanet.GetComponentInChildren<OrbitalLauncher>();
            if (launcher == null)
            {
                Debug.LogWarning($"{LOG_TAG} Spawned planet has no OrbitalLauncher -- Next Law cannot auto-unlock.", spawnedPlanet);
                return;
            }

            launcher.OnOrbitLaunched -= HandleOrbitLaunched;
            launcher.OnOrbitLaunched += HandleOrbitLaunched;
            if (!_spawnedLaunchers.Contains(launcher))
                _spawnedLaunchers.Add(launcher);
        }

        private void UnsubscribePlanetLaunchEvents()
        {
            for (int i = 0; i < _spawnedLaunchers.Count; i++)
            {
                OrbitalLauncher launcher = _spawnedLaunchers[i];
                if (launcher != null)
                    launcher.OnOrbitLaunched -= HandleOrbitLaunched;
            }

            _spawnedLaunchers.Clear();
        }

        private void HandleOrbitLaunched(float semiMajorAxis, float orbitalPeriod)
        {
            _ = semiMajorAxis;
            _ = orbitalPeriod;

            if (_currentSceneIndex != 0 || _hasPlacedPlanetInKeplerLab1)
                return;

            _hasPlacedPlanetInKeplerLab1 = true;
            _wristMenuController?.SetNextLawButtonInteractable(true);
            Debug.Log($"{LOG_TAG} Orbit launched -- Next Law unlocked.");
        }

        #endregion

        #region Internals -- Intro Panel ----------------------------------------

        private void ShowIntroPanel()
        {
            if (_introPanelGo == null)
                CreateIntroPanel();
            if (_introPanelGo == null) return;

            if (_introLabel != null)
                _introLabel.text = _introText;

            // Stop any running fade before starting fresh
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

            _introPanelGo = new GameObject(INTRO_PANEL_NAME);

            // CanvasGroup lets us tween the whole panel's alpha in one place
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

        /// <summary>
        /// Fades the intro panel in, holds it, then fades it out and deactivates it.
        /// Timing controlled by _introFadeInDuration, _introHoldDuration, _introFadeOutDuration.
        /// </summary>
        private IEnumerator IntroPanelFadeSequence()
        {
            float fadeIn   = Mathf.Max(0f, _introFadeInDuration);
            float hold     = Mathf.Max(0f, _introHoldDuration);
            float fadeOut  = Mathf.Max(0f, _introFadeOutDuration);

            // --- Fade In ---
            float elapsed = 0f;
            while (elapsed < fadeIn)
            {
                elapsed += Time.deltaTime;
                if (_introCanvasGroup != null)
                    _introCanvasGroup.alpha = Mathf.Clamp01(elapsed / Mathf.Max(fadeIn, 0.001f));
                yield return null;
            }
            if (_introCanvasGroup != null) _introCanvasGroup.alpha = 1f;

            // --- Hold ---
            yield return new WaitForSeconds(hold);

            // --- Fade Out ---
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

        #region Internals -- Explanation Panel ----------------------------------

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
            if (_explanationPanelGo == null) return; // creacion fallo (sin font/camara)

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

            _explanationPanelGo = new GameObject(EXPLANATION_PANEL_NAME);
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

            Camera cam = Camera.main;
            Vector3 panelPos = cam.transform.position
                             + cam.transform.forward * _messageDistance
                             + Vector3.up * _messageHeightOffset;
            panelGo.transform.position = panelPos;
            panelGo.transform.rotation =
                Quaternion.LookRotation(panelPos - cam.transform.position, Vector3.up);
            return true;
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

            Vector3 panelPosition = GetSunWorldPosition() + _kepler2PausePanelSunOffset;
            panelGo.transform.SetParent(null, worldPositionStays: true);
            panelGo.transform.position = panelPosition;

            if (Camera.main != null)
                panelGo.transform.rotation = Quaternion.LookRotation(panelPosition - Camera.main.transform.position, Vector3.up);
            else
                panelGo.transform.rotation = Quaternion.identity;

            return true;
        }

        private bool ShouldPlacePausePanelOverSun()
        {
            return _placeKepler2PausePanelAboveSun
                && (_currentSceneIndex == 1 || _currentSceneIndex == 2);
        }

        private TextMeshProUGUI BuildPanelVisuals(GameObject panelGo, TMP_FontAsset font, string textObjectName)
        {
            var canvas = panelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var canvasRect = panelGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(_messagePanelSize.x * 100f, _messagePanelSize.y * 100f);
            bool anchoredToController = _rightControllerAnchor != null
                && panelGo.transform.parent == _rightControllerAnchor;
            float baseScale = anchoredToController ? _panelControllerScale * 0.01f : 0.01f;
            canvasRect.localScale = Vector3.one * baseScale;

            var bgGo = new GameObject("Img_PanelBackground");
            bgGo.transform.SetParent(panelGo.transform, worldPositionStays: false);
            var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var textGo = new GameObject(textObjectName);
            textGo.transform.SetParent(panelGo.transform, worldPositionStays: false);
            var label = textGo.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = _messageFontSize * 100f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 20);
            textRect.offsetMax = new Vector2(-20, -20);

            return label;
        }

        private void ApplyUnifiedPanelStyleIfEnabled()
        {
            if (!_useUnifiedPanelStyle)
                return;

            _panelLocalOffset = _unifiedPanelLocalOffset;
            _panelLocalEuler = _unifiedPanelLocalEuler;
            _panelControllerScale = _unifiedPanelControllerScale;
            _messagePanelSize = _unifiedPanelSize;
            _messageFontSize = _unifiedPanelFontSize;
        }

        private void CleanupRuntimePanels()
        {
            if (_introFadeCoroutine != null)
            {
                StopCoroutine(_introFadeCoroutine);
                _introFadeCoroutine = null;
            }

            if (_introPanelGo != null)
            {
                Destroy(_introPanelGo);
                _introPanelGo = null;
            }

            if (_explanationPanelGo != null)
            {
                Destroy(_explanationPanelGo);
                _explanationPanelGo = null;
            }

            _introLabel = null;
            _introCanvasGroup = null;
            _explanationLabel = null;

            DestroyOrphanPanelByName(INTRO_PANEL_NAME);
            DestroyOrphanPanelByName(EXPLANATION_PANEL_NAME);
        }

        private static void DestroyOrphanPanelByName(string panelName)
        {
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform current = transforms[i];
                if (!string.Equals(current.name, panelName, StringComparison.Ordinal))
                    continue;

                UnityEngine.Object.Destroy(current.gameObject);
            }
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
            if (string.IsNullOrWhiteSpace(_mainMenuSceneName))
                Debug.LogWarning($"{LOG_TAG} _mainMenuSceneName is not assigned.", this);
            if (!HasAnyConfiguredSpawnPrefab())
                Debug.LogWarning($"{LOG_TAG} No spawn prefabs assigned (_planetPrefabs/_planetPrefab).", this);
            if (_cameraTransform == null)
                Debug.LogWarning($"{LOG_TAG} _cameraTransform is not assigned.", this);
            if (_currentSceneIndex < 0 || _currentSceneIndex >= KEPLER_SCENES.Length)
                Debug.LogWarning($"{LOG_TAG} _currentSceneIndex {_currentSceneIndex} is out of range.", this);
        }

        #endregion
    }
}
