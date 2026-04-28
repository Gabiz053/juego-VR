using UnityEngine;
using _Project.Scripts.Core;

namespace _Project.Scripts.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/Main VR Scene Bootstrap")]
    public sealed class MainVRSceneBootstrap : MonoBehaviour
    {
        [Header("Dependencies")]
        [Tooltip("Punto donde aparece el jugador al entrar en Main_VR.")]
        [SerializeField] private Transform _spawnPoint;

        private void Awake()
        {
            if (SceneController.Instance != null)
                SceneController.Instance.OnTransitionCompleted += RepositionPlayer;
        }

        private void OnDisable()
        {
            if (SceneController.Instance != null)
                SceneController.Instance.OnTransitionCompleted -= RepositionPlayer;
        }

        private void Start()
        {
            ValidateReferences();
            RepositionPlayer(); // primera carga
        }

        private void RepositionPlayer()
        {
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogWarning("[MainVRSceneBootstrap] XROrigin not found.", this);
                return;
            }

            // Deshabilitar el CharacterController antes de mover
            var cc = xrOrigin.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            xrOrigin.transform.SetPositionAndRotation(
                SessionContext.MainMenuSpawnPosition,
                SessionContext.MainMenuSpawnRotation
            );

            // Rehabilitar el CharacterController
            if (cc != null) cc.enabled = true;

            Debug.Log($"[MainVRSceneBootstrap] Player repositioned -- {SessionContext.MainMenuSpawnPosition}.");
        }

        private void ValidateReferences()
        {
            if (_spawnPoint == null)
                Debug.LogWarning("[MainVRSceneBootstrap] _spawnPoint is not assigned.", this);
        }
    }
}