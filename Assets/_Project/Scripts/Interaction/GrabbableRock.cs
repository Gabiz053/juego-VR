using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Marker component for grabbable rocks in planet scenes.
    /// Requires Rigidbody and XRGrabInteractable on the same GameObject.
    /// The rock falls according to Physics.gravity set per-planet by LocalGravityModifier.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    [AddComponentMenu("ProyectoVR/Interaction/Grabbable Rock")]
    public sealed class GrabbableRock : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[GrabbableRock]";

        #endregion

        #region Inspector
        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            Debug.Log($"{LOG_TAG} '{gameObject.name}' ready -- gravity: {Physics.gravity.y:F2} m/s².");
        }

        #endregion

        #region Internals
        #endregion

        #region Validation

        private void ValidateReferences() { }

        #endregion
    }
}
