using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using _Project.Scripts.Core;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Teleports the player to a planet scene when they select (click/grab) this planet.
    /// Requires an XRSimpleInteractable on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(XRSimpleInteractable))]
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Interaction/Planet Teleporter")]
    public sealed class PlanetTeleporter : MonoBehaviour
    {
        #region Inspector

        [Header("Destination")]
        [Tooltip("Exact scene name as registered in Build Settings (e.g. 'Tierra').")]
        [SerializeField] private string _sceneName;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            GetComponent<XRSimpleInteractable>().selectEntered.AddListener(OnSelected);
            Debug.Log($"[PlanetTeleporter] Initialized -- destination: '{_sceneName}'.");
        }

        private void OnDestroy()
        {
            var interactable = GetComponent<XRSimpleInteractable>();
            if (interactable != null)
                interactable.selectEntered.RemoveListener(OnSelected);
        }

        #endregion

        #region Handlers

        private void OnSelected(SelectEnterEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(_sceneName))
            {
                Debug.LogWarning("[PlanetTeleporter] _sceneName is empty — assign a scene name in the Inspector.", this);
                return;
            }

            if (SceneController.Instance == null)
            {
                Debug.LogWarning("[PlanetTeleporter] SceneController.Instance is null.", this);
                return;
            }

            if (SceneController.Instance.IsTransitioning)
                return;

            Debug.Log($"[PlanetTeleporter] Selected '{gameObject.name}' — loading scene '{_sceneName}'.");
            SceneController.Instance.LoadScene(_sceneName, GameState.PlanetSurface);
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (string.IsNullOrWhiteSpace(_sceneName))
                Debug.LogWarning("[PlanetTeleporter] _sceneName is not assigned.", this);
        }

        #endregion
    }
}
