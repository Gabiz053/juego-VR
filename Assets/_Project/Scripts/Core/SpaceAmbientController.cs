using System.Collections;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Controls the ambient decorations in the portal room:
    /// slow black-hole rotation, continuous asteroid fly-bys, and optional skybox rotation.
    /// Place this on a dedicated SceneManager GameObject in Main_VR.unity.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/SpaceAmbientController")]
    public sealed class SpaceAmbientController : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[SpaceAmbientController]";
        private const string SKYBOX_ROTATION_PROP = "_Rotation";
        private const float MIN_ASTEROID_SPEED = 0.1f;
        private const float MIN_ASTEROID_SCALE = 0.05f;
        private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

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
        [Tooltip("Prefabs to choose from for each fly-by. One is picked at random each spawn. They do not need AsteroidFlyBy -- it is added automatically.")]
        [SerializeField] private GameObject[] _asteroidPrefabs;

        [Tooltip("How many asteroids to spawn immediately when the scene starts.")]
        [SerializeField, Range(0, 50)] private int _initialBatchCount = 20;

        [Tooltip("Minimum seconds between subsequent asteroid spawns (after the initial batch).")]
        [SerializeField, Range(0.5f, 60f)] private float _minSpawnInterval = 2f;

        [Tooltip("Maximum seconds between subsequent asteroid spawns.")]
        [SerializeField, Range(0.5f, 120f)] private float _maxSpawnInterval = 6f;

        [Tooltip("Radius from the scene centre where asteroids spawn and despawn (metres).")]
        [SerializeField, Range(30f, 300f)] private float _spawnRadius = 100f;

        [Tooltip("Maximum concurrent asteroids in the scene at once.")]
        [SerializeField, Range(1, 60)] private int _maxConcurrentAsteroids = 30;

        [Tooltip("Travel speed range for spawned asteroids in metres per second.")]
        [SerializeField] private Vector2 _asteroidSpeedRange = new(6f, 20f);

        [Tooltip("Scale range for spawned asteroids in world metres.")]
        [SerializeField] private Vector2 _asteroidScaleRange = new(0.3f, 5f);

        [Tooltip("Minimum metres from the scene centre that asteroid paths must clear. Prevents asteroids flying through the platform area. Set to roughly the platform diagonal radius (~8 for a 10x10 platform).")]
        [SerializeField, Range(0f, 60f)] private float _platformExclusionRadius = 12f;

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
            SanitizeConfiguration();
            SetupSkybox();
            ValidateReferences();

            StartCoroutine(AsteroidSpawnRoutine());
        }

        private void Update()
        {
            RotateBlackHole();
            RotateSkybox();
        }

        private void OnDestroy()
        {
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

        private GameObject PickRandomPrefab()
        {
            if (_asteroidPrefabs == null || _asteroidPrefabs.Length == 0) return null;
            // Returns null when the randomly chosen slot is empty — caller falls back to primitive.
            return _asteroidPrefabs[UnityEngine.Random.Range(0, _asteroidPrefabs.Length)];
        }

        [ContextMenu("Spawn Test Asteroid Now")]
        private void SpawnTestAsteroid()
        {
            if (!Application.isPlaying) return;
            // Fallback to primitives if no prefabs — still useful for testing the spawn logic.
            SpawnAsteroid();
        }

        private IEnumerator AsteroidSpawnRoutine()
        {
            // Spawn the initial burst immediately, one per frame to avoid a single-frame hitch.
            int batch = Mathf.Min(_initialBatchCount, _maxConcurrentAsteroids);
            for (int i = 0; i < batch; i++)
            {
                SpawnAsteroid();
                yield return null;
            }

            // Keep topping up asteroids at a regular interval.
            while (true)
            {
                if (_activeAsteroidCount < _maxConcurrentAsteroids)
                    SpawnAsteroid();

                yield return new WaitForSeconds(UnityEngine.Random.Range(_minSpawnInterval, _maxSpawnInterval));
            }
        }

        private void SpawnAsteroid()
        {
            var origin = UnityEngine.Random.onUnitSphere * _spawnRadius;
            // Perpendicular offset steers the path away from the centre so asteroids
            // clear the platform exclusion zone instead of flying through it.
            var perp        = Vector3.Cross(origin.normalized, UnityEngine.Random.onUnitSphere).normalized;
            float sideShift = _platformExclusionRadius * UnityEngine.Random.Range(1.5f, 2.5f);
            var destination = -origin + perp * sideShift
                              + UnityEngine.Random.insideUnitSphere * (_spawnRadius * 0.15f);
            var pathVector = destination - origin;
            var direction = pathVector.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE
                ? -origin.normalized
                : pathVector.normalized;
            float speed = Mathf.Max(MIN_ASTEROID_SPEED, UnityEngine.Random.Range(_asteroidSpeedRange.x, _asteroidSpeedRange.y));
            float lifetime  = (destination - origin).magnitude / speed + 5f;

            GameObject go;

            var chosenPrefab = PickRandomPrefab();
            if (chosenPrefab != null)
            {
                go = Instantiate(chosenPrefab, origin, Quaternion.LookRotation(direction));

                // Strip any third-party orbit/physics scripts (e.g. AsteroidOrbitAndRotate from
                // the Asteroids asset pack) — they crash without a 'sun' reference and we drive
                // movement ourselves via AsteroidFlyBy.
                var legacyOrbit = go.GetComponent("AsteroidOrbitAndRotate");
                if (legacyOrbit != null) Destroy(legacyOrbit);

                if (go.TryGetComponent<Rigidbody>(out var rb)) Destroy(rb);
            }
            else
            {
                // No prefabs assigned — primitive sphere fallback so asteroids always appear.
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Asteroid_Runtime";
                go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
                if (go.TryGetComponent<Collider>(out var col)) Destroy(col);
            }

            float scale = Mathf.Max(MIN_ASTEROID_SCALE, UnityEngine.Random.Range(_asteroidScaleRange.x, _asteroidScaleRange.y));
            go.transform.localScale = Vector3.one * scale;

            if (!go.TryGetComponent<AsteroidFlyBy>(out var flyBy))
                flyBy = go.AddComponent<AsteroidFlyBy>();

            flyBy.Initialize(direction * speed, lifetime);
            flyBy.OnExpired += OnAsteroidExpired;
            _activeAsteroidCount++;
        }

        private void OnAsteroidExpired()
        {
            _activeAsteroidCount = Mathf.Max(0, _activeAsteroidCount - 1);
        }

        private void SanitizeConfiguration()
        {
            if (_minSpawnInterval > _maxSpawnInterval)
                (_minSpawnInterval, _maxSpawnInterval) = (_maxSpawnInterval, _minSpawnInterval);

            if (_asteroidSpeedRange.x > _asteroidSpeedRange.y)
                (_asteroidSpeedRange.x, _asteroidSpeedRange.y) = (_asteroidSpeedRange.y, _asteroidSpeedRange.x);

            if (_asteroidScaleRange.x > _asteroidScaleRange.y)
                (_asteroidScaleRange.x, _asteroidScaleRange.y) = (_asteroidScaleRange.y, _asteroidScaleRange.x);

            _asteroidSpeedRange.x = Mathf.Max(MIN_ASTEROID_SPEED, _asteroidSpeedRange.x);
            _asteroidSpeedRange.y = Mathf.Max(_asteroidSpeedRange.x, _asteroidSpeedRange.y);

            _asteroidScaleRange.x = Mathf.Max(MIN_ASTEROID_SCALE, _asteroidScaleRange.x);
            _asteroidScaleRange.y = Mathf.Max(_asteroidScaleRange.x, _asteroidScaleRange.y);

            _spawnRadius = Mathf.Max(1f, _spawnRadius);
            _maxConcurrentAsteroids = Mathf.Max(1, _maxConcurrentAsteroids);
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_blackHole == null)
                Debug.LogWarning($"{LOG_TAG} _blackHole is not assigned.", this);
            if (_asteroidPrefabs == null || _asteroidPrefabs.Length == 0)
                Debug.LogWarning($"{LOG_TAG} _asteroidPrefabs is empty -- using primitive spheres as fallback.", this);
            if (_platformExclusionRadius >= _spawnRadius)
                Debug.LogWarning($"{LOG_TAG} _platformExclusionRadius is greater than or equal to _spawnRadius -- trajectories may be unstable.", this);
        }

        #endregion
    }
}
