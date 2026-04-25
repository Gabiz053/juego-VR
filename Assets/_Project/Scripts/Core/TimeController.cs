using System;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Controls global game time (pause / resume).
    /// Setting Time.timeScale to 0 freezes all planet physics, orbits and particles
    /// without affecting the VR headset tracking (which runs at OS level).
    /// Call TogglePause() from the Wrist Menu button or via the Inspector context menu.
    /// Place this on the SceneManager GameObject in each gameplay scene.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/TimeController")]
    public sealed class TimeController : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[TimeController]";
        private const float NORMAL_TIME_SCALE = 1f;
        private const float PAUSED_TIME_SCALE  = 0f;

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Audio")]
        [Tooltip("Silences all Unity audio while paused. Disable if you want music/UI sounds to keep playing.")]
        [SerializeField] private bool _muteAudioOnPause = false;

        #endregion

        #region Events ----------------------------------------------------------

        /// <summary>Raised every time the pause state changes. True = paused, False = running.</summary>
        public event Action<bool> OnPauseStateChanged;

        #endregion

        #region Cached Components -----------------------------------------------

        private bool _isPaused;

        #endregion

        #region Public API ------------------------------------------------------

        /// <summary>True while the game is paused.</summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Toggles between paused and running. Safe to call from a single button.
        /// </summary>
        public void TogglePause()
        {
            if (_isPaused) Resume();
            else            Pause();
        }

        /// <summary>Freezes game time. No-op if already paused.</summary>
        public void Pause()
        {
            if (_isPaused) return;

            _isPaused        = true;
            Time.timeScale   = PAUSED_TIME_SCALE;

            if (_muteAudioOnPause)
                AudioListener.pause = true;

            OnPauseStateChanged?.Invoke(true);
            Debug.Log($"{LOG_TAG} Paused -- timeScale: {PAUSED_TIME_SCALE}.");
        }

        /// <summary>Restores game time. No-op if already running.</summary>
        public void Resume()
        {
            if (!_isPaused) return;

            _isPaused        = false;
            Time.timeScale   = NORMAL_TIME_SCALE;

            if (_muteAudioOnPause)
                AudioListener.pause = false;

            OnPauseStateChanged?.Invoke(false);
            Debug.Log($"{LOG_TAG} Resumed -- timeScale: {NORMAL_TIME_SCALE}.");
        }

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            // If this object is destroyed while paused (e.g. scene unload), restore time
            // so the next scene does not start frozen.
            if (!_isPaused) return;

            Time.timeScale = NORMAL_TIME_SCALE;

            if (_muteAudioOnPause)
                AudioListener.pause = false;

            Debug.Log($"{LOG_TAG} Destroyed while paused -- timeScale restored.");
        }

        #endregion

        #region Internals -------------------------------------------------------

        [ContextMenu("Toggle Pause (Play Mode Only)")]
        private void DebugTogglePause()
        {
            if (!Application.isPlaying) return;
            TogglePause();
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            // No serialized object references required.
            // Wrist Menu assigns TimeController via Inspector [SerializeField] on its own side.
        }

        #endregion
    }
}
