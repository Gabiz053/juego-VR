using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Continuously rotates a GameObject on all three axes.
    /// Attach to the portal visual mesh (the galaxy sphere) to make it spin in place.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/PortalRotator")]
    public sealed class PortalRotator : MonoBehaviour
    {
        #region Constants -------------------------------------------------------
        // No constants.
        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Rotation")]
        [Tooltip("Rotation speed in degrees per second per axis. Tweak to get a slow mesmerising tumble.")]
        [SerializeField] private Vector3 _rotationSpeed = new(8f, 22f, 5f);

        [Tooltip("Space in which the rotation is applied.")]
        [SerializeField] private Space _rotationSpace = Space.Self;

        #endregion

        #region Events ----------------------------------------------------------
        // No events.
        #endregion

        #region Cached Components -----------------------------------------------
        // No cached components.
        #endregion

        #region Public API ------------------------------------------------------
        // No public API.
        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            ValidateReferences();
        }

        private void Update()
        {
            transform.Rotate(_rotationSpeed * Time.deltaTime, _rotationSpace);
        }

        #endregion

        #region Internals -------------------------------------------------------
        // No internals.
        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            // No serialized references to validate.
        }

        #endregion
    }
}
