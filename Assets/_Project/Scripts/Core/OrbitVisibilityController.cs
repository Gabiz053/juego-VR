using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Muestra y oculta las lineas de orbita de todos los planetas de la escena.
    /// Busca todos los LineRenderer activos y los activa/desactiva en bloque.
    /// Adjuntar al mismo GameObject que SolarSystemSceneConnector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Orbit Visibility Controller")]
    public sealed class OrbitVisibilityController : MonoBehaviour
    {
        #region State

        private readonly List<LineRenderer> _lineRenderers = new();
        private bool _isVisible = true;

        #endregion

        #region Public API

        /// <summary>Indica si las lineas de orbita estan visibles.</summary>
        public bool IsVisible => _isVisible;

        /// <summary>Alterna entre mostrar y ocultar todas las lineas de orbita.</summary>
        public void ToggleVisibility()
        {
            if (_isVisible)
                HideOrbits();
            else
                ShowOrbits();
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            CacheLineRenderers();
            Debug.Log($"[OrbitVisibilityController] Initialized -- {_lineRenderers.Count} orbit lines found.");
        }

        #endregion

        #region Internals

        private void CacheLineRenderers()
        {
            _lineRenderers.Clear();
            var found = FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
            foreach (var lr in found)
                _lineRenderers.Add(lr);
        }

        private void HideOrbits()
        {
            foreach (var lr in _lineRenderers)
                if (lr != null) lr.enabled = false;

            _isVisible = false;
            Debug.Log("[OrbitVisibilityController] Orbits hidden.");
        }

        private void ShowOrbits()
        {
            foreach (var lr in _lineRenderers)
                if (lr != null) lr.enabled = true;

            _isVisible = true;
            Debug.Log("[OrbitVisibilityController] Orbits visible.");
        }

        #endregion
    }
}