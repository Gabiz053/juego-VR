using TMPro;
using UnityEngine;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Muestra una tarjeta de datos world-space encima del planeta con la 3a ley de Kepler.
    /// Se activa cuando el planeta entra en orbita tras ser soltado.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Orbital Data Card")]
    public sealed class OrbitalDataCard : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[OrbitalDataCard]";

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Canvas world-space de la tarjeta.")]
        [SerializeField] private Canvas _canvas;

        [Tooltip("Texto del periodo orbital calculado.")]
        [SerializeField] private TextMeshProUGUI _txtPeriod;

        [Tooltip("Texto de la formula de la 3a ley de Kepler.")]
        [SerializeField] private TextMeshProUGUI _txtFormula;

        [Tooltip("Texto del semi-eje mayor.")]
        [SerializeField] private TextMeshProUGUI _txtSemiMajorAxis;

        [Header("Settings")]
        [Tooltip("Offset en Y sobre el planeta donde aparece la tarjeta.")]
        [SerializeField] private float _heightOffset = 1.5f;

        [Tooltip("Transform del planeta al que sigue la tarjeta.")]
        [SerializeField] private Transform _planetTransform;

        #endregion

        #region State

        private Transform _cameraTransform;
        private bool _isActive;

        #endregion

        #region Public API

        /// <summary>
        /// Muestra la tarjeta con los datos orbitales calculados.
        /// Llamar desde KeplerLab3SceneConnector al soltar el planeta.
        /// </summary>
        public void ShowOrbitalData(float semiMajorAxis, float orbitalPeriod)
        {
            if (_canvas == null) return;

            // Tierra como referencia: busca EarthOrbit en la escena
            // EarthOrbit Semi Major Axis = ? — necesito ese valor
            const float EARTH_UNITS = 9f;   // <-- sustituir por el valor real de EarthOrbit
            const float EARTH_DAYS = 365f;

            float ratio = semiMajorAxis / EARTH_UNITS;
            float periodInDays = EARTH_DAYS * Mathf.Sqrt(ratio * ratio * ratio);
            float distanceInUA = semiMajorAxis / EARTH_UNITS;
            float distanceInKm = distanceInUA * 149_597_870f;

            float tSquared = periodInDays * periodInDays;
            float aCubed = distanceInUA * distanceInUA * distanceInUA;
            float keplerConst = aCubed > 0.0001f ? tSquared / aCubed : 0f;

            if (_txtFormula != null)
                _txtFormula.text = $"Una vuelta al Sol: {periodInDays:F0} días\n" +
                                   $"Distancia al Sol: {distanceInKm:N0} km\n" +
                                   $"({distanceInUA:F2} UA)\n" +
                                   $"Cuanto más lejos, ¡más tarda!\n" +
                                   $"T² / a³ = {keplerConst:F3}";

            _canvas.gameObject.SetActive(true);
            _isActive = true;
            AudioManager.Instance?.PlayUIMenuOpen();

            Debug.Log($"{LOG_TAG} DataCard shown -- T={periodInDays:F0}d a={distanceInKm:N0}km ({distanceInUA:F2}UA).");
        }

        /// <summary>Oculta la tarjeta.</summary>
        public void Hide()
        {
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);

            _isActive = false;
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();

            if (Camera.main != null)
                _cameraTransform = Camera.main.transform;

            if (_canvas != null)
                _canvas.gameObject.SetActive(false);

            Debug.Log($"{LOG_TAG} Initialized.");
        }

        private void LateUpdate()
        {
            if (!_isActive) return;
            FollowPlanet();
            FaceCamera();
        }

        #endregion

        #region Internals

        private void FollowPlanet()
        {
            if (_planetTransform == null) return;
            transform.position = _planetTransform.position + Vector3.up * _heightOffset;
        }

        private void FaceCamera()
        {
            if (_cameraTransform == null) return;
            transform.rotation = Quaternion.LookRotation(
                transform.position - _cameraTransform.position);
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_canvas == null)
                Debug.LogWarning($"{LOG_TAG} _canvas is not assigned.", this);
            if (_txtPeriod == null)
                Debug.LogWarning($"{LOG_TAG} _txtPeriod is not assigned.", this);
            if (_txtFormula == null)
                Debug.LogWarning($"{LOG_TAG} _txtFormula is not assigned.", this);
            if (_txtSemiMajorAxis == null)
                Debug.LogWarning($"{LOG_TAG} _txtSemiMajorAxis is not assigned.", this);
            if (_planetTransform == null)
                Debug.LogWarning($"{LOG_TAG} _planetTransform is not assigned.", this);
        }

        #endregion
    }
}