using UnityEngine;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Lightweight data component. Add to each planet GameObject and set the scene
    /// name to load when the player points at and selects the planet.
    /// Read by PlanetClickTeleporter.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Interaction/Planet Scene Link")]
    public sealed class PlanetSceneLink : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[PlanetSceneLink]";

        #endregion

        #region Inspector

        [Header("Destination")]
        [Tooltip("Exact scene name as registered in Build Settings (e.g. 'Tierra', 'Marte', 'Jupiter').")]
        [SerializeField] private string _sceneName;

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API

        public string SceneName => _sceneName;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Internals
        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (string.IsNullOrWhiteSpace(_sceneName))
                Debug.LogWarning($"{LOG_TAG} _sceneName is not assigned on '{gameObject.name}'.", this);
        }

        #endregion
    }
}
