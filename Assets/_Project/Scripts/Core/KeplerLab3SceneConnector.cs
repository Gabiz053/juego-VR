using _Project.Scripts.Planets;
using _Project.Scripts.UI;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Conector especifico para KeplerLab 3.
    /// Igual que KeplerSceneConnector pero activa OrbitalDataCard al soltar el planeta.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Kepler Lab3 Scene Connector")]
    public sealed class KeplerLab3SceneConnector : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[KeplerLab3SceneConnector]";
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

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (_cameraTransform == null && Camera.main != null)
                _cameraTransform = Camera.main.transform;

            _spawnDistance = Mathf.Max(MIN_SPAWN_DISTANCE, _spawnDistance);
            ValidateReferences();
            SubscribeEvents();

            if (_wristMenuController != null)
                _wristMenuController.SetSpawnButtonInteractable(true);

            Debug.Log($"{LOG_TAG} Initialized -- KeplerLab3, events subscribed.");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            UnsubscribePlanetEvents();
        }

        #endregion

        #region Internals

        private void SubscribeEvents()
        {
            if (_wristMenuController == null) return;
            _wristMenuController.OnBackPressed += HandleBackPressed;
            _wristMenuController.OnPausePressed += HandlePausePressed;
            _wristMenuController.OnToggleOrbitsPressed += HandleToggleOrbitsPressed;
            _wristMenuController.OnSpawnPlanetPressed += HandleSpawnPlanetPressed;
            _wristMenuController.OnNextLawPressed += HandleNextLawPressed;
        }

        private void UnsubscribeEvents()
        {
            if (_wristMenuController == null) return;
            _wristMenuController.OnBackPressed -= HandleBackPressed;
            _wristMenuController.OnPausePressed -= HandlePausePressed;
            _wristMenuController.OnToggleOrbitsPressed -= HandleToggleOrbitsPressed;
            _wristMenuController.OnSpawnPlanetPressed -= HandleSpawnPlanetPressed;
            _wristMenuController.OnNextLawPressed -= HandleNextLawPressed;
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