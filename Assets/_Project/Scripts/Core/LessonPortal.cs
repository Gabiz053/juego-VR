using System;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Marks a portal that transports the player to a lesson scene when they physically
    /// walk through the activation volume. Plays a looping hum while the player is nearby
    /// and a teleport whoosh when they enter. Only the camera/head position triggers activation
    /// — controller proximity does not.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/LessonPortal")]
    public sealed class LessonPortal : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[LessonPortal]";

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Lesson Target")]
        [Tooltip("Exact name of the Unity scene to load when the player enters this portal.")]
        [SerializeField] private string _targetSceneName;

        [Tooltip("Game state applied after the target scene finishes loading.")]
        [SerializeField] private GameState _targetGameState;

        [Tooltip("Human-readable label shown in debug logs. E.g. 'Leccion 1 - Diorama Solar'.")]
        [SerializeField] private string _lessonLabel;

        [Header("Activation Zone")]
        [Tooltip("Trigger collider the player must physically walk into. Usually a thin box placed at the portal opening.")]
        [SerializeField] private Collider _activationZone;

        [Header("Proximity Audio")]
        [Tooltip("Radius in metres at which the ambient hum starts playing.")]
        [SerializeField, Range(1f, 10f)] private float _proximityRadius = 5f;

        [Tooltip("Looping audio clip for the portal ambient hum. Assign teleporter_loop.wav.")]
        [SerializeField] private AudioClip _ambientHumClip;

        [Tooltip("Seconds to fade the hum in and out when entering/leaving proximity range.")]
        [SerializeField, Range(0f, 3f)] private float _humFadeDuration = 1f;

        [Tooltip("Maximum volume of the ambient hum (0-1).")]
        [SerializeField, Range(0f, 1f)] private float _humMaxVolume = 0.5f;

        #endregion

        #region Events ----------------------------------------------------------

        /// <summary>Raised when the player enters this portal. Passes the portal instance.</summary>
        public event Action<LessonPortal> OnPortalEntered;

        #endregion

        #region Cached Components -----------------------------------------------

        private Camera _mainCamera;
        private AudioSource _humSource;
        private bool _isActivated;
        private float _currentHumVolume;

        #endregion

        #region Public API ------------------------------------------------------

        /// <summary>Scene name this portal loads.</summary>
        public string TargetSceneName => _targetSceneName;

        /// <summary>Game state set after loading.</summary>
        public GameState TargetGameState => _targetGameState;

        /// <summary>True once the player has entered and the scene transition has begun.</summary>
        public bool IsActivated => _isActivated;

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Awake()
        {
            BuildHumSource();
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            ValidateReferences();
        }

        private void Update()
        {
            if (_isActivated || _mainCamera == null) return;

            var cameraPos = _mainCamera.transform.position;
            float dist = Vector3.Distance(cameraPos, transform.position);

            UpdateProximityHum(dist);
            CheckActivation(cameraPos);
        }

        private void OnDestroy()
        {
            if (_humSource != null)
                Destroy(_humSource.gameObject);
        }

        #endregion

        #region Internals -------------------------------------------------------

        private void CheckActivation(Vector3 cameraPos)
        {
            if (_activationZone == null) return;
            if (SceneController.Instance == null || SceneController.Instance.IsTransitioning) return;
            if (!_activationZone.bounds.Contains(cameraPos)) return;

            _isActivated = true;
            if (_humSource != null) _humSource.Stop();
            AudioManager.Instance?.PlayPortalTeleportSound(transform.position);
            OnPortalEntered?.Invoke(this);

            Debug.Log($"{LOG_TAG} Portal entered -- '{_lessonLabel}', loading '{_targetSceneName}'.");
            SceneController.Instance.LoadScene(_targetSceneName, _targetGameState);
        }

        private void UpdateProximityHum(float distanceToPortal)
        {
            if (_humSource == null) return;

            float target = distanceToPortal < _proximityRadius ? _humMaxVolume : 0f;
            float step = _humFadeDuration > 0f ? Time.deltaTime / _humFadeDuration : 1f;
            _currentHumVolume = Mathf.MoveTowards(_currentHumVolume, target, step);
            _humSource.volume = _currentHumVolume;
        }

        private void BuildHumSource()
        {
            var go = new GameObject("PortalHum");
            go.transform.SetParent(transform, worldPositionStays: false);

            _humSource = go.AddComponent<AudioSource>();
            _humSource.spatialBlend = 1f;
            _humSource.loop = true;
            _humSource.volume = 0f;
            _humSource.playOnAwake = false;
            _humSource.rolloffMode = AudioRolloffMode.Logarithmic;
            _humSource.minDistance = 0.5f;
            _humSource.maxDistance = _proximityRadius * 1.5f;

            if (_ambientHumClip != null)
            {
                _humSource.clip = _ambientHumClip;
                _humSource.Play();
            }
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (string.IsNullOrWhiteSpace(_targetSceneName))
                Debug.LogWarning($"{LOG_TAG} _targetSceneName is not assigned.", this);
            if (_activationZone == null)
                Debug.LogWarning($"{LOG_TAG} _activationZone is not assigned.", this);
            if (_mainCamera == null)
                Debug.LogWarning($"{LOG_TAG} Main Camera not found in scene.", this);
            if (_ambientHumClip == null)
                Debug.LogWarning($"{LOG_TAG} _ambientHumClip is not assigned.", this);
        }

        #endregion
    }
}
