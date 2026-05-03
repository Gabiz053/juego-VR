using UnityEngine;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// ScriptableObject that holds all per-planet environment parameters.
    /// Create via: Assets > Create > ProyectoVR > Planets > Planet Config
    /// </summary>
    [CreateAssetMenu(fileName = "PlanetConfig", menuName = "ProyectoVR/Planets/Planet Config")]
    public class PlanetConfig : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Planet name in Spanish (used in the gravity HUD and UI labels).")]
        public string _displayNameEs;

        [Tooltip("Planet name in English (used in code logs and scene names).")]
        public string _displayNameEn;

        [Tooltip("Exact Unity scene name to load for this planet's surface.")]
        public string _sceneName;

        [Header("Physics")]
        [Tooltip("Surface gravity in m/s² (negative = downward). Earth = -9.81.")]
        public float _gravityY = -9.81f;

        [Header("Sky")]
        [Tooltip("Base sky colour tint for the Procedural Skybox.")]
        public Color _skyTint = new Color(0.3f, 0.5f, 0.9f, 1f);

        [Tooltip("Ground colour for the Procedural Skybox horizon.")]
        public Color _skyGroundTint = new Color(0.15f, 0.22f, 0.1f, 1f);

        [Tooltip("Atmosphere density (0 = no atmosphere, 5 = very thick).")]
        [Range(0f, 5f)] public float _atmosphereThickness = 1.0f;

        [Tooltip("Sky brightness multiplier.")]
        [Range(0f, 8f)] public float _skyExposure = 1.3f;

        [Header("Sun / Directional Light")]
        [Tooltip("Colour of the scene's main directional light (sun).")]
        public Color _sunColor = new Color(1f, 0.96f, 0.84f, 1f);

        [Tooltip("Intensity of the scene's main directional light.")]
        public float _sunIntensity = 1.1f;

        [Header("Fog")]
        [Tooltip("Enable or disable fog in this planet's scene.")]
        public bool _fogEnabled = false;

        [Tooltip("Fog colour.")]
        public Color _fogColor = new Color(0.5f, 0.5f, 0.6f, 1f);

        [Tooltip("Exponential fog density. Higher = thicker fog.")]
        public float _fogDensity = 0.006f;

        [Header("Platform")]
        [Tooltip("Colour tint applied to the landing platform surface.")]
        public Color _platformTint = new Color(0.55f, 0.55f, 0.6f, 1f);

        [Header("Scenery Rocks")]
        [Tooltip("Colour tint applied to the scattered scenery rocks.")]
        public Color _rockTint = new Color(0.45f, 0.4f, 0.35f, 1f);

        [Tooltip("Number of scenery rocks spawned around the platform.")]
        public int _sceneryRockCount = 90;

        [Tooltip("Minimum spawn distance from the platform centre.")]
        public float _sceneryInnerRadius = 14f;

        [Tooltip("Maximum spawn distance from the platform centre.")]
        public float _sceneryOuterRadius = 70f;

        [Tooltip("Min and max scale of each scenery rock (X = min, Y = max).")]
        public Vector2 _sceneryScaleRange = new Vector2(0.6f, 3.5f);

        [Tooltip("Random seed for deterministic rock placement.")]
        public int _sceneryRandomSeed = 110;
    }
}
