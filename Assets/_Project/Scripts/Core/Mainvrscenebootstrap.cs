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

        private Unity.XR.CoreUtils.XROrigin _xrOrigin;
        private bool _isSubscribed;

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SubscribeToSceneController();
        }

        private void OnDisable()
        {
            UnsubscribeFromSceneController();
        }

        private void Start()
        {
            ValidateReferences();

            if (!_isSubscribed)
                SubscribeToSceneController();

            if (!SessionContext.HasMainMenuSpawnOverride && _spawnPoint != null)
                SessionContext.SetMainMenuSpawn(_spawnPoint.position, _spawnPoint.rotation);

            RepositionPlayer();
        }

        #endregion

        #region Internals

        private void RepositionPlayer()
        {
            if (!TryGetXROrigin(out var xrOrigin))
            {
                Debug.LogWarning($"{LOG_TAG} XROrigin not found.", this);
                return;
            }

            Vector3 targetPosition = SessionContext.MainMenuSpawnPosition;
            Quaternion targetRotation = SessionContext.MainMenuSpawnRotation;

            if (!SessionContext.HasMainMenuSpawnOverride && _spawnPoint != null)
            {
                targetPosition = _spawnPoint.position;
                targetRotation = _spawnPoint.rotation;
            }

            var cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            xrOrigin.transform.SetPositionAndRotation(targetPosition, targetRotation);

            if (cc != null) cc.enabled = true;

            Debug.Log($"{LOG_TAG} Player repositioned -- {targetPosition}.");
        }

        private void SubscribeToSceneController()
        {
            if (_isSubscribed || SceneController.Instance == null)
                return;

            SceneController.Instance.OnTransitionCompleted += RepositionPlayer;
            _isSubscribed = true;
        }

        private void UnsubscribeFromSceneController()
        {
            if (!_isSubscribed)
                return;

            if (SceneController.Instance != null)
                SceneController.Instance.OnTransitionCompleted -= RepositionPlayer;

            _isSubscribed = false;
        }

        private bool TryGetXROrigin(out Unity.XR.CoreUtils.XROrigin xrOrigin)
        {
            if (_xrOrigin == null)
                _xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();

            xrOrigin = _xrOrigin;
            return xrOrigin != null;
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
