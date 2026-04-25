using UnityEngine;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Contiene los datos del planeta que PlanetPointer lee cuando apunta a el.
    /// Asignar a cada GameObject de planeta en escena.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Interaction/Planet Proxy")]
    public class PlanetProxy : MonoBehaviour
    {
        #region Inspector

        [Header("Visual")]
        [Tooltip("Sprite del icono del planeta para mostrar en el panel.")]
        [SerializeField] private Sprite _planetIcon;

        [Header("Identificacion")]
        [Tooltip("Nombre del planeta.")]
        [SerializeField] private string _planetName = "Nombre";

        [Tooltip("Tipo de planeta. Ej: Rocoso, Gaseoso, Gigante de hielo.")]
        [SerializeField] private string _planetType = "Rocoso";

        [Header("Datos orbitales")]
        [Tooltip("Distancia media al Sol en millones de km.")]
        [SerializeField] private float _distanceSunMKm = 57.9f;

        [Tooltip("Periodo orbital en dias terrestres.")]
        [SerializeField] private float _orbitalPeriod = 88f;

        [Header("Datos fisicos")]
        [Tooltip("Diametro del planeta en km.")]
        [SerializeField] private float _diameterKm = 4879f;

        [Tooltip("Duracion del dia en dias terrestres.")]
        [SerializeField] private float _dayDuration = 176f;

        [Tooltip("Gravedad superficial en m/s².")]
        [SerializeField] private float _gravity = 3.7f;

        [Header("Temperatura")]
        [Tooltip("Temperatura media en grados Celsius.")]
        [SerializeField] private float _avgTempC = 167f;

        [Tooltip("Temperatura minima en grados Celsius.")]
        [SerializeField] private float _minTempC = -180f;

        [Tooltip("Temperatura maxima en grados Celsius.")]
        [SerializeField] private float _maxTempC = 430f;

        [Header("Atmosfera")]
        [Tooltip("Gases de la atmosfera. Dejar vacio si no tiene atmosfera real.")]
        [SerializeField] private string[] _atmosphereGases = { "Oxigeno", "Sodio", "Hidrogeno" };

        [Header("Curiosidad")]
        [Tooltip("Dato curioso del planeta.")]
        [SerializeField]
        [TextArea(2, 4)]
        private string _curiosity =
            "Es el planeta mas cercano al Sol, pero no es el mas caliente (ese es Venus).";

        #endregion

        #region Public API

        public Sprite PlanetIcon => _planetIcon;
        public string PlanetName => _planetName;
        public string PlanetType => _planetType;
        public float DistanceSunMKm => _distanceSunMKm;
        public float OrbitalPeriod => _orbitalPeriod;
        public float DiameterKm => _diameterKm;
        public float DayDuration => _dayDuration;
        public float Gravity => _gravity;
        public float AvgTempC => _avgTempC;
        public float MinTempC => _minTempC;
        public float MaxTempC => _maxTempC;
        public string[] AtmosphereGases => _atmosphereGases;
        public string Curiosity => _curiosity;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            Debug.Log($"[PlanetProxy] Initialized -- planet: {_planetName}.");
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (string.IsNullOrEmpty(_planetName))
                Debug.LogWarning("[PlanetProxy] _planetName is not assigned.", this);
            if (string.IsNullOrEmpty(_planetType))
                Debug.LogWarning("[PlanetProxy] _planetType is not assigned.", this);
        }

        #endregion
    }
}