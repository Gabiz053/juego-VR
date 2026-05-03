using TMPro;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// World-space floating panel that shows planet data.
    /// Place the Canvas as a child of the right hand in the XR Rig hierarchy.
    /// Call UpdateData() when the ray hits a planet; call Hide() when it leaves.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Planet Data Card")]
    public class PlanetDataCard : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[PlanetDataCard]";

        #endregion

        #region Inspector

        [Header("Header")]
        [Tooltip("Planet icon image.")]
        [SerializeField] private Image _planetIcon;

        [Tooltip("Planet name label.")]
        [SerializeField] private TextMeshProUGUI _txtPlanetName;

        [Tooltip("Planet type label.")]
        [SerializeField] private TextMeshProUGUI _txtPlanetType;

        [Header("Stats Grid")]
        [Tooltip("Distance to the Sun value label.")]
        [SerializeField] private TextMeshProUGUI _txtDistSol;

        [Tooltip("Diameter value label.")]
        [SerializeField] private TextMeshProUGUI _txtDiametro;

        [Tooltip("Day duration value label.")]
        [SerializeField] private TextMeshProUGUI _txtDurDia;

        [Tooltip("Year duration value label.")]
        [SerializeField] private TextMeshProUGUI _txtDurAnio;

        [Tooltip("Average temperature value label.")]
        [SerializeField] private TextMeshProUGUI _txtTMedia;

        [Tooltip("Temperature range value label.")]
        [SerializeField] private TextMeshProUGUI _txtRangoTemp;

        [Tooltip("Surface gravity value label.")]
        [SerializeField] private TextMeshProUGUI _txtGravedad;

        [Header("Atmosphere")]
        [Tooltip("Container that holds the atmosphere gas pills.")]
        [SerializeField] private Transform _pillContainer;

        [Tooltip("Prefab for a single gas pill — must have a TextMeshProUGUI child.")]
        [SerializeField] private GameObject _atmPillPrefab;

        [Header("Curiosity")]
        [Tooltip("Curiosity text label.")]
        [SerializeField] private TextMeshProUGUI _txtCuriosity;

        [Header("Hand Position")]
        [Tooltip("Right hand Transform of the XR Rig. The canvas floats relative to it.")]
        [SerializeField] private Transform _rightHand;

        [Tooltip("Local offset from the right hand in metres. Default: 15 cm upward.")]
        [SerializeField] private Vector3 _handOffset = new Vector3(0f, 0.15f, 0f);

        [Header("Canvas")]
        [Tooltip("Root world-space Canvas of the panel.")]
        [SerializeField] private Canvas _canvas;

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private Camera _mainCamera;

        #endregion

        #region Public API

        /// <summary>
        /// Fills all panel fields with data from the PlanetProxy being pointed at.
        /// </summary>
        public void UpdateData(PlanetProxy proxy)
        {
            if (_planetIcon != null && proxy.PlanetIcon != null)
                _planetIcon.sprite = proxy.PlanetIcon;

            SetText(_txtPlanetName, proxy.PlanetName);
            SetText(_txtPlanetType, proxy.PlanetType);
            SetText(_txtDistSol,    $"{proxy.DistanceSunMKm:F1} mill. km");
            SetText(_txtDiametro,   $"{proxy.DiameterKm:N0} km");
            SetText(_txtDurDia,     proxy.DayDuration);
            SetText(_txtDurAnio,    proxy.OrbitalPeriod);
            SetText(_txtTMedia,     $"{proxy.AvgTempC:F0} °C");
            SetText(_txtRangoTemp,  $"{proxy.MinTempC:F0} °C to {proxy.MaxTempC:F0} °C");
            SetText(_txtGravedad,   $"{proxy.Gravity:F1} m/s²");
            SetText(_txtCuriosity,  proxy.Curiosity);

            BuildAtmospherePills(proxy.AtmosphereGases);

            gameObject.SetActive(true);
            Debug.Log($"{LOG_TAG} Showing data for: {proxy.PlanetName}.");
        }

        /// <summary>
        /// Hides the panel when the ray no longer points at a planet.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            ValidateReferences();
            DisableRaycasts();
            gameObject.SetActive(false);
            Debug.Log($"{LOG_TAG} Initialized.");
        }

        private void LateUpdate()
        {
            if (_rightHand == null) return;

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            transform.position = _rightHand.TransformPoint(_handOffset);
            transform.LookAt(_mainCamera.transform);
            transform.Rotate(0f, 180f, 0f);
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
            var raycaster = _canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = false;
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_mainCamera == null)
                Debug.LogWarning($"{LOG_TAG} No Main Camera found -- panel will not face camera.", this);
            if (_planetIcon == null)
                Debug.LogWarning($"{LOG_TAG} _planetIcon is not assigned.", this);
            if (_txtPlanetName == null)
                Debug.LogWarning($"{LOG_TAG} _txtPlanetName is not assigned.", this);
            if (_txtPlanetType == null)
                Debug.LogWarning($"{LOG_TAG} _txtPlanetType is not assigned.", this);
            if (_txtDistSol == null)
                Debug.LogWarning($"{LOG_TAG} _txtDistSol is not assigned.", this);
            if (_txtDiametro == null)
                Debug.LogWarning($"{LOG_TAG} _txtDiametro is not assigned.", this);
            if (_txtDurDia == null)
                Debug.LogWarning($"{LOG_TAG} _txtDurDia is not assigned.", this);
            if (_txtDurAnio == null)
                Debug.LogWarning($"{LOG_TAG} _txtDurAnio is not assigned.", this);
            if (_txtTMedia == null)
                Debug.LogWarning($"{LOG_TAG} _txtTMedia is not assigned.", this);
            if (_txtRangoTemp == null)
                Debug.LogWarning($"{LOG_TAG} _txtRangoTemp is not assigned.", this);
            if (_txtGravedad == null)
                Debug.LogWarning($"{LOG_TAG} _txtGravedad is not assigned.", this);
            if (_pillContainer == null)
                Debug.LogWarning($"{LOG_TAG} _pillContainer is not assigned.", this);
            if (_atmPillPrefab == null)
                Debug.LogWarning($"{LOG_TAG} _atmPillPrefab is not assigned.", this);
            if (_txtCuriosity == null)
                Debug.LogWarning($"{LOG_TAG} _txtCuriosity is not assigned.", this);
            if (_rightHand == null)
                Debug.LogWarning($"{LOG_TAG} _rightHand is not assigned.", this);
            if (_canvas == null)
                Debug.LogWarning($"{LOG_TAG} _canvas is not assigned.", this);
        }

        #endregion
    }
}
