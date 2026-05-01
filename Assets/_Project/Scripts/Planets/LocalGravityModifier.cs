using UnityEngine;

/// <summary>
/// Applies a planet-specific gravitational acceleration to the scene and restores
/// the default value when the scene is unloaded.
///
/// Usage: Add to any persistent scene GameObject alongside a PlanetConfig reference.
/// If both _config and _gravityOverride are set, _config takes precedence.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("ProyectoVR/Planets/Local Gravity Modifier")]
public sealed class LocalGravityModifier : MonoBehaviour
{
    #region Inspector

    [Header("Gravity Source")]
    [Tooltip("Planet config asset. When assigned, _gravityOverride is ignored.")]
    [SerializeField] private PlanetConfig _config;

    [Tooltip("Direct gravity value (m/s²) used only when _config is not set.")]
    [SerializeField] private float _gravityOverride = -9.81f;

    #endregion

    #region State

    private Vector3 _previousGravity;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        _previousGravity = Physics.gravity;
        ApplyGravity();
    }

    private void OnDestroy()
    {
        Physics.gravity = _previousGravity;
        Debug.Log($"[LocalGravityModifier] Gravity restored to {_previousGravity.y:F2} m/s².");
    }

    #endregion

    #region Internals

    private void ApplyGravity()
    {
        float gravityY = _config != null ? _config._gravityY : _gravityOverride;
        Physics.gravity = new Vector3(0f, gravityY, 0f);
        Debug.Log($"[LocalGravityModifier] Gravity set to {gravityY:F2} m/s².");
    }

    #endregion

#if UNITY_EDITOR
    [ContextMenu("Preview Gravity Value")]
    private void PreviewGravityValue()
    {
        float gravityY = _config != null ? _config._gravityY : _gravityOverride;
        Debug.Log($"[LocalGravityModifier] Would apply gravity: {gravityY:F2} m/s²");
    }
#endif
}
