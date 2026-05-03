using Asteroids;
using UnityEngine;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Generates a ring of asteroids in elliptical orbits around the Sun.
    /// Each asteroid requires an AsteroidOrbitAndRotate component on its prefab.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Asteroid Belt Generator")]
    public class AsteroidBeltGenerator : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[AsteroidBeltGenerator]";

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Transform of the Sun used as the orbit centre.")]
        [SerializeField] private Transform _sun;

        [Tooltip("Prefab of the asteroid to instantiate. Must have AsteroidOrbitAndRotate.")]
        [SerializeField] private GameObject _asteroidPrefab;

        [Tooltip("Dust-blast VFX prefab forwarded to AsteroidOrbitAndRotate.")]
        [SerializeField] private GameObject _dustBlastPrefab;

        [Header("Belt Parameters")]
        [Tooltip("Total number of asteroids to generate.")]
        [SerializeField] private int _asteroidCount = 200;

        [Tooltip("Inner radius of the asteroid belt in world units.")]
        [SerializeField] private float _minRadius = 5.8f;

        [Tooltip("Outer radius of the asteroid belt in world units.")]
        [SerializeField] private float _maxRadius = 6.3f;

        [Tooltip("Maximum vertical scatter from the belt plane in world units.")]
        [SerializeField] private float _verticalSpread = 0.3f;

        [Tooltip("Minimum scale applied to each asteroid.")]
        [SerializeField] private float _minScale = 0.05f;

        [Tooltip("Maximum scale applied to each asteroid.")]
        [SerializeField] private float _maxScale = 0.2f;

        [Header("AsteroidOrbitAndRotate Parameters")]
        [Tooltip("Base orbital speed forwarded to AsteroidOrbitAndRotate.")]
        [SerializeField] private float _orbitSpeed = 1f;

        [Tooltip("Minimum self-rotation speed in degrees per second.")]
        [SerializeField] private float _minRotationSpeed = 10f;

        [Tooltip("Maximum self-rotation speed in degrees per second.")]
        [SerializeField] private float _maxRotationSpeed = 50f;

        [Tooltip("Additional random range added to the base orbital speed per asteroid.")]
        [SerializeField] private float _additionalOrbitSpeedRange = 0.21f;

        [Tooltip("Whether AsteroidOrbitAndRotate checks for collisions.")]
        [SerializeField] private bool _checkCollisions = true;

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            if (_asteroidPrefab == null)
                return;

            if (_asteroidCount <= 0)
            {
                Debug.LogWarning($"{LOG_TAG} _asteroidCount must be greater than zero.", this);
                return;
            }

            GenerateBelt();
        }

        #endregion

        #region Internals

        private void GenerateBelt()
        {
            int asteroidCount = Mathf.Max(_asteroidCount, 0);
            float minRadius = Mathf.Min(_minRadius, _maxRadius);
            float maxRadius = Mathf.Max(_minRadius, _maxRadius);
            float minScale = Mathf.Min(_minScale, _maxScale);
            float maxScale = Mathf.Max(_minScale, _maxScale);
            Transform orbitCenter = _sun != null ? _sun : transform;
            Vector3 center = orbitCenter.position;

            for (int i = 0; i < asteroidCount; i++)
            {
                float   radius    = Random.Range(minRadius, maxRadius);
                float   angle     = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float   y         = Random.Range(-_verticalSpread, _verticalSpread);
                Vector3 spawnPos  = center + new Vector3(
                    Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);

                GameObject asteroid = Instantiate(_asteroidPrefab, spawnPos, Random.rotation, transform);
                asteroid.name                   = $"Asteroid_{i}";
                asteroid.transform.localScale   = Vector3.one * Random.Range(minScale, maxScale);

                AsteroidOrbitAndRotate orbitAndRotate = asteroid.GetComponent<AsteroidOrbitAndRotate>();
                if (orbitAndRotate != null)
                {
                    orbitAndRotate.sun                       = orbitCenter;
                    orbitAndRotate.orbitSpeed                = _orbitSpeed;
                    orbitAndRotate.minRotationSpeed          = _minRotationSpeed;
                    orbitAndRotate.maxRotationSpeed          = _maxRotationSpeed;
                    orbitAndRotate.checkCollisions           = _checkCollisions;
                    orbitAndRotate.additionalOrbitSpeedRange = _additionalOrbitSpeedRange;
                    orbitAndRotate.dustBlustPrefab           = _dustBlastPrefab;
                }
                else
                {
                    Debug.LogWarning($"{LOG_TAG} Asteroid_{i} prefab has no AsteroidOrbitAndRotate component.", asteroid);
                }
            }

            Debug.Log($"{LOG_TAG} Generated {asteroidCount} asteroids.");
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_sun == null)
                Debug.LogWarning($"{LOG_TAG} _sun is not assigned.", this);
            if (_asteroidPrefab == null)
                Debug.LogWarning($"{LOG_TAG} _asteroidPrefab is not assigned.", this);
            if (_dustBlastPrefab == null)
                Debug.LogWarning($"{LOG_TAG} _dustBlastPrefab is not assigned.", this);
            if (_minRadius > _maxRadius)
                Debug.LogWarning($"{LOG_TAG} _minRadius is greater than _maxRadius -- values will be swapped at runtime.", this);
            if (_minScale > _maxScale)
                Debug.LogWarning($"{LOG_TAG} _minScale is greater than _maxScale -- values will be swapped at runtime.", this);
        }

        #endregion
    }
}
