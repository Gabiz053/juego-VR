using UnityEngine;

/// <summary>
/// ScriptableObject that holds all per-planet environment parameters.
/// Create via: Assets > Create > VR Game > Planet Config
/// </summary>
[CreateAssetMenu(fileName = "PlanetConfig", menuName = "VR Game/Planet Config")]
public class PlanetConfig : ScriptableObject
{
    [Header("Identity")]
    public string _displayNameEs;
    public string _displayNameEn;
    public string _sceneName;

    [Header("Physics")]
    public float _gravityY = -9.81f;

    [Header("Sky")]
    public Color _skyTint       = new Color(0.3f,  0.5f,  0.9f, 1f);
    public Color _skyGroundTint  = new Color(0.15f, 0.22f, 0.1f, 1f);
    [Range(0f, 5f)] public float _atmosphereThickness = 1.0f;
    [Range(0f, 8f)] public float _skyExposure         = 1.3f;

    [Header("Sun / Directional Light")]
    public Color _sunColor     = new Color(1f, 0.96f, 0.84f, 1f);
    public float _sunIntensity = 1.1f;

    [Header("Fog")]
    public bool  _fogEnabled = false;
    public Color _fogColor   = new Color(0.5f, 0.5f, 0.6f, 1f);
    public float _fogDensity = 0.006f;

    [Header("Platform")]
    public Color _platformTint = new Color(0.55f, 0.55f, 0.6f, 1f);

    [Header("Scenery Rocks")]
    public Color   _rockTint           = new Color(0.45f, 0.4f, 0.35f, 1f);
    public int     _sceneryRockCount   = 90;
    public float   _sceneryInnerRadius = 14f;
    public float   _sceneryOuterRadius = 70f;
    public Vector2 _sceneryScaleRange  = new Vector2(0.6f, 3.5f);
    public int     _sceneryRandomSeed  = 110;
}