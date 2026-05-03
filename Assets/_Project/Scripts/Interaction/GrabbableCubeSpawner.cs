using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Spawns a grabbable, physics-driven object a fixed height above a reference surface.
    ///
    /// HOW TO USE
    /// ──────────
    /// 1. Add this component to any persistent scene GameObject (e.g. the Scene Manager).
    /// 2. Assign a GrabbableCube prefab (or any other prefab) to the _cubePrefab field.
    ///    Leave it empty to fall back to the legacy runtime-built primitive cube.
    /// 3. Tune the Inspector parameters below.
    /// 4. Press Play — the object is created in Start(), inheriting the scene's gravity
    ///    (set by PlanetSceneSetup or LocalGravityModifier).
    ///
    /// SWAPPING THE OBJECT
    /// ───────────────────
    /// Drag any prefab into the "Cube Prefab" slot and it will be instantiated instead
    /// of the default cube. The prefab is responsible for its own components (Rigidbody,
    /// XRGrabInteractable, etc.); only its position is controlled by this spawner.
    ///
    /// REQUIREMENTS
    /// ────────────
    /// • XR Interaction Toolkit must be present in the project (already satisfied).
    /// </summary>
    [AddComponentMenu("ProyectoVR/Interaction/Grabbable Cube Spawner")]
    public sealed class GrabbableCubeSpawner : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        // Inspector — Prefab (primary path)
        // ------------------------------------------------------------------ //

        [Header("Cube Prefab")]
        [Tooltip("Prefab to instantiate as the grabbable object. " +
                 "Swap this to change what appears in the scene. " +
                 "Leave empty to use the legacy runtime-built primitive cube.")]
        [SerializeField] private GameObject _cubePrefab;

        // ------------------------------------------------------------------ //
        // Inspector — Cube shape & appearance (legacy / fallback only)
        // ------------------------------------------------------------------ //

        [Header("Cube Shape (fallback — used only when no prefab is assigned)")]
        [Tooltip("Side length of the cube in world units.")]
        [SerializeField] private float _cubeSize = 0.3f;

        [Tooltip("Optional material to apply to the cube. Leave empty to use a default colour.")]
        [SerializeField] private Material _cubeMaterial;

        [Tooltip("Fallback colour used when no material is assigned.")]
        [SerializeField] private Color _cubeColor = new Color(0.2f, 0.6f, 1f);

        // ------------------------------------------------------------------ //
        // Inspector — Spawn position
        // ------------------------------------------------------------------ //

        [Header("Spawn Position")]
        [Tooltip("World-space Y of the surface the cube sits above (matches PlanetSceneSetup._platformY).")]
        [SerializeField] private float _surfaceY = 0f;

        [Tooltip("How many units above the surface the cube centre is placed.")]
        [SerializeField] [Min(0f)] private float _heightAboveSurface = 1.5f;

        [Tooltip("Horizontal offset from the world origin so the cube doesn't spawn inside the player.")]
        [SerializeField] private Vector3 _horizontalOffset = new Vector3(0f, 0f, 1f);

        // ------------------------------------------------------------------ //
        // Inspector — Physics
        // ------------------------------------------------------------------ //

        [Header("Physics")]
        [Tooltip("Mass of the Rigidbody in kg.")]
        [SerializeField] [Min(0.01f)] private float _mass = 1f;

        [Tooltip("Linear drag applied to the Rigidbody.")]
        [SerializeField] [Min(0f)] private float _drag = 0f;

        [Tooltip("Angular drag applied to the Rigidbody.")]
        [SerializeField] [Min(0f)] private float _angularDrag = 0.05f;

        // ------------------------------------------------------------------ //
        // Inspector — XR Interaction
        // ------------------------------------------------------------------ //

        [Header("XR Interaction")]
        [Tooltip("When enabled the cube snaps back to its original scale after being released.")]
        [SerializeField] private bool _throwOnRelease = true;

        // ------------------------------------------------------------------ //
        // Runtime reference
        // ------------------------------------------------------------------ //

        private GameObject _spawnedCube;

        // ------------------------------------------------------------------ //
        // Unity lifecycle
        // ------------------------------------------------------------------ //

        private void Start()
        {
            SpawnCube();
        }

        // ------------------------------------------------------------------ //
        // Core logic
        // ------------------------------------------------------------------ //

        private void SpawnCube()
        {
            // ── Shared spawn position ─────────────────────────────────────
            float spawnY = _surfaceY + _heightAboveSurface + _cubeSize * 0.5f;
            Vector3 spawnPos = new Vector3(
                _horizontalOffset.x,
                spawnY + _horizontalOffset.y,
                _horizontalOffset.z);

            if (_cubePrefab != null)
            {
                // ── Prefab path ───────────────────────────────────────────
                // Instantiate the assigned prefab; it carries its own components
                // (Rigidbody, XRGrabInteractable, etc.) — we only set position.
                _spawnedCube = Instantiate(_cubePrefab, spawnPos, Quaternion.identity);
                _spawnedCube.name = "GrabbableCube";

                Debug.Log($"[GrabbableCubeSpawner] Prefab '{_cubePrefab.name}' spawned at y={spawnY:F2} " +
                          $"(surface={_surfaceY:F2} + offset={_heightAboveSurface:F2}), " +
                          $"gravity={Physics.gravity.y:F2} m/s².");
            }
            else
            {
                // ── Legacy fallback: build a cube from scratch ────────────
                _spawnedCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _spawnedCube.name = "GrabbableCube";

                // Position
                _spawnedCube.transform.position = spawnPos;

                // Scale
                _spawnedCube.transform.localScale = Vector3.one * _cubeSize;

                // Material / colour
                Renderer rend = _spawnedCube.GetComponent<Renderer>();
                if (_cubeMaterial != null)
                {
                    rend.sharedMaterial = _cubeMaterial;
                }
                else
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                                 ?? Shader.Find("Standard");
                    if (shader != null)
                    {
                        Material mat = new Material(shader) { name = "GrabbableCubeMat" };
                        mat.color = _cubeColor;
                        rend.sharedMaterial = mat;
                    }
                }

                // Rigidbody — gravity enabled (falls according to Physics.gravity set per planet)
                Rigidbody rb = _spawnedCube.AddComponent<Rigidbody>();
                rb.mass            = _mass;
                rb.linearDamping   = _drag;
                rb.angularDamping  = _angularDrag;
                rb.useGravity      = true;
                rb.isKinematic     = false;

                // XRGrabInteractable — VR hand grabbing
                XRGrabInteractable grab = _spawnedCube.AddComponent<XRGrabInteractable>();
                grab.throwOnDetach = _throwOnRelease;

                Debug.Log($"[GrabbableCubeSpawner] Primitive cube spawned at y={spawnY:F2} " +
                          $"(surface={_surfaceY:F2} + offset={_heightAboveSurface:F2}), " +
                          $"gravity={Physics.gravity.y:F2} m/s².");
            }
        }

        // ------------------------------------------------------------------ //
        // Editor helper
        // ------------------------------------------------------------------ //

#if UNITY_EDITOR
        [ContextMenu("Respawn Cube (Editor Play Mode)")]
        private void RespawnCube()
        {
            if (_spawnedCube != null)
            {
                DestroyImmediate(_spawnedCube);
                _spawnedCube = null;
            }
            SpawnCube();
        }
#endif
    }
}
