using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Moves the holographic floor quad to follow the player's horizontal position so that
    /// the Forcefield effect appears only in the area around the player's feet.
    /// The quad should be sized to the desired reveal radius (e.g. 4 m diameter).
    /// Optionally also sends the player world position to a shader property for shaders
    /// that support a native radial-reveal effect.
    /// Attach this to the small holographic floor quad (NOT the full glass platform).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/HoloFloorController")]
    public sealed class HoloFloorController : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[HoloFloorController]";

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Platform Bounds")]
        [Tooltip("Half-size of the full glass platform in metres. Prevents the holo quad from sliding off the edge. Set to 5 for a 10 x 10 platform.")]
        [SerializeField, Range(1f, 50f)] private float _platformHalfSize = 5f;

        [Tooltip("Tiny Y offset so the holo quad renders just above the glass floor and avoids z-fighting.")]
        [SerializeField] private float _surfaceYOffset = 0.005f;

        [Header("Shader Integration (optional)")]
        [Tooltip("If the Forcefield material exposes a world-space player position property, enter its exact name here (e.g. '_PlayerWorldPos'). Leave empty to skip.")]
        [SerializeField] private string _shaderPlayerPosProperty = "_PlayerWorldPos";

        [Tooltip("If the Forcefield material exposes a reveal radius property, enter its exact name here. Leave empty to skip.")]
        [SerializeField] private string _shaderRevealRadiusProperty = "";

        [Tooltip("Radius value sent to the shader when _shaderRevealRadiusProperty is set.")]
        [SerializeField, Range(0.5f, 10f)] private float _shaderRevealRadius = 2f;

        #endregion

        #region Events ----------------------------------------------------------
        // No events.
        #endregion

        #region Cached Components -----------------------------------------------

        private Camera _mainCamera;
        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Vector3 _platformCenter;

        #endregion

        #region Public API ------------------------------------------------------
        // No public API beyond Inspector.
        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Awake()
        {
            _renderer  = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            _platformCenter = transform.position;
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            ValidateReferences();
        }

        // LateUpdate so we read the camera position after the XR subsystem has applied
        // tracking data for this frame — eliminates a 1-frame lag in VR.
        private void LateUpdate()
        {
            if (_mainCamera == null) return;
            MoveWithPlayer();
            PushShaderProperties();
        }

        #endregion

        #region Internals -------------------------------------------------------

        private void MoveWithPlayer()
        {
            var cam = _mainCamera.transform.position;

            float x = Mathf.Clamp(cam.x, _platformCenter.x - _platformHalfSize, _platformCenter.x + _platformHalfSize);
            float z = Mathf.Clamp(cam.z, _platformCenter.z - _platformHalfSize, _platformCenter.z + _platformHalfSize);

            transform.position = new Vector3(x, _platformCenter.y + _surfaceYOffset, z);
        }

        private void PushShaderProperties()
        {
            bool pushPos    = !string.IsNullOrEmpty(_shaderPlayerPosProperty);
            bool pushRadius = !string.IsNullOrEmpty(_shaderRevealRadiusProperty);
            if (!pushPos && !pushRadius) return;
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propBlock);

            if (pushPos)
                _propBlock.SetVector(_shaderPlayerPosProperty, _mainCamera.transform.position);

            if (pushRadius)
                _propBlock.SetFloat(_shaderRevealRadiusProperty, _shaderRevealRadius);

            _renderer.SetPropertyBlock(_propBlock);
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_mainCamera == null)
                Debug.LogWarning($"{LOG_TAG} Main Camera not found in scene.", this);
            if (_renderer == null)
                Debug.LogWarning($"{LOG_TAG} Renderer is not assigned.", this);
        }

        #endregion
    }
}
