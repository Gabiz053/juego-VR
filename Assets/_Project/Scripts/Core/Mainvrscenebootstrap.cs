using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Repositions the XR player to the designated spawn point when Main_VR loads
    /// and whenever SceneController signals a completed scene transition back to it.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Main VR Scene Bootstrap")]
    public sealed class MainVRSceneBootstrap : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[MainVRSceneBootstrap]";

        #endregion

        #region Inspector

        [Header("Dependencies")]
        [Tooltip("Spawn point the player is placed at when this scene loads.")]
        [SerializeField] private Transform _spawnPoint;

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (SceneController.Instance != null)
                SceneController.Instance.OnTransitionCompleted += RepositionPlayer;
        }

        private void OnDisable()
        {
            if (SceneController.Instance != null)
                SceneController.Instance.OnTransitionCompleted -= RepositionPlayer;
        }

        private void Start()
        {
            ValidateReferences();
            RepositionPlayer();
        }

        #endregion

        #region Internals

        private void RepositionPlayer()
        {
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogWarning($"{LOG_TAG} XROrigin not found.", this);
                return;
            }

            var cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            xrOrigin.transform.SetPositionAndRotation(
                SessionContext.MainMenuSpawnPosition,
                SessionContext.MainMenuSpawnRotation
            );

            if (cc != null) cc.enabled = true;

            Debug.Log($"{LOG_TAG} Player repositioned -- {SessionContext.MainMenuSpawnPosition}.");
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_spawnPoint == null)
                Debug.LogWarning($"{LOG_TAG} _spawnPoint is not assigned.", this);
        }

        #endregion
    }
}
