using UnityEngine;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Spins a planet continuously around a configurable axis at a constant speed.
    /// Attach to any planet GameObject in the SolarSystem or planet surface scenes.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Planet Rotation")]
    public class PlanetRotation : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[PlanetRotation]";

        #endregion

        #region Inspector

        [Header("Rotation")]
        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField] private float rotationSpeed = 30f;

        [Tooltip("Local axis around which the planet rotates.")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        #endregion

        #region Cached Components

        private bool _hasValidAxis;
        private Vector3 _normalizedAxis;

        #endregion

        #region State

        private bool _isPaused;

        #endregion

        #region Public API

        /// <summary>Pauses or resumes the self-rotation of this planet.</summary>
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            _hasValidAxis = rotationAxis.sqrMagnitude > 0f;
            if (_hasValidAxis)
                _normalizedAxis = rotationAxis.normalized;
        }

        private void Update()
        {
            if (_isPaused || !_hasValidAxis || Mathf.Approximately(rotationSpeed, 0f))
                return;

            transform.Rotate(_normalizedAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (rotationAxis == Vector3.zero)
                Debug.LogWarning($"{LOG_TAG} rotationAxis is zero — planet will not rotate.", this);
        }

        #endregion
    }
}
