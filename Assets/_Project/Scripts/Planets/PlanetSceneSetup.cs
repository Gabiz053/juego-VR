using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Drop this MonoBehaviour onto the SceneManager GameObject in each planet scene.
    /// Assign a PlanetConfig asset in the Inspector; on Awake it applies gravity, sky,
    /// directional light, fog, and spawns a teleportable platform.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Planet Scene Setup")]
    public class PlanetSceneSetup : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[PlanetSceneSetup]";

        #endregion

        #region Inspector

        [Header("Config")]
        [Tooltip("Planet configuration asset (e.g. PlanetConfig_Earth).")]
        [SerializeField] private PlanetConfig _config;

        [Header("Platform Settings")]
        [Tooltip("Optional prefab to instantiate as the platform. Leave empty to use a default flat cube.")]
        [SerializeField] private GameObject _platformPrefab;

        [Tooltip("Width and depth of the platform in world units (only used when no prefab is assigned).")]
        [SerializeField] private float _platformSize = 30f;

        [Tooltip("Thickness of the platform cube (only used when no prefab is assigned).")]
        [SerializeField] private float _platformHeight = 1f;

        [Tooltip("Y position of the platform top surface where players stand.")]
        [SerializeField] private float _platformY = 0f;

        [Header("Sun Override (optional)")]
        [Tooltip("Leave empty to find the first Directional Light automatically.")]
        [SerializeField] private Light _sunLight;

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API

        /// <summary>The PlanetConfig asset assigned to this scene setup.</summary>
        public PlanetConfig Config => _config;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogWarning($"{LOG_TAG} No PlanetConfig assigned!", this);
                return;
            }

            ApplyGravity();
            ApplySky();
            ApplySun();
            ApplyFog();
        }

        private void Start()
        {
            ValidateReferences();
            if (_config == null) return;
            SpawnPlatform();
        }

        #endregion

        #region Internals

        private void ApplyGravity()
        {
            Physics.gravity = new Vector3(0f, _config._gravityY, 0f);
            Debug.Log($"{LOG_TAG} Gravity set to {_config._gravityY} m/s².");
        }

        private void ApplySky()
        {
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader == null)
            {
                Debug.LogWarning($"{LOG_TAG} 'Skybox/Procedural' shader not found. " +
                                 "Make sure it is included in Graphics Settings.");
                return;
            }

            Material skyMat = new Material(skyShader)
            {
                name = $"Skybox_{_config._displayNameEn}"
            };

            skyMat.SetColor("_SkyTint",             _config._skyTint);
            skyMat.SetColor("_GroundColor",          _config._skyGroundTint);
            skyMat.SetFloat("_AtmosphereThickness",  _config._atmosphereThickness);
            skyMat.SetFloat("_Exposure",             _config._skyExposure);
            skyMat.SetFloat("_SunDisk", 2f);

            RenderSettings.skybox = skyMat;
            DynamicGI.UpdateEnvironment();

            Debug.Log($"{LOG_TAG} Skybox applied.");
        }

        private void ApplySun()
        {
            if (_sunLight == null)
            {
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
                Debug.LogWarning($"{LOG_TAG} No Directional Light found -- sun colour/intensity not applied.");
                return;
            }

            _sunLight.color     = _config._sunColor;
            _sunLight.intensity = _config._sunIntensity;

            Debug.Log($"{LOG_TAG} Sun: colour={_config._sunColor}, intensity={_config._sunIntensity}.");
        }

        private void ApplyFog()
        {
            RenderSettings.fog        = _config._fogEnabled;
            RenderSettings.fogColor   = _config._fogColor;
            RenderSettings.fogDensity = _config._fogDensity;
            RenderSettings.fogMode    = FogMode.Exponential;

            Debug.Log($"{LOG_TAG} Fog: enabled={_config._fogEnabled}, density={_config._fogDensity}.");
        }

        private void SpawnPlatform()
        {
            float     halfH      = _platformHeight * 0.5f;
            Vector3   spawnPos   = new Vector3(0f, _platformY - halfH, 0f);
            GameObject platform;

            if (_platformPrefab != null)
            {
                platform      = Instantiate(_platformPrefab, spawnPos, Quaternion.identity);
                platform.name = "Platform";
                Debug.Log($"{LOG_TAG} Platform instantiated from prefab '{_platformPrefab.name}'.");
            }
            else
            {
                platform                        = GameObject.CreatePrimitive(PrimitiveType.Cube);
                platform.name                   = "Platform";
                platform.transform.position     = spawnPos;
                platform.transform.localScale   = new Vector3(_platformSize, _platformHeight, _platformSize);

                Renderer rend      = platform.GetComponent<Renderer>();
                Shader   litShader = Shader.Find("Universal Render Pipeline/Lit")
                                  ?? Shader.Find("Standard")
                                  ?? Shader.Find("Diffuse");

                if (litShader != null)
                {
                    Material mat = new Material(litShader) { name = "PlatformMaterial" };
                    mat.color            = _config._platformTint;
                    rend.sharedMaterial  = mat;
                }
                else
                {
                    Debug.LogWarning($"{LOG_TAG} No suitable shader found -- platform will use the default material.");
                }

                Debug.Log($"{LOG_TAG} Platform created -- size={_platformSize}, tint={_config._platformTint}.");
            }

            if (platform.GetComponent<Collider>() == null)
            {
                platform.AddComponent<BoxCollider>();
                Debug.LogWarning($"{LOG_TAG} Platform prefab had no Collider -- added BoxCollider for TeleportationArea.");
            }

            TeleportationArea teleportArea = platform.GetComponent<TeleportationArea>();
            if (teleportArea == null)
                teleportArea = platform.AddComponent<TeleportationArea>();

            teleportArea.interactionLayers = InteractionLayerMask.GetMask("Teleport");

            Debug.Log($"{LOG_TAG} TeleportationArea configured on platform (layer: Teleport only).");
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_config == null)
                Debug.LogWarning($"{LOG_TAG} _config is not assigned.", this);
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Preview Setup (Editor Only)")]
        private void PreviewInEditor()
        {
            if (_config == null) { Debug.LogWarning($"{LOG_TAG} Assign a PlanetConfig first."); return; }
            ApplySky();
            ApplySun();
            ApplyFog();
            Debug.Log($"{LOG_TAG} Preview applied (gravity and platform require Play Mode).");
        }
#endif
    }
}
