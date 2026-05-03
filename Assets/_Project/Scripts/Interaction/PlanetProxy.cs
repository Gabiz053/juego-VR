using UnityEngine;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Data container read by PlanetPointer when the ray hits this planet.
    /// Attach to each planet GameObject in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Interaction/Planet Proxy")]
    public class PlanetProxy : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[PlanetProxy]";

        #endregion

        #region Inspector

        [Header("Visual")]
        [Tooltip("Planet icon sprite shown in the data panel.")]
        [SerializeField] private Sprite _planetIcon;

        [Header("Identification")]
        [Tooltip("Display name of the planet.")]
        [SerializeField] private string _planetName = "Name";

        [Tooltip("Planet type. E.g.: Rocky, Gas Giant, Ice Giant.")]
        [SerializeField] private string _planetType = "Rocky";

        [Header("Orbital Data")]
        [Tooltip("Average distance from the Sun in millions of km.")]
        [SerializeField] private float _distanceSunMKm = 57.9f;

        [Tooltip("Orbital period in Earth days.")]
        [SerializeField] private string _orbitalPeriod = "24 hours";

        [Header("Physical Data")]
        [Tooltip("Planet diameter in km.")]
        [SerializeField] private float _diameterKm = 4879f;

        [Tooltip("Length of one day in Earth days.")]
        [SerializeField] private string _dayDuration = "176 Earth days";

        [Tooltip("Surface gravity in m/s².")]
        [SerializeField] private float _gravity = 3.7f;

        [Header("Temperature")]
        [Tooltip("Average surface temperature in degrees Celsius.")]
        [SerializeField] private float _avgTempC = 167f;

        [Tooltip("Minimum surface temperature in degrees Celsius.")]
        [SerializeField] private float _minTempC = -180f;

        [Tooltip("Maximum surface temperature in degrees Celsius.")]
        [SerializeField] private float _maxTempC = 430f;

        [Header("Atmosphere")]
        [Tooltip("Atmospheric gases. Leave empty if the planet has no real atmosphere.")]
        [SerializeField] private string[] _atmosphereGases = { "Oxygen", "Sodium", "Hydrogen" };

        [Header("Curiosity")]
        [Tooltip("Interesting fact about the planet.")]
        [SerializeField]
        [TextArea(2, 4)]
        private string _curiosity =
            "It is the planet closest to the Sun, but not the hottest (that is Venus).";

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API

        public Sprite PlanetIcon      => _planetIcon;
        public string PlanetName      => _planetName;
        public string PlanetType      => _planetType;
        public float  DistanceSunMKm  => _distanceSunMKm;
        public string OrbitalPeriod   => _orbitalPeriod;
        public float  DiameterKm      => _diameterKm;
        public string DayDuration     => _dayDuration;
        public float  Gravity         => _gravity;
        public float  AvgTempC        => _avgTempC;
        public float  MinTempC        => _minTempC;
        public float  MaxTempC        => _maxTempC;
        public string[] AtmosphereGases => _atmosphereGases;
        public string Curiosity       => _curiosity;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
#if UNITY_EDITOR
            Debug.Log($"{LOG_TAG} '{gameObject.name}' ready.");
#endif
        }

        #endregion

        #region Internals
        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (string.IsNullOrEmpty(_planetName))
                Debug.LogWarning($"{LOG_TAG} _planetName is not assigned.", this);
            if (string.IsNullOrEmpty(_planetType))
                Debug.LogWarning($"{LOG_TAG} _planetType is not assigned.", this);
        }

        #endregion
    }
}
