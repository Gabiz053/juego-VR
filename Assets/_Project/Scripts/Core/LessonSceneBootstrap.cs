using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Repositions the XR player to the designated spawn point when a lesson scene loads,
    /// both on initial Start and whenever SceneController signals a completed transition.
    /// Place on a service GameObject in any lesson scene (SolarSystem, KeplerLab, Sandbox…).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Lesson Scene Bootstrap")]
    public sealed class LessonSceneBootstrap : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[LessonSceneBootstrap]";

        #endregion

        #region Inspector

        [Header("Dependencies")]
        [Tooltip("Spawn point the player is placed at when this lesson scene loads. " +
                 "Create an empty GameObject, name it [PlayerSpawnPoint], position it where " +
                 "the player should appear, and drag it here.")]
        [SerializeField] private Transform _spawnPoint;

        #endregion

        #region Cached Components

        private Unity.XR.CoreUtils.XROrigin _xrOrigin;
        private bool _isSubscribed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Subscribe early (before Start) so we don't miss the OnTransitionCompleted event
            // in the same frame the scene activates.
            SubscribeToSceneController();
        }

        private void Start()
        {
            ValidateReferences();

            // Retry subscription in case SceneController wasn't ready during Awake.
            if (!_isSubscribed)
                SubscribeToSceneController();

            RepositionPlayer();
        }

        private void OnDisable()
        {
            UnsubscribeFromSceneController();
        }

        #endregion

        #region Internals

        private void RepositionPlayer()
        {
            if (_spawnPoint == null)
            {
                Debug.LogWarning($"{LOG_TAG} _spawnPoint is not assigned -- cannot reposition player.", this);
                return;
            }

            if (!TryGetXROrigin(out var xrOrigin))
            {
                Debug.LogWarning($"{LOG_TAG} XROrigin not found in scene.", this);
                return;
            }

            var cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            xrOrigin.transform.SetPositionAndRotation(_spawnPoint.position, _spawnPoint.rotation);

            if (cc != null) cc.enabled = true;

            Debug.Log($"{LOG_TAG} Player repositioned to spawn point -- {_spawnPoint.position}.");
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
            if (!_isSubscribed) return;

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
