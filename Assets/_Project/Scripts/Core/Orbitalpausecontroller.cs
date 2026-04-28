using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Pausa y reanuda el movimiento orbital de todos los planetas de la escena.
    /// Busca todos los SplineAnimate activos y los controla en bloque.
    /// Adjuntar al mismo GameObject que SolarSystemSceneConnector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Orbital Pause Controller")]
    public sealed class OrbitalPauseController : MonoBehaviour
    {
        #region State

        private readonly List<SplineAnimate> _animators = new();
        private bool _isPaused;

        #endregion

        #region Public API

        /// <summary>Indica si las orbitas estan pausadas.</summary>
        public bool IsPaused => _isPaused;

        /// <summary>Alterna entre pausar y reanudar todas las orbitas.</summary>
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
            CacheAnimators();
            Debug.Log($"[OrbitalPauseController] Initialized -- {_animators.Count} orbital animators found.");
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
            Debug.Log("[OrbitalPauseController] Orbits paused.");
        }

        private void ResumeOrbits()
        {
            foreach (var anim in _animators)
                if (anim != null) anim.Play();

            _isPaused = false;
            Debug.Log("[OrbitalPauseController] Orbits resumed.");
        }

        #endregion
    }
}