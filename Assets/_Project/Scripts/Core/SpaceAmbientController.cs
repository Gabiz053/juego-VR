using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Controls the ambient decorations in the portal room:
    /// slow black-hole rotation, periodic asteroid fly-bys, and optional skybox rotation.
    /// Place this on a dedicated SceneManager GameObject in Main_VR.unity.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/SpaceAmbientController")]
    public sealed class SpaceAmbientController : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[SpaceAmbientController]";

        // Standard skybox shader rotation property shared by most Unity skybox shaders.
        private const string SKYBOX_ROTATION_PROP = "_Rotation";

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Black Hole")]
        [Tooltip("Reference to the Blackhole_Large prefab in the scene.")]
        [SerializeField] private Transform _blackHole;

        [Tooltip("Degrees per second the black hole rotates around its local Y axis.")]
        [SerializeField, Range(0f, 30f)] private float _blackHoleRotationSpeed = 3f;

        [Header("Skybox Rotation")]
        [Tooltip("Slowly rotates the skybox to give the universe a living feel.")]
        [SerializeField] private bool _rotateSkybox = true;

        [Tooltip("Degrees per second the skybox rotates. Keep below 2 to avoid obvious movement.")]
        [SerializeField, Range(0f, 10f)] private float _skyboxRotationSpeed = 0.4f;

        [Header("Asteroid Fly-bys")]
        [Tooltip("Prefab spawned for each fly-by. Must have an AsteroidFlyBy component.")]
        [SerializeField] private GameObject _asteroidPrefab;

        [Tooltip("Minimum seconds between asteroid spawns.")]
        [SerializeField, Range(5f, 180f)] private float _minSpawnInterval = 25f;

        [Tooltip("Maximum seconds between asteroid spawns.")]
        [SerializeField, Range(5f, 600f)] private float _maxSpawnInterval = 70f;

        [Tooltip("Radius from the scene centre where asteroids spawn and despawn (metres).")]
        [SerializeField, Range(30f, 300f)] private float _spawnRadius = 100f;

        [Tooltip("Maximum concurrent fly-bys allowed at once.")]
        [SerializeField, Range(1, 10)] private int _maxConcurrentAsteroids = 3;

        [Tooltip("Travel speed range for spawned asteroids in metres per second.")]
        [SerializeField] private Vector2 _asteroidSpeedRange = new(5f, 12f);

        [Tooltip("Scale range for spawned asteroids in world metres.")]
        [SerializeField] private Vector2 _asteroidScaleRange = new(0.5f, 4f);

        #endregion

        #region Events ----------------------------------------------------------
        // No events.
        #endregion

        #region Cached Components -----------------------------------------------

        private int _activeAsteroidCount;
        private float _skyboxRotation;
        private Material _skyboxInstance;
        private bool _skyboxSupportsRotation;

        #endregion

        #region Public API ------------------------------------------------------
        // No public API.
        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            SetupSkybox();
            ValidateReferences();

            if (_asteroidPrefab != null)
                StartCoroutine(AsteroidSpawnRoutine());
        }

        private void Update()
        {
            RotateBlackHole();
            RotateSkybox();
        }

        private void OnDestroy()
        {
            // Reset skybox to 0 so the next scene starts with a clean rotation.
            if (_skyboxSupportsRotation && _skyboxInstance != null)
                _skyboxInstance.SetFloat(SKYBOX_ROTATION_PROP, 0f);

            if (_skyboxInstance != null)
                Destroy(_skyboxInstance);
        }

        #endregion

        #region Internals -------------------------------------------------------

        private void SetupSkybox()
        {
            if (!_rotateSkybox || RenderSettings.skybox == null) return;

            // Create a runtime instance so we never dirty the shared skybox asset.
            _skyboxInstance = new Material(RenderSettings.skybox);
            RenderSettings.skybox = _skyboxInstance;
            _skyboxSupportsRotation = _skyboxInstance.HasProperty(SKYBOX_ROTATION_PROP);

            if (!_skyboxSupportsRotation)
                Debug.Log($"{LOG_TAG} Skybox does not expose '{SKYBOX_ROTATION_PROP}' -- rotation skipped.");
        }

        private void RotateBlackHole()
        {
            if (_blackHole == null) return;
            _blackHole.Rotate(0f, _blackHoleRotationSpeed * Time.deltaTime, 0f, Space.Self);
        }

        private void RotateSkybox()
        {
            if (!_skyboxSupportsRotation) return;

            _skyboxRotation = (_skyboxRotation + _skyboxRotationSpeed * Time.deltaTime) % 360f;
            _skyboxInstance.SetFloat(SKYBOX_ROTATION_PROP, _skyboxRotation);
        }

        private IEnumerator AsteroidSpawnRoutine()
        {
            // Short random warm-up so the scene settles before the first asteroid.
            yield return new WaitForSeconds(UnityEngine.Random.Range(5f, 15f));

            while (true)
            {
                if (_activeAsteroidCount < _maxConcurrentAsteroids)
                    SpawnAsteroid();

                float interval = UnityEngine.Random.Range(_minSpawnInterval, _maxSpawnInterval);
                Debug.Log($"{LOG_TAG} Next asteroid in {interval:F0}s -- active: {_activeAsteroidCount}.");
                yield return new WaitForSeconds(interval);
            }
        }

        private void SpawnAsteroid()
        {
            // Pick a random origin on the spawn sphere and aim across to the opposite side.
            var origin      = UnityEngine.Random.onUnitSphere * _spawnRadius;
            var destination = -origin + UnityEngine.Random.insideUnitSphere * (_spawnRadius * 0.25f);
            var direction   = (destination - origin).normalized;
            float speed     = UnityEngine.Random.Range(_asteroidSpeedRange.x, _asteroidSpeedRange.y);
            var velocity    = direction * speed;
            float lifetime  = (destination - origin).magnitude / speed + 3f;

            var go = Instantiate(_asteroidPrefab, origin, Quaternion.LookRotation(direction));
            go.transform.localScale = Vector3.one * UnityEngine.Random.Range(_asteroidScaleRange.x, _asteroidScaleRange.y);

            if (go.TryGetComponent<AsteroidFlyBy>(out var flyBy))
            {
                flyBy.Initialize(velocity, lifetime);
                flyBy.OnExpired += OnAsteroidExpired;
                _activeAsteroidCount++;
                Debug.Log($"{LOG_TAG} Asteroid spawned -- speed: {speed:F1} m/s, lifetime: {lifetime:F1}s.");
            }
            else
            {
                Debug.LogWarning($"{LOG_TAG} _asteroidPrefab is missing AsteroidFlyBy component.", this);
                Destroy(go);
            }
        }

        private void OnAsteroidExpired()
        {
            _activeAsteroidCount = Mathf.Max(0, _activeAsteroidCount - 1);
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_blackHole == null)
                Debug.LogWarning($"{LOG_TAG} _blackHole is not assigned.", this);
            if (_asteroidPrefab == null)
                Debug.LogWarning($"{LOG_TAG} _asteroidPrefab is not assigned.", this);
        }

        #endregion
    }
}
