using System;
using TMPro;
using UnityEngine;
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
        [Tooltip("Si esta activado, al pulsar pausa aparece un panel TMP en la muneca " +
                 "izquierda con una explicacion de la 1ra Ley de Kepler. " +
                 "Replicamos el patron del HUD de KeplerLab2Controller.")]
        [SerializeField] private bool _showExplanationOnPause = true;

        [Tooltip("Transform de la muneca / mando izquierdo. Si esta vacio, se autoresuelve " +
                 "buscando un transform llamado 'Left Controller' / 'LeftHand' en escena.")]
        [SerializeField] private Transform _leftControllerAnchor;

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
            "<size=80%>Pulsa pausa de nuevo para reanudar.</size>";

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
            TryResolveLeftControllerAnchor();
            ValidateReferences();
            SubscribeEvents();

            // Desactivar boton spawn si no estamos en KeplerLab 1
            if (_wristMenuController != null)
                _wristMenuController.SetSpawnButtonInteractable(_currentSceneIndex == 0);

            Debug.Log($"{LOG_TAG} Initialized -- scene index: {_currentSceneIndex}, events subscribed.");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
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

        #region Internals -- Explanation Panel ----------------------------------

        private void TryResolveLeftControllerAnchor()
        {
            if (_leftControllerAnchor != null) return;

            Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            Transform exact = null;
            Transform fuzzy = null;
            for (int i = 0; i < all.Length; i++)
            {
                string n     = all[i].name;
                string lower = n.ToLowerInvariant();
                if (string.Equals(n, "Left Controller", StringComparison.Ordinal))
                {
                    exact = all[i];
                    break;
                }
                if (fuzzy == null
                    && (lower.Contains("leftcontroller")
                        || lower.Contains("left controller")
                        || lower.Contains("lefthand")
                        || lower.Contains("left hand")))
                {
                    fuzzy = all[i];
                }
            }

            _leftControllerAnchor = exact != null ? exact : fuzzy;
            if (_leftControllerAnchor != null)
                Debug.Log($"{LOG_TAG} Auto-assigned _leftControllerAnchor: {_leftControllerAnchor.name}.");
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
            // Misma estrategia que KeplerLab2Controller.CreateMessagePanel: si
            // tenemos referencia al mando izquierdo lo anclamos ahi (HUD de
            // muneca); si no, fallback delante de la camara.
            TMP_FontAsset font = _messageFont;
            if (font == null)
                font = Resources.Load<TMP_FontAsset>(TMP_FONT_RESOURCE_PATH);
            if (font == null && TMP_Settings.instance != null)
                font = TMP_Settings.defaultFontAsset;

            if (font == null)
            {
                Debug.LogWarning($"{LOG_TAG} No TMP font available -- explanation panel skipped.", this);
                return;
            }

            _explanationPanelGo = new GameObject("HUD_KeplerLab1_Explanation");

            if (_leftControllerAnchor != null)
            {
                _explanationPanelGo.transform.SetParent(_leftControllerAnchor, worldPositionStays: false);
                _explanationPanelGo.transform.localPosition = _panelLocalOffset;
                _explanationPanelGo.transform.localRotation = Quaternion.Euler(_panelLocalEuler);
            }
            else
            {
                if (Camera.main == null)
                {
                    Debug.LogWarning($"{LOG_TAG} No main camera and no left controller -- cannot place explanation panel.", this);
                    Destroy(_explanationPanelGo);
                    _explanationPanelGo = null;
                    return;
                }

                Camera cam = Camera.main;
                Vector3 panelPos = cam.transform.position
                                 + cam.transform.forward * _messageDistance
                                 + Vector3.up * _messageHeightOffset;
                _explanationPanelGo.transform.position = panelPos;
                _explanationPanelGo.transform.rotation =
                    Quaternion.LookRotation(panelPos - cam.transform.position, Vector3.up);
            }

            var canvas = _explanationPanelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            var canvasRect = _explanationPanelGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(_messagePanelSize.x * 100f, _messagePanelSize.y * 100f);
            float baseScale = _leftControllerAnchor != null ? _panelControllerScale * 0.01f : 0.01f;
            canvasRect.localScale = Vector3.one * baseScale;

            // Fondo translucido.
            var bgGo = new GameObject("Img_PanelBackground");
            bgGo.transform.SetParent(_explanationPanelGo.transform, worldPositionStays: false);
            var bgImage = bgGo.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.55f);
            var bgRect = bgGo.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Texto.
            var textGo = new GameObject("Txt_Explanation");
            textGo.transform.SetParent(_explanationPanelGo.transform, worldPositionStays: false);
            _explanationLabel = textGo.AddComponent<TextMeshProUGUI>();
            _explanationLabel.font = font;
            _explanationLabel.fontSize = _messageFontSize * 100f;
            _explanationLabel.alignment = TextAlignmentOptions.Center;
            _explanationLabel.color = Color.white;
            _explanationLabel.textWrappingMode = TextWrappingModes.Normal;
            _explanationLabel.text = _explanationText;

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 20);
            textRect.offsetMax = new Vector2(-20, -20);
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