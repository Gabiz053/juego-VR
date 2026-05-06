using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.UI;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Connects WristMenuController events to global systems (SceneController, GameManager)
    /// within the solar system scene.
    /// Place on a service GameObject in the scene (e.g. Svc_SceneConnector).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Kepler Scene Connector")]
    public sealed class KeplerSceneConnector : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[KeplerSceneConnector]";
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
        [SerializeField] private int _currentSceneIndex = 0;

        [Header("Spawn Settings")]
        [Tooltip("Prefab del planeta a instanciar (MarsOrbit).")]
        [SerializeField] private GameObject _planetPrefab;

        [Tooltip("Transform de la camara del jugador para calcular posicion de spawn.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Distancia frente al jugador donde aparece el planeta.")]
        [SerializeField] private float _spawnDistance = 1.5f;

        [Header("Explanation Panel (Pause)")]
        [Tooltip("Si esta activado, al entrar en la escena aparece un panel con instrucciones " +
                 "de uso, igual que en KeplerLab 2.")]
        [SerializeField] private bool _showIntroPanel = true;

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
            "<size=120%><b>KeplerLab 1</b></size>\n" +
            "<size=80%>(Primera Ley de Kepler)</size>\n\n" +
            "1. Pulsa <b>Spawn Planet</b> para crear un planeta.\n" +
            "2. <b>Agarralo</b> y colocalo en cualquier punto del espacio.\n" +
            "3. Al soltarlo, el sistema genera una <b>orbita eliptica 3D</b> " +
            "con el <b>Sol en uno de los focos</b>.\n" +
            "4. Puedes pausar la simulacion para ver la explicacion de la ley.\n\n" +
            "<size=80%>Observa como cambia la forma de la orbita segun la posicion y el lanzamiento.</size>";

        [Tooltip("Si esta activado, al pulsar pausa aparece un panel TMP en la muneca " +
                 "izquierda con una explicacion de la 1ra Ley de Kepler. " +
                 "Replicamos el patron del HUD de KeplerLab2Controller.")]
        [SerializeField] private bool _showExplanationOnPause = true;

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
            "<size=120%><b>Primera Ley de Kepler</b></size>\n" +
            "<size=80%>(Ley de las orbitas)</size>\n\n" +
            "Todos los planetas describen <b>orbitas elipticas</b>, " +
            "con el <b>Sol</b> situado en uno de los <b>focos</b> de la elipse.\n\n" +
            "● <b>Perihelio</b>: punto mas cercano al Sol.\n" +
            "● <b>Afelio</b>: punto mas lejano al Sol.\n" +
            "● La <b>excentricidad</b> (e) define la forma:\n" +
            "    e = 0  -> circulo\n" +
            "    0 < e < 1 -> elipse\n\n" +
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

        #endregion

        #region Public API
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

            // Desactivar boton spawn si no estamos en KeplerLab 1
            if (_wristMenuController != null)
                _wristMenuController.SetSpawnButtonInteractable(_currentSceneIndex == 0);

            if (_showIntroPanel)
                ShowIntroPanel();

            Debug.Log($"{LOG_TAG} Initialized -- scene index: {_currentSceneIndex}, events subscribed.");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (_introPanelGo != null)
                Destroy(_introPanelGo);
            if (_explanationPanelGo != null)
                Destroy(_explanationPanelGo);
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

            if (_planetPrefab == null)
            {
                Debug.LogWarning($"{LOG_TAG} _planetPrefab is not assigned.", this);
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
            Instantiate(_planetPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"{LOG_TAG} Planet spawned at {spawnPos}.");
        }

        private void HandleNextLawPressed()
        {
            if (SceneController.Instance == null)
            {
                Debug.LogWarning($"{LOG_TAG} SceneController.Instance is null.", this);
                return;
            }

            int nextIndex = _currentSceneIndex + 1;

            if (nextIndex >= KEPLER_SCENES.Length)
            {
                Debug.Log($"{LOG_TAG} Next law pressed -- already on last Kepler scene, returning to main menu.");
                SceneController.Instance.LoadScene(_mainMenuSceneName, GameState.MainMenu);
                return;
            }

            string nextScene = KEPLER_SCENES[nextIndex];
            Debug.Log($"{LOG_TAG} Next law pressed -- loading {nextScene} (index {nextIndex}).");
            SceneController.Instance.LoadScene(nextScene, GameState.KeplerLab);
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

            _introPanelGo = new GameObject("HUD_KeplerLab1_Intro");

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

            _explanationPanelGo = new GameObject("HUD_KeplerLab1_Explanation");
            if (!TryPlacePanel(_explanationPanelGo))
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

        private bool TryPlacePanel(GameObject panelGo)
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

        private TextMeshProUGUI BuildPanelVisuals(GameObject panelGo, TMP_FontAsset font, string textObjectName)
        {
            var canvas = panelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var canvasRect = panelGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(_messagePanelSize.x * 100f, _messagePanelSize.y * 100f);
            float baseScale = _rightControllerAnchor != null ? _panelControllerScale * 0.01f : 0.01f;
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
            if (_planetPrefab == null)
                Debug.LogWarning($"{LOG_TAG} _planetPrefab is not assigned.", this);
            if (_cameraTransform == null)
                Debug.LogWarning($"{LOG_TAG} _cameraTransform is not assigned.", this);
            if (_currentSceneIndex < 0 || _currentSceneIndex >= KEPLER_SCENES.Length)
                Debug.LogWarning($"{LOG_TAG} _currentSceneIndex {_currentSceneIndex} is out of range.", this);
        }

        #endregion
    }
}
