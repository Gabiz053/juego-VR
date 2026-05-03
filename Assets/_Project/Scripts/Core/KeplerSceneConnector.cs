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

        [Header("Spawn Settings")]
        [Tooltip("Prefab del planeta a instanciar (MarsOrbit).")]
        [SerializeField] private GameObject _planetPrefab;

        [Tooltip("Transform de la camara del jugador para calcular posicion de spawn.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Distancia frente al jugador donde aparece el planeta.")]
        [SerializeField] private float _spawnDistance = 1.5f;

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            SubscribeEvents();
            Debug.Log($"{LOG_TAG} Initialized -- events subscribed.");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
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
            if (_planetPrefab == null)
            {
                Debug.LogWarning($"{LOG_TAG} _planetPrefab is not assigned.", this);
                return;
            }

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
            // TODO: implementar logica de siguiente ley de Kepler
            Debug.Log($"{LOG_TAG} Next law pressed -- pending implementation.");
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
        }

        #endregion
    }
}
