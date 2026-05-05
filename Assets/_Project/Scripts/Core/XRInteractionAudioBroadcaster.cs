using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Persistent component that hooks into every XRBaseInteractable in every scene and
    /// plays grab, drop, and looping hold sounds automatically through AudioManager.
    /// No per-object setup required — attach once to the AudioManager GameObject.
    ///
    /// Strategy: subscribe to XRInteractionManager.interactableRegistered so dynamically
    /// spawned objects (GrabbableCube, Kepler planets, etc.) are caught, and also
    /// scan existing interactables at scene load for objects already in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/XR Interaction Audio Broadcaster")]
    public sealed class XRInteractionAudioBroadcaster : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[XRInteractionAudioBroadcaster]";

        #endregion

        #region Cached Components -----------------------------------------------

        private XRInteractionManager _currentManager;

        // One looping hold AudioSource per held object.
        private readonly Dictionary<GameObject, AudioSource> _holdSources = new();

        // Tracks which interactables we have subscribed to (avoids double-subscription).
        private readonly HashSet<IXRSelectInteractable> _subscribedInteractables = new();

        // Reused scratch list — avoids per-frame allocation in LateUpdate.
        private readonly List<GameObject> _staleKeys = new();

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            StartCoroutine(SubscribeNextFrame());
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UnsubscribeFromScene();
            ClearAllHoldSounds();
        }

        private void LateUpdate()
        {
            // Move each hold AudioSource to follow the held object.
            // Remove any stale entries whose object was destroyed mid-grab.
            _staleKeys.Clear();
            foreach (var kvp in _holdSources)
            {
                if (kvp.Key == null)
                {
                    if (kvp.Value != null) AudioManager.Instance?.StopHoldSound(kvp.Value);
                    _staleKeys.Add(kvp.Key);
                    continue;
                }
                if (kvp.Value == null) { _staleKeys.Add(kvp.Key); continue; }
                kvp.Value.transform.position = kvp.Key.transform.position;
            }
            foreach (var key in _staleKeys) _holdSources.Remove(key);
        }

        #endregion

        #region Internals -------------------------------------------------------

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ClearAllHoldSounds();
            UnsubscribeFromScene();
            StartCoroutine(SubscribeNextFrame());
        }

        // Wait one frame so all scene objects finish their Awake/OnEnable before we scan.
        private IEnumerator SubscribeNextFrame()
        {
            yield return null;

            _currentManager = FindFirstObjectByType<XRInteractionManager>();
            if (_currentManager == null)
            {
                Debug.Log($"{LOG_TAG} No XRInteractionManager in scene -- no interaction sounds.");
                yield break;
            }

            // Catch dynamically spawned interactables (e.g. GrabbableCubeSpawner).
            _currentManager.interactableRegistered   += OnInteractableRegistered;
            _currentManager.interactableUnregistered += OnInteractableUnregistered;

            // Subscribe to interactables that are already in the scene.
            var existing = FindObjectsByType<XRBaseInteractable>(FindObjectsSortMode.None);
            foreach (var interactable in existing)
                SubscribeToInteractable(interactable);

            Debug.Log($"{LOG_TAG} Subscribed to {_subscribedInteractables.Count} interactable(s).");
        }

        private void UnsubscribeFromScene()
        {
            if (_currentManager != null)
            {
                _currentManager.interactableRegistered   -= OnInteractableRegistered;
                _currentManager.interactableUnregistered -= OnInteractableUnregistered;
                _currentManager = null;
            }

            foreach (var selectable in _subscribedInteractables)
            {
                if (selectable == null) continue;
                selectable.selectEntered.RemoveListener(OnSelectEntered);
                selectable.selectExited.RemoveListener(OnSelectExited);
            }
            _subscribedInteractables.Clear();
        }

        private void OnInteractableRegistered(InteractableRegisteredEventArgs args)
        {
            SubscribeToInteractable(args.interactableObject);
        }

        private void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            UnsubscribeFromInteractable(args.interactableObject);
        }

        private void SubscribeToInteractable(IXRInteractable interactable)
        {
            if (interactable is not IXRSelectInteractable selectable) return;
            if (!_subscribedInteractables.Add(selectable)) return; // already subscribed

            selectable.selectEntered.AddListener(OnSelectEntered);
            selectable.selectExited.AddListener(OnSelectExited);
        }

        private void UnsubscribeFromInteractable(IXRInteractable interactable)
        {
            if (interactable is not IXRSelectInteractable selectable) return;
            if (!_subscribedInteractables.Remove(selectable)) return;

            selectable.selectEntered.RemoveListener(OnSelectEntered);
            selectable.selectExited.RemoveListener(OnSelectExited);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (args.interactableObject is not MonoBehaviour mono) return;
            var go  = mono.gameObject;
            var pos = go.transform.position;

            AudioManager.Instance?.PlayGrabSound(pos);

            if (!_holdSources.ContainsKey(go))
            {
                var src = AudioManager.Instance?.PlayHoldSound(pos);
                if (src != null)
                    _holdSources[go] = src;
            }
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (args.interactableObject is not MonoBehaviour mono) return;
            var go  = mono.gameObject;
            var pos = go.transform.position;

            AudioManager.Instance?.PlayDropSound(pos);

            if (_holdSources.TryGetValue(go, out var src))
            {
                AudioManager.Instance?.StopHoldSound(src);
                _holdSources.Remove(go);
            }
        }

        private void ClearAllHoldSounds()
        {
            foreach (var src in _holdSources.Values)
                AudioManager.Instance?.StopHoldSound(src);
            _holdSources.Clear();
        }

        #endregion
    }
}
