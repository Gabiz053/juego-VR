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

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }

        #endregion

        #region Internals
        #endregion

        #region Validation

        private void Start()
        {
            ValidateReferences();
        }

        private void ValidateReferences()
        {
            if (rotationAxis == Vector3.zero)
                Debug.LogWarning($"{LOG_TAG} rotationAxis is zero — planet will not rotate.", this);
        }

        #endregion
    }
}
