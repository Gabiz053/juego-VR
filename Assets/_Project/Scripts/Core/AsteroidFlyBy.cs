using System;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Moves a single asteroid in a straight line and tumbles it for an organic look.
    /// Created and configured by <see cref="SpaceAmbientController"/> after instantiation.
    /// Destroys itself after its lifetime expires and notifies the spawner.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/AsteroidFlyBy")]
    public sealed class AsteroidFlyBy : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[AsteroidFlyBy]";

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Tumble")]
        [Tooltip("Rotation speed in degrees per second on each axis. Gives an organic tumbling look.")]
        [SerializeField] private Vector3 _tumbleSpeed = new(18f, 7f, 13f);

        #endregion

        #region Events ----------------------------------------------------------

        /// <summary>Raised just before this asteroid destroys itself. Used by the spawner to track counts.</summary>
        public event Action OnExpired;

        #endregion

        #region Cached Components -----------------------------------------------

        private Vector3 _velocity;
        private float _lifetime;
        private float _elapsed;
        private bool _initialized;

        #endregion

        #region Public API ------------------------------------------------------

        /// <summary>
        /// Called by <see cref="SpaceAmbientController"/> immediately after instantiation.
        /// Must be called before the first Update frame.
        /// </summary>
        public void Initialize(Vector3 velocity, float lifetime)
        {
            _velocity    = velocity;
            _lifetime    = lifetime;
            _initialized = true;
        }

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            ValidateReferences();
        }

        private void Update()
        {
            if (!_initialized) return;

            transform.Translate(_velocity * Time.deltaTime, Space.World);
            transform.Rotate(_tumbleSpeed * Time.deltaTime, Space.Self);

            _elapsed += Time.deltaTime;
            if (_elapsed >= _lifetime)
                Expire();
        }

        #endregion

        #region Internals -------------------------------------------------------

        private void Expire()
        {
            OnExpired?.Invoke();
            Destroy(gameObject);
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (!_initialized)
                Debug.LogWarning($"{LOG_TAG} Initialize() was not called before Start.", this);
        }

        #endregion
    }
}
