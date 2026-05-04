using UnityEngine;
using _Project.Scripts.UI;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Connects WristMenuController events to global systems (SceneController, GameManager)
    /// within the sandbox scene.
    /// Place on a service GameObject in the scene (e.g. Svc_SceneConnector).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Sandbox Scene Connector")]
    public sealed class SandboxSceneConnector : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[SandboxSceneConnector]";

        #endregion

        #region Inspector

        [Header("Dependencies")]
        [Tooltip("WristMenuController on the XR Rig.")]
        [SerializeField] private WristMenuController _wristMenuController;

        [Header("Scene Settings")]
        [Tooltip("Exact name of the main menu scene (must be in Build Settings).")]
        [SerializeField] private string _mainMenuSceneName = "Main_VR";

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

            _wristMenuController.OnBackPressed += HandleBackPressed;
        }

        private void UnsubscribeEvents()
        {
            if (_wristMenuController == null) return;

            _wristMenuController.OnBackPressed -= HandleBackPressed;
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

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_wristMenuController == null)
                Debug.LogWarning($"{LOG_TAG} _wristMenuController is not assigned.", this);
            if (string.IsNullOrWhiteSpace(_mainMenuSceneName))
                Debug.LogWarning($"{LOG_TAG} _mainMenuSceneName is not assigned.", this);
        }

        #endregion
    }
}
