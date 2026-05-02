using UnityEngine;
using Asteroids;

/// <summary>
/// Genera asteroides en orbitas elipticas alrededor del Sol.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("ProyectoVR/Planets/Asteroid Belt Generator")]
public class AsteroidBeltGenerator : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Transform del Sol.")]
    [SerializeField] private Transform _sun;

    [Tooltip("Prefab del asteroide a instanciar.")]
    [SerializeField] private GameObject _asteroidPrefab;

    [Tooltip("Prefab de explosion de polvo para AsteroidOrbitAndRotate.")]
    [SerializeField] private GameObject _dustBlastPrefab;

    [Header("Parametros del cinturon")]
    [Tooltip("Numero de asteroides a generar.")]
    [SerializeField] private int _asteroidCount = 200;

    [Tooltip("Radio minimo del cinturon.")]
    [SerializeField] private float _minRadius = 5.8f;

    [Tooltip("Radio maximo del cinturon.")]
    [SerializeField] private float _maxRadius = 6.3f;

    [Tooltip("Dispersion vertical maxima en unidades de escena.")]
    [SerializeField] private float _verticalSpread = 0.3f;

    [Tooltip("Escala minima del asteroide.")]
    [SerializeField] private float _minScale = 0.05f;

    [Tooltip("Escala maxima del asteroide.")]
    [SerializeField] private float _maxScale = 0.2f;

    [Header("Parametros AsteroidOrbitAndRotate")]
    [Tooltip("Velocidad orbital base.")]
    [SerializeField] private float _orbitSpeed = 1f;

    [Tooltip("Velocidad minima de rotacion propia.")]
    [SerializeField] private float _minRotationSpeed = 10f;

    [Tooltip("Velocidad maxima de rotacion propia.")]
    [SerializeField] private float _maxRotationSpeed = 50f;

    [Tooltip("Rango adicional de velocidad orbital aleatoria.")]
    [SerializeField] private float _additionalOrbitSpeedRange = 0.21f;

    [Tooltip("Activar deteccion de colisiones.")]
    [SerializeField] private bool _checkCollisions = true;

    private void Start()
    {
        ValidateReferences();
        GenerateBelt();
    }

    private void GenerateBelt()
    {
        for (int i = 0; i < _asteroidCount; i++)
        {
            float radius = Random.Range(_minRadius, _maxRadius);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float y = Random.Range(-_verticalSpread, _verticalSpread);

            Vector3 spawnPos = transform.position + new Vector3(
                Mathf.Cos(angle) * radius,
                y,
                Mathf.Sin(angle) * radius
            );

            GameObject asteroid = Instantiate(_asteroidPrefab, spawnPos, Random.rotation, transform);
            asteroid.name = $"Asteroid_{i}";
            asteroid.transform.localScale = Vector3.one * Random.Range(_minScale, _maxScale);

            AsteroidOrbitAndRotate orbitAndRotate = asteroid.GetComponent<AsteroidOrbitAndRotate>();
            if (orbitAndRotate != null)
            {
                orbitAndRotate.sun = _sun;
                orbitAndRotate.orbitSpeed = _orbitSpeed;
                orbitAndRotate.minRotationSpeed = _minRotationSpeed;
                orbitAndRotate.maxRotationSpeed = _maxRotationSpeed;
                orbitAndRotate.checkCollisions = _checkCollisions;
                orbitAndRotate.additionalOrbitSpeedRange = _additionalOrbitSpeedRange;
                orbitAndRotate.dustBlustPrefab = _dustBlastPrefab;
            }
            else
            {
                Debug.LogWarning($"[AsteroidBeltGenerator] Asteroid_{i} prefab does not have AsteroidOrbitAndRotate component.", asteroid);
            }
        }

        Debug.Log($"[AsteroidBeltGenerator] Generated {_asteroidCount} asteroids.");
    }

    private void ValidateReferences()
    {
        if (_sun == null)
            Debug.LogWarning("[AsteroidBeltGenerator] _sun is not assigned.", this);
        if (_asteroidPrefab == null)
            Debug.LogWarning("[AsteroidBeltGenerator] _asteroidPrefab is not assigned.", this);
        if (_dustBlastPrefab == null)
            Debug.LogWarning("[AsteroidBeltGenerator] _dustBlastPrefab is not assigned.", this);
    }
}