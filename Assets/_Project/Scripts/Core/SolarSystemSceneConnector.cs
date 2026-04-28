using UnityEngine;
using _Project.Scripts.UI;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Conecta los eventos del WristMenuController con los sistemas globales
    /// (SceneController, GameManager) en la escena del sistema solar.
    /// Coloca este script en un GameObject de servicio de la escena (Svc_SceneConnector).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Solar System Scene Connector")]
    public sealed class SolarSystemSceneConnector : MonoBehaviour
    {
        #region Inspector

        [Header("Dependencies")]
        [Tooltip("Referencia al WristMenuController del XR Rig.")]
        [SerializeField] private WristMenuController _wristMenuController;

        [Header("Scene Settings")]
        [Tooltip("Nombre exacto de la escena del menu principal (debe estar en Build Settings).")]
        [SerializeField] private string _mainMenuSceneName = "Menu";

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            SubscribeEvents();

            Debug.Log("[SolarSystemSceneConnector] Initialized -- events subscribed.");
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        #endregion

        #region Internals

        private void SubscribeEvents()
        {
            if (_wristMenuController == null)
                return;

            _wristMenuController.OnBackPressed += HandleBackPressed;
            _wristMenuController.OnPausePressed += HandlePausePressed;
            _wristMenuController.OnToggleOrbitsPressed += HandleToggleOrbitsPressed;
        }

        private void UnsubscribeEvents()
        {
            if (_wristMenuController == null)
                return;

            _wristMenuController.OnBackPressed -= HandleBackPressed;
            _wristMenuController.OnPausePressed -= HandlePausePressed;
            _wristMenuController.OnToggleOrbitsPressed -= HandleToggleOrbitsPressed;
        }

        private void HandleBackPressed()
        {
            if (SceneController.Instance == null)
            {
                Debug.LogWarning("[SolarSystemSceneConnector] SceneController.Instance is null.", this);
                return;
            }

            Debug.Log("[SolarSystemSceneConnector] Back pressed -- returning to main menu.");
            SceneController.Instance.LoadScene(_mainMenuSceneName, GameState.MainMenu);
        }

        private void HandlePausePressed()
        {
            // TODO: conectar al sistema de pausa cuando este implementado
            Debug.Log("[SolarSystemSceneConnector] Pause pressed -- pending implementation.");
        }

        private void HandleToggleOrbitsPressed()
        {
            // TODO: conectar al sistema de orbitas cuando este implementado
            Debug.Log("[SolarSystemSceneConnector] Toggle orbits pressed -- pending implementation.");
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_wristMenuController == null)
                Debug.LogWarning("[SolarSystemSceneConnector] _wristMenuController is not assigned.", this);

            if (string.IsNullOrWhiteSpace(_mainMenuSceneName))
                Debug.LogWarning("[SolarSystemSceneConnector] _mainMenuSceneName is not assigned.", this);
        }

        #endregion
    }
}