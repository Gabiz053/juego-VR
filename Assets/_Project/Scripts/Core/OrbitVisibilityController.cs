using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Shows and hides the orbit lines of all planets in the scene.
    /// Finds all LineRenderer components at Start and toggles them as a group.
    /// Attach to the same GameObject as SolarSystemSceneConnector.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Orbit Visibility Controller")]
    public sealed class OrbitVisibilityController : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[OrbitVisibilityController]";

        #endregion

        #region Inspector
        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region State

        private readonly List<LineRenderer> _lineRenderers = new();
        private bool _isVisible = true;

        #endregion

        #region Public API

        /// <summary>True while orbit lines are visible.</summary>
        public bool IsVisible => _isVisible;

        /// <summary>Toggles between showing and hiding all orbit lines.</summary>
        public void ToggleVisibility()
        {
            EnsureLineRenderersCache();

            if (_isVisible)
                HideOrbits();
            else
                ShowOrbits();
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            CacheLineRenderers();
            Debug.Log($"{LOG_TAG} Initialized -- {_lineRenderers.Count} orbit lines found.");
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

        private void EnsureLineRenderersCache()
        {
            _lineRenderers.RemoveAll(lr => lr == null);

            if (_lineRenderers.Count == 0)
                CacheLineRenderers();
        }

        private void HideOrbits()
        {
            foreach (var lr in _lineRenderers)
                if (lr != null) lr.enabled = false;

            _isVisible = false;
            Debug.Log($"{LOG_TAG} Orbits hidden.");
        }

        private void ShowOrbits()
        {
            foreach (var lr in _lineRenderers)
                if (lr != null) lr.enabled = true;

            _isVisible = true;
            Debug.Log($"{LOG_TAG} Orbits visible.");
        }

        #endregion

        #region Validation

        private void ValidateReferences() { }

        #endregion
    }
}
