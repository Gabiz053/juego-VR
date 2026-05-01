using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Marker component for grabbable rocks in planet scenes.
    /// Requires Rigidbody and XRGrabInteractable on the same GameObject.
    /// The rock will fall according to whatever Physics.gravity is set to
    /// (configured per-planet by LocalGravityModifier or PlanetSceneSetup).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Interaction/Grabbable Rock")]
    public sealed class GrabbableRock : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log($"[GrabbableRock] '{gameObject.name}' ready — gravity: {Physics.gravity.y:F2} m/s².");
        }
    }
}
