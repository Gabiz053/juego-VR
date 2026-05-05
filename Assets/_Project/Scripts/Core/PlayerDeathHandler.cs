using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Detects two death conditions and respawns the player on the XR Origin:
    ///   1. Head drops below _fallThreshold (Y-axis fall out of scene).
    ///   2. Head enters any spherical death zone (e.g. the asteroid belt ring).
    /// Attach to the XR Origin root GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Player Death Handler")]
    public sealed class PlayerDeathHandler : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[PlayerDeathHandler]";

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Camera")]
        [Tooltip("Head-tracking camera used for position checks. Leave empty to use Camera.main.")]
        [SerializeField] private Camera _cameraOverride;

        [Header("Fall Detection")]
        [Tooltip("If the player's head Y drops below this value, death triggers.")]
        [SerializeField] private float _fallThreshold = -20f;

        [Header("Death Zones (asteroid belt, etc.)")]
        [Tooltip("World-space centre Transforms of spherical death zones.")]
        [SerializeField] private Transform[] _deathZoneCenters;

        [Tooltip("Uniform radius (metres) of each death zone sphere.")]
        [SerializeField, Min(0f)] private float _deathZoneRadius = 5f;

        [Header("Respawn")]
        [Tooltip("XR Origin is teleported to this position on death. Assign the scene spawn point.")]
        [SerializeField] private Transform _respawnPoint;

        [Tooltip("Seconds that must pass before death can trigger again. Prevents rapid re-death.")]
        [SerializeField, Min(0f)] private float _deathCooldown = 3f;

        #endregion

        #region Cached Components -----------------------------------------------

        private Camera _camera;
        private CharacterController _characterController;
        private float _lastDeathTime = -999f;
        private float _deathZoneRadiusSqr;

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            _camera              = _cameraOverride != null ? _cameraOverride : Camera.main;
            _characterController = GetComponent<CharacterController>();
            _deathZoneRadiusSqr  = _deathZoneRadius * _deathZoneRadius;
            ValidateReferences();
            Debug.Log($"{LOG_TAG} Initialized -- fallY={_fallThreshold:F1}, zones={(_deathZoneCenters?.Length ?? 0)}.");
        }

        private void Update()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null) return;
            }

            if (Time.time - _lastDeathTime < _deathCooldown) return;

            // Skip if a scene transition is already in progress.
            if (SceneController.Instance != null && SceneController.Instance.IsTransitioning) return;

            Vector3 headPos = _camera.transform.position;

            if (headPos.y < _fallThreshold)
            {
                TriggerDeath($"fell below y={_fallThreshold:F1}");
                return;
            }

            if (_deathZoneCenters == null) return;
            foreach (var zone in _deathZoneCenters)
            {
                if (zone == null) continue;
                if ((headPos - zone.position).sqrMagnitude <= _deathZoneRadiusSqr)
                {
                    TriggerDeath($"entered death zone '{zone.name}'");
                    return;
                }
            }
        }

        #endregion

        #region Internals -------------------------------------------------------

        private void TriggerDeath(string reason)
        {
            _lastDeathTime = Time.time;
            Debug.Log($"{LOG_TAG} Player died -- {reason}.");
            AudioManager.Instance?.PlayPlayerDeathSound();
            Respawn();
        }

        private void Respawn()
        {
            if (_respawnPoint == null)
            {
                Debug.LogWarning($"{LOG_TAG} _respawnPoint not assigned -- cannot teleport.", this);
                return;
            }

            // CharacterController must be disabled before moving transform directly,
            // otherwise Unity ignores the position change.
            if (_characterController != null)
                _characterController.enabled = false;

            transform.position = _respawnPoint.position;
            transform.rotation = _respawnPoint.rotation;

            if (_characterController != null)
                _characterController.enabled = true;

            Debug.Log($"{LOG_TAG} Respawned to {_respawnPoint.position}.");
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_camera == null)
                Debug.LogWarning($"{LOG_TAG} No camera found -- death detection disabled until Camera.main is available.", this);
            if (_respawnPoint == null)
                Debug.LogWarning($"{LOG_TAG} _respawnPoint is not assigned -- player will not teleport on death.", this);
        }

        #endregion
    }
}
