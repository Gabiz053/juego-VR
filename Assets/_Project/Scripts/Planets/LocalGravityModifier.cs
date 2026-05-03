using UnityEngine;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Applies a planet-specific gravitational acceleration to the scene and restores
    /// the default value when the scene is unloaded.
    /// If both _config and _gravityOverride are set, _config takes precedence.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Local Gravity Modifier")]
    public sealed class LocalGravityModifier : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[LocalGravityModifier]";

        #endregion

        #region Inspector

        [Header("Gravity Source")]
        [Tooltip("Planet config asset. When assigned, _gravityOverride is ignored.")]
        [SerializeField] private PlanetConfig _config;

        [Tooltip("Direct gravity value in m/s² used only when _config is not assigned.")]
        [SerializeField] private float _gravityOverride = -9.81f;

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _previousGravity = Physics.gravity;
            ApplyGravity();
        }

        private void Start()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            Physics.gravity = _previousGravity;
            Debug.Log($"{LOG_TAG} Gravity restored to {_previousGravity.y:F2} m/s².");
        }

        #endregion

        #region Internals

        private Vector3 _previousGravity;

        private void ApplyGravity()
        {
            float gravityY  = _config != null ? _config._gravityY : _gravityOverride;
            Physics.gravity = new Vector3(0f, gravityY, 0f);
            Debug.Log($"{LOG_TAG} Gravity set to {gravityY:F2} m/s².");
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_config == null)
                Debug.LogWarning($"{LOG_TAG} _config is not assigned -- using _gravityOverride ({_gravityOverride:F2} m/s²).", this);
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Preview Gravity Value")]
        private void PreviewGravityValue()
        {
            float gravityY = _config != null ? _config._gravityY : _gravityOverride;
            Debug.Log($"{LOG_TAG} Would apply gravity: {gravityY:F2} m/s².");
        }
#endif
    }
}
