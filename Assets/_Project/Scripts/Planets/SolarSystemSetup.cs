using UnityEngine;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Scales all planets in the SolarSystem diorama at scene load.
    /// Planets are positioned relative to the Sun's world position — move the Sun
    /// GameObject in the Editor to reposition the whole diorama.
    /// All planet Transforms must be assigned in the Inspector before Play mode.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Solar System Setup")]
    public class SolarSystemSetup : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[SolarSystemSetup]";

        #endregion

        #region Inspector

        [Header("Planets")]
        [Tooltip("Transform of the Sun. Position it in the Editor — all planets orbit around its world position.")]
        [SerializeField] private Transform sun;

        [Tooltip("Transform of Mercury.")]
        [SerializeField] private Transform mercury;

        [Tooltip("Transform of Venus.")]
        [SerializeField] private Transform venus;

        [Tooltip("Transform of Earth.")]
        [SerializeField] private Transform earth;

        [Tooltip("Transform of Mars.")]
        [SerializeField] private Transform mars;

        [Tooltip("Transform of Jupiter.")]
        [SerializeField] private Transform jupiter;

        [Tooltip("Transform of Saturn.")]
        [SerializeField] private Transform saturn;

        [Tooltip("Transform of Uranus.")]
        [SerializeField] private Transform uranus;

        [Tooltip("Transform of Neptune.")]
        [SerializeField] private Transform neptune;

        [Header("Moons")]
        [Tooltip("Transform of the Moon. Positioned in local space relative to Earth.")]
        [SerializeField] private Transform moon;

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            SetupSolarSystem();
        }

        private void Start()
        {
            ValidateReferences();
        }

        #endregion

        #region Internals

        private void SetupSolarSystem()
        {
            if (sun != null)
                sun.localScale = Vector3.one * 5f;

            Vector3 center = sun != null ? sun.position : transform.position;

            SetupPlanet(mercury, 0.8f, 6f,  center);
            SetupPlanet(venus,   1.1f, 9f,  center);
            SetupPlanet(earth,   1.2f,  13f, center);
            SetupPlanet(mars,    0.9f, 17f, center);
            SetupPlanet(jupiter, 3.2f,  26f, center);
            SetupPlanet(saturn,  2.7f,  36f, center);
            SetupPlanet(uranus,  1.8f,  45f, center);
            SetupPlanet(neptune, 1.7f,  54f, center);

            if (moon != null)
            {
                moon.localScale    = Vector3.one * 0.4f;
                moon.localPosition = new Vector3(2.5f, 0f, 0f);
            }

            Debug.Log($"{LOG_TAG} Solar system positioned -- center: {center}.");
        }

        private void SetupPlanet(Transform planet, float scale, float orbitRadius, Vector3 center)
        {
            if (planet == null) return;

            planet.localScale = Vector3.one * scale;
            planet.position   = center + new Vector3(orbitRadius, 0f, 0f);
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (sun     == null) Debug.LogWarning($"{LOG_TAG} sun is not assigned.", this);
            if (mercury == null) Debug.LogWarning($"{LOG_TAG} mercury is not assigned.", this);
            if (venus   == null) Debug.LogWarning($"{LOG_TAG} venus is not assigned.", this);
            if (earth   == null) Debug.LogWarning($"{LOG_TAG} earth is not assigned.", this);
            if (moon    == null) Debug.LogWarning($"{LOG_TAG} moon is not assigned.", this);
            if (mars    == null) Debug.LogWarning($"{LOG_TAG} mars is not assigned.", this);
            if (jupiter == null) Debug.LogWarning($"{LOG_TAG} jupiter is not assigned.", this);
            if (saturn  == null) Debug.LogWarning($"{LOG_TAG} saturn is not assigned.", this);
            if (uranus  == null) Debug.LogWarning($"{LOG_TAG} uranus is not assigned.", this);
            if (neptune == null) Debug.LogWarning($"{LOG_TAG} neptune is not assigned.", this);
        }

        #endregion
    }
}
