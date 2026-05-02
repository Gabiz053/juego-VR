using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Spawns a grabbable, physics-driven cube a fixed height above a reference surface.
    ///
    /// HOW TO USE
    /// ──────────
    /// 1. Add this component to any persistent scene GameObject (e.g. the Scene Manager).
    /// 2. Tune the Inspector parameters below.
    /// 3. Press Play — the cube is created in Start(), inheriting the scene's gravity
    ///    (set by PlanetSceneSetup or LocalGravityModifier).
    ///
    /// REQUIREMENTS
    /// ────────────
    /// • XR Interaction Toolkit must be present in the project (already satisfied).
    /// • No prefab needed — the cube is built from a Unity primitive at runtime.
    /// </summary>
    [AddComponentMenu("ProyectoVR/Interaction/Grabbable Cube Spawner")]
    public sealed class GrabbableCubeSpawner : MonoBehaviour
    {
        // ------------------------------------------------------------------ //
        // Inspector — Cube shape & appearance
        // ------------------------------------------------------------------ //

        [Header("Cube Shape")]
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
            // ── 1. Create the primitive ───────────────────────────────────
            _spawnedCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _spawnedCube.name = "GrabbableCube";

            // ── 2. Position — a bit above the surface ────────────────────
            float spawnY = _surfaceY + _heightAboveSurface + _cubeSize * 0.5f;
            _spawnedCube.transform.position = new Vector3(
                _horizontalOffset.x,
                spawnY + _horizontalOffset.y,
                _horizontalOffset.z);

            // ── 3. Scale ──────────────────────────────────────────────────
            _spawnedCube.transform.localScale = Vector3.one * _cubeSize;

            // ── 4. Material / colour ──────────────────────────────────────
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

            // ── 5. Rigidbody — gravity enabled ────────────────────────────
            Rigidbody rb = _spawnedCube.AddComponent<Rigidbody>();
            rb.mass        = _mass;
            rb.linearDamping   = _drag;
            rb.angularDamping  = _angularDrag;
            rb.useGravity  = true;    // falls according to Physics.gravity (set per planet)
            rb.isKinematic = false;

            // ── 6. XRGrabInteractable — VR hand grabbing ──────────────────
            XRGrabInteractable grab = _spawnedCube.AddComponent<XRGrabInteractable>();
            grab.throwOnDetach = _throwOnRelease;

            Debug.Log($"[GrabbableCubeSpawner] Cube spawned at y={spawnY:F2} " +
                      $"(surface={_surfaceY:F2} + offset={_heightAboveSurface:F2}), " +
                      $"gravity={Physics.gravity.y:F2} m/s².");
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
