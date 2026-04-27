using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Panel flotante world-space que muestra los datos de un planeta.
    /// Colocar el Canvas como hijo de la mano derecha del XR Rig en la jerarquia.
    /// Llamar a UpdateData() cuando el raycast apunte a un planeta.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Planet Data Card")]
    public class PlanetDataCard : MonoBehaviour
    {
        #region Inspector

        [Header("Header")]
        [Tooltip("Imagen del icono del planeta.")]
        [SerializeField] private Image _planetIcon;

        [Tooltip("Texto con el nombre del planeta.")]
        [SerializeField] private TextMeshProUGUI _txtPlanetName;

        [Tooltip("Texto con el tipo de planeta.")]
        [SerializeField] private TextMeshProUGUI _txtPlanetType;

        [Header("Stats Grid")]
        [Tooltip("Valor de distancia al Sol.")]
        [SerializeField] private TextMeshProUGUI _txtDistSol;

        [Tooltip("Valor del diametro.")]
        [SerializeField] private TextMeshProUGUI _txtDiametro;

        [Tooltip("Valor de duracion del dia.")]
        [SerializeField] private TextMeshProUGUI _txtDurDia;

        [Tooltip("Valor de duracion del año.")]
        [SerializeField] private TextMeshProUGUI _txtDurAnio;

        [Tooltip("Valor de temperatura media.")]
        [SerializeField] private TextMeshProUGUI _txtTMedia;

        [Tooltip("Valor del rango de temperatura.")]
        [SerializeField] private TextMeshProUGUI _txtRangoTemp;

        [Tooltip("Valor de gravedad superficial.")]
        [SerializeField] private TextMeshProUGUI _txtGravedad;

        [Header("Atmosphere")]
        [Tooltip("Contenedor de pills de atmosfera.")]
        [SerializeField] private Transform _pillContainer;

        [Tooltip("Prefab de una pill de gas con TextMeshProUGUI dentro.")]
        [SerializeField] private GameObject _atmPillPrefab;

        [Header("Curiosity")]
        [Tooltip("Texto de la curiosidad del planeta.")]
        [SerializeField] private TextMeshProUGUI _txtCuriosity;

        [Header("Posicion en mano")]
        [Tooltip("Transform de la mano derecha del XR Rig. El Canvas flotara 15cm delante de ella.")]
        [SerializeField] private Transform _rightHand;

        [Tooltip("Offset local respecto a la mano derecha (en metros). Por defecto 15cm hacia arriba.")]
        [SerializeField] private Vector3 _handOffset = new Vector3(0f, 0.15f, 0f);

        [Header("Canvas")]
        [Tooltip("Canvas world-space raiz del panel.")]
        [SerializeField] private Canvas _canvas;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            DisableRaycasts();
            gameObject.SetActive(false);
            Debug.Log("[PlanetDataCard] Initialized.");
        }

        private void LateUpdate()
        {
            if (_rightHand == null) return;

            transform.position = _rightHand.TransformPoint(_handOffset);

            transform.LookAt(Camera.main.transform);
            transform.Rotate(0f, 180f, 0f);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Rellena todos los campos del panel con los datos del PlanetProxy apuntado.
        /// </summary>
        public void UpdateData(PlanetProxy proxy)
        {
            if (_planetIcon != null && proxy.PlanetIcon != null)
                _planetIcon.sprite = proxy.PlanetIcon;
            SetText(_txtPlanetName, proxy.PlanetName);
            SetText(_txtPlanetType, proxy.PlanetType);
            SetText(_txtDistSol, $"{proxy.DistanceSunMKm:F1} mill. km");
            SetText(_txtDiametro, $"{proxy.DiameterKm:N0} km");
            SetText(_txtDurDia, proxy.DayDuration);
            SetText(_txtDurAnio, proxy.OrbitalPeriod);
            SetText(_txtTMedia, $"{proxy.AvgTempC:F0} °C");
            SetText(_txtRangoTemp, $"{proxy.MinTempC:F0} °C a {proxy.MaxTempC:F0} °C");
            SetText(_txtGravedad, $"{proxy.Gravity:F1} m/s²");
            SetText(_txtCuriosity, proxy.Curiosity);

            BuildAtmospherePills(proxy.AtmosphereGases);

            gameObject.SetActive(true);
            Debug.Log($"[PlanetDataCard] UpdateData -- planet: {proxy.PlanetName}.");
        }

        /// <summary>
        /// Oculta el panel cuando el raycast deja de apuntar a un planeta.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            Debug.Log("[PlanetDataCard] Hidden.");
        }

        #endregion

        #region Internals

        private void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null)
                label.text = value;
        }

        private void BuildAtmospherePills(string[] gases)
        {
            if (_pillContainer == null) return;

            foreach (Transform child in _pillContainer)
                Destroy(child.gameObject);

            if (gases == null || _atmPillPrefab == null) return;

            foreach (string gas in gases)
            {
                GameObject pill = Instantiate(_atmPillPrefab, _pillContainer);
                TextMeshProUGUI label = pill.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = gas;
            }
        }

        private void DisableRaycasts()
        {
            if (_canvas == null) return;
            var raycaster = _canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = false;

            Debug.Log("[PlanetDataCard] GraphicRaycaster disabled.");
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_planetIcon == null)
                Debug.LogWarning("[PlanetDataCard] _planetIcon is not assigned.", this);
            if (_txtPlanetName == null)
                Debug.LogWarning("[PlanetDataCard] _txtPlanetName is not assigned.", this);
            if (_txtPlanetType == null)
                Debug.LogWarning("[PlanetDataCard] _txtPlanetType is not assigned.", this);
            if (_txtDistSol == null)
                Debug.LogWarning("[PlanetDataCard] _txtDistSol is not assigned.", this);
            if (_txtDiametro == null)
                Debug.LogWarning("[PlanetDataCard] _txtDiametro is not assigned.", this);
            if (_txtDurDia == null)
                Debug.LogWarning("[PlanetDataCard] _txtDurDia is not assigned.", this);
            if (_txtDurAnio == null)
                Debug.LogWarning("[PlanetDataCard] _txtDurAnio is not assigned.", this);
            if (_txtTMedia == null)
                Debug.LogWarning("[PlanetDataCard] _txtTMedia is not assigned.", this);
            if (_txtRangoTemp == null)
                Debug.LogWarning("[PlanetDataCard] _txtRangoTemp is not assigned.", this);
            if (_txtGravedad == null)
                Debug.LogWarning("[PlanetDataCard] _txtGravedad is not assigned.", this);
            if (_pillContainer == null)
                Debug.LogWarning("[PlanetDataCard] _pillContainer is not assigned.", this);
            if (_atmPillPrefab == null)
                Debug.LogWarning("[PlanetDataCard] _atmPillPrefab is not assigned.", this);
            if (_txtCuriosity == null)
                Debug.LogWarning("[PlanetDataCard] _txtCuriosity is not assigned.", this);
            if (_rightHand == null)
                Debug.LogWarning("[PlanetDataCard] _rightHand is not assigned.", this);
            if (_canvas == null)
                Debug.LogWarning("[PlanetDataCard] _canvas is not assigned.", this);
        }

        #endregion
    }
}