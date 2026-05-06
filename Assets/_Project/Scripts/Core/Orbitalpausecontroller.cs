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

        private readonly struct RigidbodyPauseState
        {
            public readonly bool IsKinematic;
            public readonly bool DetectCollisions;

            public RigidbodyPauseState(bool isKinematic, bool detectCollisions)
            {
                IsKinematic = isKinematic;
                DetectCollisions = detectCollisions;
            }
        }

        private readonly List<SplineAnimate> _animators = new();
        private readonly List<PlanetRotation> _rotators = new();
        private readonly List<KeplerLabOrbiter> _keplerOrbiters = new();
        private readonly List<OrbitalLauncher> _orbitalLaunchers = new();
        private readonly Dictionary<Rigidbody, RigidbodyPauseState> _pausedRigidbodies = new();
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

            _keplerOrbiters.Clear();
            foreach (var orbiter in FindObjectsByType<KeplerLabOrbiter>(FindObjectsSortMode.None))
                if (orbiter.enabled)
                    _keplerOrbiters.Add(orbiter);

            _orbitalLaunchers.Clear();
            foreach (var launcher in FindObjectsByType<OrbitalLauncher>(FindObjectsSortMode.None))
                if (launcher.enabled)
                    _orbitalLaunchers.Add(launcher);
        }

        private void EnsureCache()
        {
            // Rebuild on every toggle so dynamically spawned Kepler planets are included.
            CacheAll();
        }

        private void Pause()
        {
            foreach (var anim in _animators)
                if (anim != null) anim.Pause();

            foreach (var rot in _rotators)
                if (rot != null) rot.SetPaused(true);

            foreach (var orbiter in _keplerOrbiters)
                if (orbiter != null) orbiter.Pause();

            FreezeLauncherRigidbodies();

            _isPaused = true;
            Debug.Log($"{LOG_TAG} Orbits paused.");
        }

        private void Resume()
        {
            foreach (var anim in _animators)
                if (anim != null) anim.Play();

            foreach (var rot in _rotators)
                if (rot != null) rot.SetPaused(false);

            foreach (var orbiter in _keplerOrbiters)
                if (orbiter != null) orbiter.Resume();

            RestoreLauncherRigidbodies();

            _isPaused = false;
            Debug.Log($"{LOG_TAG} Orbits resumed.");
        }

        private void FreezeLauncherRigidbodies()
        {
            for (int i = 0; i < _orbitalLaunchers.Count; i++)
            {
                OrbitalLauncher launcher = _orbitalLaunchers[i];
                if (launcher == null)
                    continue;

                Rigidbody rb = launcher.GetComponent<Rigidbody>();
                if (rb == null)
                    continue;

                if (!_pausedRigidbodies.ContainsKey(rb))
                    _pausedRigidbodies.Add(rb, new RigidbodyPauseState(rb.isKinematic, rb.detectCollisions));

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.detectCollisions = false;
                rb.isKinematic = true;
            }
        }

        private void RestoreLauncherRigidbodies()
        {
            foreach (var entry in _pausedRigidbodies)
            {
                Rigidbody rb = entry.Key;
                if (rb == null)
                    continue;

                RigidbodyPauseState cachedState = entry.Value;
                rb.isKinematic = cachedState.IsKinematic;
                rb.detectCollisions = cachedState.DetectCollisions;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                if (!rb.isKinematic)
                    rb.WakeUp();
            }

            _pausedRigidbodies.Clear();
        }

        #endregion

        #region Validation

        private void ValidateReferences() { }

        #endregion
    }
}
