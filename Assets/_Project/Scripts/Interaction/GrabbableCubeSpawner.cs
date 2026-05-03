using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Spawns a grabbable, physics-driven object a fixed height above a reference surface.
    /// Assign a prefab to _cubePrefab for a custom mesh; leave it empty to use a primitive cube.
    /// The spawned object respawns automatically if it falls below _fallThreshold.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Interaction/Grabbable Cube Spawner")]
    public sealed class GrabbableCubeSpawner : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[GrabbableCubeSpawner]";

        #endregion

        #region Inspector

        [Header("Cube Prefab")]
        [Tooltip("Prefab to instantiate as the grabbable object. Leave empty to use a runtime-built primitive cube.")]
        [SerializeField] private GameObject _cubePrefab;

        [Header("Cube Shape (fallback — only when no prefab is assigned)")]
        [Tooltip("Side length of the cube in world units.")]
        [SerializeField] private float _cubeSize = 0.3f;

        [Tooltip("Optional material to apply to the cube.")]
        [SerializeField] private Material _cubeMaterial;

        [Tooltip("Fallback colour used when no material is assigned.")]
        [SerializeField] private Color _cubeColor = new Color(0.2f, 0.6f, 1f);

        [Header("Spawn Position")]
        [Tooltip("World-space Y of the surface the cube sits above.")]
        [SerializeField] private float _surfaceY = 0f;

        [Tooltip("How many units above the surface the cube centre is placed.")]
        [SerializeField, Min(0f)] private float _heightAboveSurface = 1.5f;

        [Tooltip("Horizontal offset from world origin so the cube doesn't spawn inside the player.")]
        [SerializeField] private Vector3 _horizontalOffset = new Vector3(0f, 0f, 1f);

        [Header("Physics")]
        [Tooltip("Mass of the Rigidbody in kg.")]
        [SerializeField, Min(0.01f)] private float _mass = 1f;

        [Tooltip("Linear drag applied to the Rigidbody.")]
        [SerializeField, Min(0f)] private float _drag = 0f;

        [Tooltip("Angular drag applied to the Rigidbody.")]
        [SerializeField, Min(0f)] private float _angularDrag = 0.05f;

        [Header("XR Interaction")]
        [Tooltip("When enabled, the cube is thrown on release.")]
        [SerializeField] private bool _throwOnRelease = true;

        [Header("Out-of-Bounds Respawn")]
        [Tooltip("If the cube's Y position drops below this value it teleports back to its spawn position.")]
        [SerializeField] private float _fallThreshold = -10f;

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private GameObject _spawnedCube;
        private Rigidbody  _spawnedRb;
        private Vector3    _spawnPosition;

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            SpawnCube();
        }

        private void Update()
        {
            if (_spawnedCube == null) return;
            if (_spawnedCube.transform.position.y <= _fallThreshold)
                RespawnToOrigin();
        }

        #endregion

        #region Internals

        private void SpawnCube()
        {
            float   spawnY   = _surfaceY + _heightAboveSurface + _cubeSize * 0.5f;
            _spawnPosition   = new Vector3(_horizontalOffset.x, spawnY + _horizontalOffset.y, _horizontalOffset.z);

            if (_cubePrefab != null)
            {
                _spawnedCube      = Instantiate(_cubePrefab, _spawnPosition, Quaternion.identity);
                _spawnedCube.name = "GrabbableCube";

                _spawnedRb = _spawnedCube.GetComponent<Rigidbody>();
                if (_spawnedRb == null) _spawnedRb = _spawnedCube.AddComponent<Rigidbody>();
                _spawnedRb.mass          = _mass;
                _spawnedRb.linearDamping = _drag;
                _spawnedRb.angularDamping = _angularDrag;
                _spawnedRb.useGravity    = true;
                _spawnedRb.isKinematic   = false;

                XRGrabInteractable grab = _spawnedCube.GetComponent<XRGrabInteractable>();
                if (grab == null) grab  = _spawnedCube.AddComponent<XRGrabInteractable>();
                grab.throwOnDetach      = _throwOnRelease;
                grab.useDynamicAttach   = true;

                Debug.Log($"{LOG_TAG} Prefab '{_cubePrefab.name}' spawned at y={spawnY:F2}, gravity={Physics.gravity.y:F2} m/s².");
            }
            else
            {
                _spawnedCube                        = GameObject.CreatePrimitive(PrimitiveType.Cube);
                _spawnedCube.name                   = "GrabbableCube";
                _spawnedCube.transform.position     = _spawnPosition;
                _spawnedCube.transform.localScale   = Vector3.one * _cubeSize;

                Renderer rend      = _spawnedCube.GetComponent<Renderer>();
                Shader   litShader = Shader.Find("Universal Render Pipeline/Lit")
                                  ?? Shader.Find("Standard");
                if (_cubeMaterial != null)
                {
                    rend.sharedMaterial = _cubeMaterial;
                }
                else if (litShader != null)
                {
                    Material mat = new Material(litShader) { name = "GrabbableCubeMat" };
                    mat.color           = _cubeColor;
                    rend.sharedMaterial = mat;
                }

                _spawnedRb              = _spawnedCube.AddComponent<Rigidbody>();
                _spawnedRb.mass          = _mass;
                _spawnedRb.linearDamping = _drag;
                _spawnedRb.angularDamping = _angularDrag;
                _spawnedRb.useGravity    = true;
                _spawnedRb.isKinematic   = false;

                XRGrabInteractable grab = _spawnedCube.AddComponent<XRGrabInteractable>();
                grab.throwOnDetach      = _throwOnRelease;
                grab.useDynamicAttach   = true;

                Debug.Log($"{LOG_TAG} Primitive cube spawned at y={spawnY:F2}, gravity={Physics.gravity.y:F2} m/s².");
            }
        }

        private void RespawnToOrigin()
        {
            if (_spawnedRb != null)
            {
                _spawnedRb.linearVelocity  = Vector3.zero;
                _spawnedRb.angularVelocity = Vector3.zero;
            }

            _spawnedCube.transform.position = _spawnPosition;
            _spawnedCube.transform.rotation = Quaternion.identity;

            Debug.Log($"{LOG_TAG} Cube fell below y={_fallThreshold:F1} -- reset to {_spawnPosition}.");
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_cubePrefab == null)
                Debug.LogWarning($"{LOG_TAG} _cubePrefab is not assigned -- using primitive cube fallback.", this);
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Respawn Cube (Editor Play Mode)")]
        private void RespawnCube()
        {
            if (_spawnedCube != null)
            {
                DestroyImmediate(_spawnedCube);
                _spawnedCube = null;
                _spawnedRb   = null;
            }
            SpawnCube();
        }
#endif
    }
}
