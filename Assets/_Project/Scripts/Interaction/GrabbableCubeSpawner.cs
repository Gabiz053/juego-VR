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
    /// of the default cube. The prefab defines the visual appearance (mesh, material,
    /// scale, collider). Physics (Rigidbody) and grab interaction (XRGrabInteractable)
    /// are always added programmatically so grabbing is guaranteed to work.
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
        // Inspector — Out-of-bounds respawn
        // ------------------------------------------------------------------ //

        [Header("Out-of-Bounds Respawn")]
        [Tooltip("If the cube's Y position drops below this value it is teleported back to its spawn position.")]
        [SerializeField] private float _fallThreshold = -10f;

        // ------------------------------------------------------------------ //
        // Runtime references
        // ------------------------------------------------------------------ //

        private GameObject _spawnedCube;
        private Vector3    _spawnPosition;

        // ------------------------------------------------------------------ //
        // Unity lifecycle
        // ------------------------------------------------------------------ //

        private void Start()
        {
            SpawnCube();
        }

        private void Update()
        {
            if (_spawnedCube == null) return;

            if (_spawnedCube.transform.position.y <= _fallThreshold)
                RespawnToOrigin();
        }

        // ------------------------------------------------------------------ //
        // Core logic
        // ------------------------------------------------------------------ //

        private void SpawnCube()
        {
            // ── Shared spawn position ─────────────────────────────────────
            float spawnY = _surfaceY + _heightAboveSurface + _cubeSize * 0.5f;
            _spawnPosition = new Vector3(
                _horizontalOffset.x,
                spawnY + _horizontalOffset.y,
                _horizontalOffset.z);
            Vector3 spawnPos = _spawnPosition;

            if (_cubePrefab != null)
            {
                // ── Prefab path ───────────────────────────────────────────
                // Instantiate the prefab for its visual/collider components,
                // then add Rigidbody and XRGrabInteractable programmatically
                // so grab interaction is guaranteed to initialise correctly.
                _spawnedCube = Instantiate(_cubePrefab, spawnPos, Quaternion.identity);
                _spawnedCube.name = "GrabbableCube";

                // Physics
                Rigidbody rb = _spawnedCube.GetComponent<Rigidbody>();
                if (rb == null) rb = _spawnedCube.AddComponent<Rigidbody>();
                rb.mass           = _mass;
                rb.linearDamping  = _drag;
                rb.angularDamping = _angularDrag;
                rb.useGravity     = true;
                rb.isKinematic    = false;

                // XR grab interaction
                XRGrabInteractable grab = _spawnedCube.GetComponent<XRGrabInteractable>();
                if (grab == null) grab = _spawnedCube.AddComponent<XRGrabInteractable>();
                grab.throwOnDetach    = _throwOnRelease;
                grab.useDynamicAttach = true; // required for far/ray grab with NearFarInteractor

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
                grab.throwOnDetach    = _throwOnRelease;
                grab.useDynamicAttach = true; // required for far/ray grab with NearFarInteractor

                Debug.Log($"[GrabbableCubeSpawner] Primitive cube spawned at y={spawnY:F2} " +
                          $"(surface={_surfaceY:F2} + offset={_heightAboveSurface:F2}), " +
                          $"gravity={Physics.gravity.y:F2} m/s².");
            }
        }

        // ------------------------------------------------------------------ //
        // Out-of-bounds respawn
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Teleports the cube back to its original spawn position and zeroes its velocity.
        /// Called automatically by Update() when the cube falls below _fallThreshold.
        /// </summary>
        private void RespawnToOrigin()
        {
            // Stop any in-flight motion so the cube doesn't immediately fly off again.
            Rigidbody rb = _spawnedCube.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity        = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            _spawnedCube.transform.position = _spawnPosition;
            _spawnedCube.transform.rotation = Quaternion.identity;

            Debug.Log($"[GrabbableCubeSpawner] Cube fell below y={_fallThreshold} — reset to {_spawnPosition}.");
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
