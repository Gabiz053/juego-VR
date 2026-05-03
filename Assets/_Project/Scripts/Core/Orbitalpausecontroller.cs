using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Pauses and resumes the orbital movement of all planets in the scene.
    /// Finds all SplineAnimate components at Start and controls them as a group.
    /// Attach to the same GameObject as SolarSystemSceneConnector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Orbital Pause Controller")]
    public sealed class OrbitalPauseController : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[OrbitalPauseController]";

        #endregion

        #region Inspector
        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region State

        private readonly List<SplineAnimate> _animators = new();
        private bool _isPaused;

        #endregion

        #region Public API

        /// <summary>True while orbits are paused.</summary>
        public bool IsPaused => _isPaused;

        /// <summary>Toggles between pausing and resuming all orbits.</summary>
        public void TogglePause()
        {
            if (_isPaused)
                ResumeOrbits();
            else
                PauseOrbits();
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            CacheAnimators();
            Debug.Log($"{LOG_TAG} Initialized -- {_animators.Count} orbital animators found.");
        }

        #endregion

        #region Internals

        private void CacheAnimators()
        {
            _animators.Clear();
            var found = FindObjectsByType<SplineAnimate>(FindObjectsSortMode.None);
            foreach (var anim in found)
                _animators.Add(anim);
        }

        private void PauseOrbits()
        {
            foreach (var anim in _animators)
                if (anim != null) anim.Pause();

            _isPaused = true;
            Debug.Log($"{LOG_TAG} Orbits paused.");
        }

        private void ResumeOrbits()
        {
            foreach (var anim in _animators)
                if (anim != null) anim.Play();

            _isPaused = false;
            Debug.Log($"{LOG_TAG} Orbits resumed.");
        }

        #endregion

        #region Validation

        private void ValidateReferences() { }

        #endregion
    }
}
