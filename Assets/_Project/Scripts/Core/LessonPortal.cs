using System;
using TMPro;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// A 3D portal sphere the player physically walks into to travel to a lesson scene.
    /// Detection is camera/head only — controllers do not trigger it.
    /// The hum clip is taken from AudioManager so no audio asset needs to be assigned here.
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

        [Tooltip("Human-readable lesson name shown in the floating label and debug logs.")]
        [SerializeField] private string _lessonLabel;

        [Header("Label")]
        [Tooltip("TMP_Text component floating above the portal. Accepts both 3D Text and UI Text TMP. Its text is set to _lessonLabel at Start.")]
        [SerializeField] private TMP_Text _labelText;

        [Header("Activation Zone")]
        [Tooltip("Trigger collider the player must physically enter. Use a SphereCollider sized to match the portal sphere visual.")]
        [SerializeField] private Collider _activationZone;

        [Header("Proximity Audio")]
        [Tooltip("Radius in metres at which the ambient hum starts playing.")]
        [SerializeField, Range(1f, 20f)] private float _proximityRadius = 10f;

        [Tooltip("Seconds to fade the hum in and out.")]
        [SerializeField, Range(0f, 3f)] private float _humFadeDuration = 1f;

        [Tooltip("Maximum volume of the ambient hum (0-1).")]
        [SerializeField, Range(0f, 1f)] private float _humMaxVolume = 0.5f;

        [Header("Return Spawn")]
        [Tooltip("Punto donde aparece el jugador al volver de esta leccion a Main_VR. " +
         "Arrastra el [PlayerSpawnPoint] de Main_VR aqui.")]
        [SerializeField] private Transform _returnSpawnPoint;

        #endregion

        #region Events ----------------------------------------------------------

        /// <summary>Raised when the player enters this portal.</summary>
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
            // Camera.main may be null in Awake with XR — safe here after all Awakes run.
            _mainCamera = Camera.main;

            ConfigureHumFromAudioManager();

            if (_labelText != null)
                _labelText.text = _lessonLabel;

            ValidateReferences();
        }

        private void Update()
        {
            // Lazy camera cache — XR camera might not be tagged MainCamera until after Start.
            if (_mainCamera == null)
                _mainCamera = Camera.main;

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
            if (!IsInsideActivationZone(cameraPos)) return;

            _isActivated = true;
            if (_humSource != null) _humSource.Stop();
            AudioManager.Instance?.PlayPortalTeleportSound(transform.position);
            OnPortalEntered?.Invoke(this);

            if (_returnSpawnPoint != null)
            {
                SessionContext.MainMenuSpawnPosition = _returnSpawnPoint.position;
                SessionContext.MainMenuSpawnRotation = _returnSpawnPoint.rotation;
            }

            SceneController.Instance.LoadScene(_targetSceneName, _targetGameState);

            Debug.Log($"{LOG_TAG} Portal entered -- '{_lessonLabel}', loading '{_targetSceneName}'.");
            SceneController.Instance.LoadScene(_targetSceneName, _targetGameState);
        }

        // Handles both SphereCollider (portal sphere) and BoxCollider (flat plane fallback).
        private bool IsInsideActivationZone(Vector3 point)
        {
            if (_activationZone is SphereCollider sphere)
            {
                float worldRadius = sphere.radius * Mathf.Max(
                    Mathf.Abs(sphere.transform.lossyScale.x),
                    Mathf.Abs(sphere.transform.lossyScale.y),
                    Mathf.Abs(sphere.transform.lossyScale.z));
                var worldCenter = sphere.transform.TransformPoint(sphere.center);
                return Vector3.Distance(point, worldCenter) <= worldRadius;
            }

            return _activationZone.bounds.Contains(point);
        }

        private void UpdateProximityHum(float distanceToPortal)
        {
            if (_humSource == null) return;

            float target = distanceToPortal < _proximityRadius ? _humMaxVolume : 0f;
            float step   = _humFadeDuration > 0f ? Time.deltaTime / _humFadeDuration : 1f;
            _currentHumVolume = Mathf.MoveTowards(_currentHumVolume, target, step);
            _humSource.volume = _currentHumVolume;
        }

        private void BuildHumSource()
        {
            var go = new GameObject("PortalHum");
            go.transform.SetParent(transform, worldPositionStays: false);

            _humSource = go.AddComponent<AudioSource>();
            _humSource.spatialBlend = 1f;
            _humSource.loop        = true;
            _humSource.volume      = 0f;
            _humSource.playOnAwake = false;
            // Linear rolloff with minDistance = proximityRadius prevents Unity's spatial engine
            // from also attenuating within the zone our script fades — logarithmic rolloff
            // combined with our own volume fade resulted in the hum being inaudible.
            _humSource.rolloffMode = AudioRolloffMode.Linear;
            _humSource.minDistance = _proximityRadius;
            _humSource.maxDistance = _proximityRadius * 2.5f;
        }

        private void ConfigureHumFromAudioManager()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning($"{LOG_TAG} AudioManager not found -- portal hum will not play.", this);
                return;
            }

            var clip = AudioManager.Instance.PickRandomPortalHumClip();
            if (clip == null)
            {
                Debug.LogWarning($"{LOG_TAG} No portal hum clip -- assign clips to '_portalHumSounds' in AudioManager Inspector.", this);
                return;
            }

            _humSource.clip = clip;
            _humSource.Play();
            Debug.Log($"{LOG_TAG} Portal hum started -- '{clip.name}'.");
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
                Debug.LogWarning($"{LOG_TAG} Main Camera not found — will retry each frame.", this);
            if (_labelText == null)
                Debug.LogWarning($"{LOG_TAG} _labelText is not assigned — create a 3D Text (TextMeshPro) child and assign it.", this);
        }

        #endregion
    }
}
