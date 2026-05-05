using UnityEngine;
using UnityEngine.InputSystem;
using _Project.Scripts.Core;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Attach to the Right-Hand XR Controller (same GameObject as PlanetPointer).
    /// Casts a ray with QueryTriggerInteraction.Collide so it detects planet trigger
    /// colliders — the XRSimpleInteractable approach failed because XRT 3.x's
    /// CurveInteractionCaster ignores triggers by default.
    /// When the ray hits a PlanetSceneLink and the player presses trigger (or E / left
    /// mouse in the Device Simulator), loads that planet's scene.
    /// Deliberately ignores the data card and floating labels — those are PlanetPointer's job.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Interaction/Planet Click Teleporter")]
    public sealed class PlanetClickTeleporter : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[PlanetClickTeleporter]";

        #endregion

        #region Inspector

        [Header("Ray")]
        [Tooltip("Maximum distance the ray travels (world units).")]
        [SerializeField] private float _rayDistance = 500f;

        [Tooltip("Layers the ray can hit. Leave as Everything unless you need to restrict.")]
        [SerializeField] private LayerMask _layerMask = ~0;

        [Header("XR Input (optional)")]
        [Tooltip("XR controller Trigger / Select action. Leave empty to rely on keyboard / mouse fallback only.")]
        [SerializeField] private InputActionReference _triggerAction;

        [Header("Keyboard / Mouse Fallback")]
        [Tooltip("Key that triggers teleport when pointing at a planet (useful in the Device Simulator).")]
        [SerializeField] private Key _fallbackKey = Key.E;

        [Tooltip("Allow left mouse button to also trigger teleport while pointing at a planet.")]
        [SerializeField] private bool _allowMouseClick = true;

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region State

        private PlanetSceneLink _currentTarget;

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            Debug.Log($"{LOG_TAG} Initialized.");
        }

        private void OnEnable()
        {
            if (_triggerAction != null && _triggerAction.action != null)
            {
                _triggerAction.action.Enable();
                _triggerAction.action.performed += OnTriggerPerformed;
            }
        }

        private void OnDisable()
        {
            if (_triggerAction != null && _triggerAction.action != null)
                _triggerAction.action.performed -= OnTriggerPerformed;
        }

        private void Update()
        {
            UpdateCurrentTarget();

            if (_currentTarget != null)
                CheckFallbackInput();
        }

        #endregion

        #region Internals

        private void UpdateCurrentTarget()
        {
            // QueryTriggerInteraction.Collide is required: planet colliders are triggers
            // and Physics.Raycast skips triggers by default.
            Ray ray = new Ray(transform.position, transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _layerMask,
                    QueryTriggerInteraction.Collide))
            {
                // PlanetSceneLink may live on the root while the collider is on a child mesh.
                PlanetSceneLink link = hit.collider.GetComponent<PlanetSceneLink>()
                                    ?? hit.collider.GetComponentInParent<PlanetSceneLink>();

                if (link != null)
                {
                    if (link != _currentTarget)
                    {
                        _currentTarget = link;
                        Debug.Log($"{LOG_TAG} Targeting '{link.gameObject.name}' → scene '{link.SceneName}'.");
                    }
                    return;
                }
            }

            _currentTarget = null;
        }

        private void CheckFallbackInput()
        {
            bool keyPressed = _fallbackKey != Key.None
                           && Keyboard.current != null
                           && Keyboard.current[_fallbackKey].wasPressedThisFrame;

            bool mousePressed = _allowMouseClick
                             && Mouse.current != null
                             && Mouse.current.leftButton.wasPressedThisFrame;

            if (keyPressed || mousePressed)
                TryTeleport();
        }

        private void OnTriggerPerformed(InputAction.CallbackContext _) => TryTeleport();

        private void TryTeleport()
        {
            if (_currentTarget == null) return;

            string scene = _currentTarget.SceneName;
            if (string.IsNullOrWhiteSpace(scene))
            {
                Debug.LogWarning($"{LOG_TAG} SceneName is empty on '{_currentTarget.gameObject.name}' -- fill it in on the PlanetSceneLink component.", this);
                return;
            }

            // Clear target before loading so a second press during the fade doesn't re-trigger.
            _currentTarget = null;

            if (SceneController.Instance == null)
            {
                Debug.LogWarning($"{LOG_TAG} SceneController.Instance is null -- cannot teleport.", this);
                return;
            }

            if (SceneController.Instance.IsTransitioning) return;

            AudioManager.Instance?.PlayUIClick();
            Debug.Log($"{LOG_TAG} Teleporting to '{scene}'.");
            SceneController.Instance.LoadScene(scene, GameState.PlanetSurface);
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_triggerAction == null)
                Debug.LogWarning($"{LOG_TAG} _triggerAction is not assigned -- keyboard/mouse fallback only.", this);
        }

        #endregion
    }
}
