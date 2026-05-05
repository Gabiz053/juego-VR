using UnityEngine;
using _Project.Scripts.Core;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Plays a random impact sound through AudioManager when this Rigidbody collides
    /// with another object fast enough. Attach to any physics-driven object.
    /// GrabbableCubeSpawner adds this automatically to spawned cubes.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Interaction/Physics Impact Sound Emitter")]
    public sealed class PhysicsImpactSoundEmitter : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[PhysicsImpactSoundEmitter]";

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Impact Settings")]
        [Tooltip("Minimum relative collision speed (m/s) required to trigger a sound. Prevents sounds from gentle resting contacts.")]
        [SerializeField, Min(0f)] private float _minVelocity = 0.4f;

        [Tooltip("Minimum seconds between consecutive impact sounds on this object. Prevents rapid-fire spam.")]
        [SerializeField, Range(0f, 2f)] private float _cooldown = 0.25f;

        #endregion

        #region Cached Components -----------------------------------------------

        private float _lastImpactTime = -999f;

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            ValidateReferences();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (AudioManager.Instance == null) return;
            if (Time.time - _lastImpactTime < _cooldown) return;
            if (collision.relativeVelocity.magnitude < _minVelocity) return;

            _lastImpactTime = Time.time;

            Vector3 contactPoint = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.position;

            AudioManager.Instance.PlayImpactSound(contactPoint);
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (GetComponent<Rigidbody>() == null)
                Debug.LogWarning($"{LOG_TAG} No Rigidbody on '{name}' -- OnCollisionEnter will never fire.", this);
        }

        #endregion
    }
}
