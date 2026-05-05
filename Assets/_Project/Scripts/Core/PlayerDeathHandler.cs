using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Detects two death conditions and reloads the current scene via SceneController,
    /// which fades to black and respawns the player at the scene's default spawn point.
    ///   1. Head drops below _fallThreshold (Y-axis fall).
    ///   2. Head enters any spherical death zone (e.g. asteroid belt).
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
        [Tooltip("Head-tracking camera. Leave empty to use Camera.main.")]
        [SerializeField] private Camera _cameraOverride;

        [Header("Fall Detection")]
        [Tooltip("If the player's head Y drops below this value, death triggers.")]
        [SerializeField] private float _fallThreshold = -20f;

        [Header("Death Zones (asteroid belt, etc.)")]
        [Tooltip("World-space centre Transforms of spherical death zones.")]
        [SerializeField] private Transform[] _deathZoneCenters;

        [Tooltip("Uniform radius (metres) of each death zone sphere.")]
        [SerializeField, Min(0f)] private float _deathZoneRadius = 5f;

        [Header("Cooldown")]
        [Tooltip("Seconds before death can trigger again (prevents double-trigger during reload fade).")]
        [SerializeField, Min(0f)] private float _deathCooldown = 3f;

        #endregion

        #region Cached Components -----------------------------------------------

        private Camera _camera;
        private float  _lastDeathTime = -999f;
        private float  _deathZoneRadiusSqr;

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            _camera             = _cameraOverride != null ? _cameraOverride : Camera.main;
            _deathZoneRadiusSqr = _deathZoneRadius * _deathZoneRadius;
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
            Debug.Log($"{LOG_TAG} Player died -- {reason}. Reloading scene.");

            AudioManager.Instance?.PlayPlayerDeathSound();

            var sceneName  = SceneManager.GetActiveScene().name;
            var gameState  = GameManager.Instance != null
                ? GameManager.Instance.CurrentState
                : GameState.MainMenu;

            SceneController.Instance.LoadScene(sceneName, gameState);
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_camera == null)
                Debug.LogWarning($"{LOG_TAG} No camera found -- detection disabled until Camera.main available.", this);
            if (SceneController.Instance == null)
                Debug.LogWarning($"{LOG_TAG} SceneController not found -- death will not reload scene.", this);
        }

        #endregion
    }
}
