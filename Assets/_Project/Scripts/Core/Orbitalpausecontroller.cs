using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using _Project.Scripts.Planets;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Pauses and resumes all orbital SplineAnimates and planet self-rotations
    /// in the scene, including the Moon.
    /// The cache is delayed two frames so OrbitalSplineGenerators have finished
    /// their WaitForEndOfFrame coroutine before we search for components.
    /// Attach to the same GameObject as SolarSystemSceneConnector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Orbital Pause Controller")]
    public sealed class OrbitalPauseController : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[OrbitalPauseController]";

        #endregion

        #region State

        private readonly List<SplineAnimate> _animators = new();
        private readonly List<PlanetRotation> _rotators = new();
        private bool _isPaused;

        #endregion

        #region Public API

        /// <summary>True while orbits and rotations are paused.</summary>
        public bool IsPaused => _isPaused;

        /// <summary>Toggles between pausing and resuming all orbits and rotations.</summary>
        public void TogglePause()
        {
            EnsureCache();
            if (_isPaused)
                Resume();
            else
                Pause();
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            StartCoroutine(CacheAfterOrbitsReady());
        }

        #endregion

        #region Internals

        private IEnumerator CacheAfterOrbitsReady()
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            CacheAll();
            Debug.Log($"{LOG_TAG} Initialized -- {_animators.Count} animators, {_rotators.Count} rotators found.");
        }

        private void CacheAll()
        {
            _animators.Clear();
            foreach (var anim in FindObjectsByType<SplineAnimate>(FindObjectsSortMode.None))
                if (anim.enabled)
                    _animators.Add(anim);

            _rotators.Clear();
            foreach (var rot in FindObjectsByType<PlanetRotation>(FindObjectsSortMode.None))
                if (rot.enabled)
                    _rotators.Add(rot);
        }

        private void EnsureCache()
        {
            _animators.RemoveAll(a => a == null);
            _rotators.RemoveAll(r => r == null);
            if (_animators.Count == 0 && _rotators.Count == 0)
                CacheAll();
        }

        private void Pause()
        {
            foreach (var anim in _animators)
                if (anim != null) anim.Pause();

            foreach (var rot in _rotators)
                if (rot != null) rot.SetPaused(true);

            _isPaused = true;
            Debug.Log($"{LOG_TAG} Orbits paused.");
        }

        private void Resume()
        {
            foreach (var anim in _animators)
                if (anim != null) anim.Play();

            foreach (var rot in _rotators)
                if (rot != null) rot.SetPaused(false);

            _isPaused = false;
            Debug.Log($"{LOG_TAG} Orbits resumed.");
        }

        #endregion

        #region Validation

        private void ValidateReferences() { }

        #endregion
    }
}
