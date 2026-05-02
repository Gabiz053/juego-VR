using UnityEngine;
using UnityEngine.InputSystem;

using _Project.Scripts.Core;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Attach to the Right-Hand XR Controller (the GameObject the user points
    /// with — same one the existing ray-line/PlanetPointer is on).
    ///
    /// Casts a ray from <c>transform.position</c> along <c>transform.forward</c>
    /// (same as <see cref="_Project.Scripts.UI.PlanetPointer"/>), but explicitly
    /// uses <see cref="QueryTriggerInteraction.Collide"/> so it also detects the
    /// trigger colliders that the planets use. This is the reason the
    /// <see cref="PlanetTeleporter"/> + XRSimpleInteractable approach never
    /// fired in practice: XRT 3.x's CurveInteractionCaster ignores triggers by
    /// default.
    ///
    /// When the ray is pointing at a planet that has a <see cref="PlanetSceneLink"/>,
    /// pressing the controller's trigger (or the keyboard fallback / left mouse
    /// button while testing without a headset) loads that planet's scene.
    ///
    /// THIS COMPONENT ONLY HANDLES TELEPORTATION. It deliberately does NOT touch
    /// the planet data card or the floating planet labels — that remains the job
    /// of <see cref="_Project.Scripts.UI.PlanetPointer"/>, so both can coexist.
    ///
    /// SETUP
    /// ─────
    /// 1. Add this component to the Right-Hand XR Controller GameObject (same
    ///    one that already has <c>PlanetPointer</c>).
    /// 2. (Optional) Drag the XR Trigger input action into <c>_triggerAction</c>
    ///    (e.g. <c>XRI Default Input Actions / XRI Right Interaction / Select</c>).
    ///    Without it, the keyboard (<c>E</c>) and mouse fallbacks still work.
    /// 3. On each planet you want to be teleport-able, add
    ///    <see cref="PlanetSceneLink"/> and fill in the Build Settings scene
    ///    name (e.g. "Tierra", "Marte", "Jupiter").
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Interaction/Planet Click Teleporter")]
    public sealed class PlanetClickTeleporter : MonoBehaviour
    {
        #region Inspector

        [Header("Ray")]
        [Tooltip("Maximum distance the ray travels (world units).")]
        [SerializeField] private float _rayDistance = 500f;

        [Tooltip("Layers the ray can hit. Leave as Everything unless you want to restrict.")]
        [SerializeField] private LayerMask _layerMask = ~0;

        [Header("XR Input (optional)")]
        [Tooltip("XR controller Trigger / Select action. Leave empty if you only want to use the keyboard / mouse fallback.")]
        [SerializeField] private InputActionReference _triggerAction;

        [Header("Keyboard / Mouse Fallback")]
        [Tooltip("Key that triggers teleport when pointing at a planet (handy in the Device Simulator).")]
        [SerializeField] private Key _fallbackKey = Key.E;

        [Tooltip("Allow left mouse button to also trigger teleport while pointing at a planet.")]
        [SerializeField] private bool _allowMouseClick = true;

        #endregion

        #region State

        private PlanetSceneLink _currentTarget;

        #endregion

        #region Unity Lifecycle

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
            // QueryTriggerInteraction.Collide is the key fix vs. the default
            // Physics.Raycast behaviour: planets use trigger colliders.
            Ray ray = new Ray(transform.position, transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _layerMask,
                    QueryTriggerInteraction.Collide))
            {
                Collider hitCol = hit.collider;

                // PlanetSceneLink may live on the planet root while the
                // collider is on a child mesh — walk up the hierarchy.
                PlanetSceneLink link = hitCol.GetComponent<PlanetSceneLink>()
                                    ?? hitCol.GetComponentInParent<PlanetSceneLink>();

                if (link != null)
                {
                    if (link != _currentTarget)
                    {
                        _currentTarget = link;
                        Debug.Log($"[PlanetClickTeleporter] Targeting '{link.gameObject.name}' → scene '{link.sceneName}'.");
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

            string scene = _currentTarget.sceneName;
            if (string.IsNullOrWhiteSpace(scene))
            {
                Debug.LogWarning($"[PlanetClickTeleporter] sceneName is empty on '{_currentTarget.gameObject.name}'." +
                                 " Fill it in on the PlanetSceneLink component.", this);
                return;
            }

            // Consume the target so a second click during the fade doesn't re-trigger.
            _currentTarget = null;

            if (SceneController.Instance == null)
            {
                Debug.LogWarning("[PlanetClickTeleporter] SceneController.Instance is null — cannot teleport.", this);
                return;
            }

            if (SceneController.Instance.IsTransitioning)
                return;

            Debug.Log($"[PlanetClickTeleporter] Teleporting to '{scene}'.");
            SceneController.Instance.LoadScene(scene, GameState.PlanetSurface);
        }

        #endregion
    }
}
