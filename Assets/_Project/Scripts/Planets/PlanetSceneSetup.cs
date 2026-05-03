using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Drop this MonoBehaviour onto a Scene Manager GameObject.
/// Assign the PlanetConfig asset in the Inspector and it will:
///   1. Set gravity from the config.
///   2. Paint the sky using Unity's built-in Procedural Skybox.
///   3. Colour and set the intensity of the scene's Directional Light (sun).
///   4. Enable / disable fog and apply fog colour + density.
///   5. Spawn a platform (prefab or default cube) with a TeleportationArea.
/// </summary>
[DisallowMultipleComponent]
public class PlanetSceneSetup : MonoBehaviour
{
    [Tooltip("Planet configuration asset (e.g. PlanetConfig_Earth)")]
    [SerializeField] private PlanetConfig _config;

    [Header("Platform settings")]
    [Tooltip("Optional prefab to instantiate as the platform. Leave empty to use a default flat cube.")]
    [SerializeField] private GameObject _platformPrefab;
    [Tooltip("Width and depth of the platform in world units (only used when no prefab is assigned)")]
    [SerializeField] private float _platformSize   = 30f;
    [Tooltip("Thickness of the platform cube (only used when no prefab is assigned)")]
    [SerializeField] private float _platformHeight = 1f;
    [Tooltip("Y position of the platform top surface (players stand here)")]
    [SerializeField] private float _platformY      = 0f;

    [Header("Sun override (optional)")]
    [Tooltip("Leave empty to find the first Directional Light automatically")]
    [SerializeField] private Light _sunLight;

    // ------------------------------------------------------------------ //

    /// <summary>The PlanetConfig asset assigned to this scene setup.</summary>
    public PlanetConfig Config => _config;

    // ------------------------------------------------------------------ //

    private void Awake()
    {
        if (_config == null)
        {
            Debug.LogError("[PlanetSceneSetup] No PlanetConfig assigned!", this);
            return;
        }

        ApplyGravity();
        ApplySky();
        ApplySun();
        ApplyFog();
    }

    private void Start()
    {
        if (_config == null) return;
        SpawnPlatform();
    }

    // ------------------------------------------------------------------ //
    // Gravity
    // ------------------------------------------------------------------ //

    private void ApplyGravity()
    {
        Physics.gravity = new Vector3(0f, _config._gravityY, 0f);
        Debug.Log($"[PlanetSceneSetup] Gravity set to {_config._gravityY} m/s²");
    }

    // ------------------------------------------------------------------ //
    // Sky  (Unity built-in Procedural Skybox)
    // ------------------------------------------------------------------ //

    private void ApplySky()
    {
        // Create a new instance so we don't permanently mutate a shared asset.
        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader == null)
        {
            Debug.LogWarning("[PlanetSceneSetup] 'Skybox/Procedural' shader not found. " +
                             "Make sure it is included in Graphics Settings.");
            return;
        }
        Material skyMat = new Material(skyShader);

        skyMat.name = $"Skybox_{_config._displayNameEn}";

        // Map PlanetConfig fields → Procedural Skybox shader properties
        skyMat.SetColor("_SkyTint",    _config._skyTint);
        skyMat.SetColor("_GroundColor", _config._skyGroundTint);
        skyMat.SetFloat("_AtmosphereThickness", _config._atmosphereThickness);
        skyMat.SetFloat("_Exposure",            _config._skyExposure);

        // Keep the sun disc visible (mode 2 = High Quality)
        skyMat.SetFloat("_SunDisk", 2f);

        RenderSettings.skybox = skyMat;

        // Request an ambient light refresh so IBL probes update immediately.
        DynamicGI.UpdateEnvironment();

        Debug.Log("[PlanetSceneSetup] Skybox applied.");
    }

    // ------------------------------------------------------------------ //
    // Sun (Directional Light)
    // ------------------------------------------------------------------ //

    private void ApplySun()
    {
        if (_sunLight == null)
        {
            // Try to locate the scene's directional light automatically.
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    _sunLight = l;
                    break;
                }
            }
        }

        if (_sunLight == null)
        {
            Debug.LogWarning("[PlanetSceneSetup] No Directional Light found in the scene. " +
                             "Sun colour/intensity not applied.");
            return;
        }

        _sunLight.color     = _config._sunColor;
        _sunLight.intensity = _config._sunIntensity;

        Debug.Log($"[PlanetSceneSetup] Sun: colour={_config._sunColor}, intensity={_config._sunIntensity}");
    }

    // ------------------------------------------------------------------ //
    // Fog
    // ------------------------------------------------------------------ //

    private void ApplyFog()
    {
        RenderSettings.fog         = _config._fogEnabled;
        RenderSettings.fogColor    = _config._fogColor;
        RenderSettings.fogDensity  = _config._fogDensity;
        RenderSettings.fogMode     = FogMode.Exponential;

        Debug.Log($"[PlanetSceneSetup] Fog: enabled={_config._fogEnabled}, density={_config._fogDensity}");
    }

    // ------------------------------------------------------------------ //
    // Platform
    // ------------------------------------------------------------------ //

    private void SpawnPlatform()
    {
        GameObject platform;
        float halfH = _platformHeight * 0.5f;
        Vector3 spawnPos = new Vector3(0f, _platformY - halfH, 0f);

        if (_platformPrefab != null)
        {
            // ── Prefab path ──────────────────────────────────────────────
            platform = Instantiate(_platformPrefab, spawnPos, Quaternion.identity);
            platform.name = "Platform";
            Debug.Log($"[PlanetSceneSetup] Platform instantiated from prefab '{_platformPrefab.name}'.");
        }
        else
        {
            // ── Default cube path ─────────────────────────────────────────
            platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Platform";
            platform.transform.position   = spawnPos;
            platform.transform.localScale = new Vector3(_platformSize, _platformHeight, _platformSize);

            // Tint the cube with a planet-specific material.
            Renderer rend = platform.GetComponent<Renderer>();

            // Check shaders before constructing Material — Shader.Find returns null
            // when the shader is not available, and new Material(null) throws.
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard")
                            ?? Shader.Find("Diffuse");

            if (litShader != null)
            {
                Material mat = new Material(litShader) { name = "PlatformMaterial" };
                mat.color = _config._platformTint;
                rend.sharedMaterial = mat;
            }
            else
            {
                Debug.LogWarning("[PlanetSceneSetup] No suitable shader found — platform will use the default material.");
            }

            Debug.Log($"[PlanetSceneSetup] Platform created — size={_platformSize}, tint={_config._platformTint}");
        }

        // ── TeleportationArea ─────────────────────────────────────────────
        // Requires a Collider. Primitive cubes have one; warn if a prefab doesn't.
        if (platform.GetComponent<Collider>() == null)
        {
            platform.AddComponent<BoxCollider>();
            Debug.LogWarning("[PlanetSceneSetup] Platform prefab had no Collider — added BoxCollider for TeleportationArea.");
        }

        TeleportationArea teleportArea = platform.GetComponent<TeleportationArea>();
        if (teleportArea == null)
            teleportArea = platform.AddComponent<TeleportationArea>();

        // The Teleport Interactor in the XR Rig requires the "Teleport" XR Interaction
        // Layer. Setting it explicitly here ensures it works whether the TeleportationArea
        // came from the prefab or was just added — a mismatch is what causes the red ray.
        // Set all interaction layer bits so the platform accepts any interactor,
        // including the Teleport Interactor which uses the custom "Teleport" XR layer.
        var iLayers = teleportArea.interactionLayers;
        iLayers.value = ~0;
        teleportArea.interactionLayers = iLayers;

        Debug.Log("[PlanetSceneSetup] TeleportationArea configured on platform (layer: Teleport).");
    }

#if UNITY_EDITOR
    // ------------------------------------------------------------------ //
    // Editor helper: preview the setup without entering Play Mode.
    // ------------------------------------------------------------------ //

    [ContextMenu("Preview Setup (Editor Only)")]
    private void PreviewInEditor()
    {
        if (_config == null) { Debug.LogError("Assign a PlanetConfig first."); return; }
        ApplySky();
        ApplySun();
        ApplyFog();
        Debug.Log("[PlanetSceneSetup] Preview applied (gravity & platform require Play Mode).");
    }
#endif
}