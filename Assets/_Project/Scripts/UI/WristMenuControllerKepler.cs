using System;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Controla la visibilidad del menu de muneca VR.
    /// Muestra el canvas HUD_WristMenu solo cuando el jugador
    /// mira hacia su palma izquierda (angulo camara-palma > umbral).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Wrist Menu Controller Kepler")]
    public class WristMenuControllerKepler : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[WristMenuControllerKepler]";
        private const float FADE_SPEED = 8f;

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Transform de la camara principal del XR Rig (Main Camera).")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Transform de la palma de la mano izquierda (Left Controller o XR Hand).")]
        [SerializeField] private Transform _palmTransform;

        [Tooltip("Canvas World Space del menu de muneca (HUD_WristMenu).")]
        [SerializeField] private Canvas _wristCanvas;

        [Tooltip("CanvasGroup del panel raiz para controlar el fade de opacidad.")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Visibility Settings")]
        [Tooltip("Dot product minimo para considerar que el jugador mira la muneca. " +
                 "0.7 equivale a ~45 grados. Aumentar para exigir mas precision.")]
        [SerializeField][Range(0f, 1f)] private float _dotThreshold = 0.7f;

        [Tooltip("Distancia maxima (metros) a la que se puede ver el menu. " +
                 "Evita que aparezca si el brazo esta muy lejos de la cabeza.")]
        [SerializeField][Range(0.1f, 1f)] private float _maxDistance = 0.5f;

        [Header("Buttons")]
        [Tooltip("Boton para volver al menu principal.")]
        [SerializeField] private Button _btnBack;

        [Tooltip("Boton para pausar / reanudar la simulacion.")]
        [SerializeField] private Button _btnPause;

        [Tooltip("Boton para mostrar u ocultar las lineas de orbita.")]
        [SerializeField] private Button _btnToggleOrbits;

        [Tooltip("Boton para instanciar un planeta.")]
        [SerializeField] private Button _btnSpawnPlanet;

        [Tooltip("Boton para pasar a la siguiente ley de Kepler.")]
        [SerializeField] private Button _btnNextLaw;

        [Header("Pause Button Icons")]
        [Tooltip("Icono que se muestra cuando las orbitas estan en marcha (indica que se puede pausar).")]
        [SerializeField] private Sprite _iconPause;

        [Tooltip("Icono que se muestra cuando las orbitas estan pausadas (indica que se puede reanudar).")]
        [SerializeField] private Sprite _iconPlay;

        [Tooltip("Componente Image del hijo Icon_Pause dentro de Btn_Pause.")]
        [SerializeField] private Image _pauseButtonIcon;

        [Header("Orbit Button Icons")]
        [Tooltip("Icono que se muestra cuando las orbitas son visibles (indica que se pueden ocultar).")]
        [SerializeField] private Sprite _iconOrbitVisible;

        [Tooltip("Icono que se muestra cuando las orbitas estan ocultas (indica que se pueden mostrar).")]
        [SerializeField] private Sprite _iconOrbitHidden;

        [Tooltip("Componente Image del hijo Icon_ToggleOrbits dentro de Btn_ToggleOrbits.")]
        [SerializeField] private Image _orbitButtonIcon;

        #endregion

        #region Events

        /// <summary>Se dispara cuando el jugador pulsa el boton Volver.</summary>
        public event Action OnBackPressed;

        /// <summary>Se dispara cuando el jugador pulsa el boton Pausa.</summary>
        public event Action OnPausePressed;

        /// <summary>Se dispara cuando el jugador pulsa el boton Toggle Orbits.</summary>
        public event Action OnToggleOrbitsPressed;

        /// <summary>Se dispara cuando el jugador pulsa el boton Spawn Planet.</summary>
        public event Action OnSpawnPlanetPressed;

        /// <summary>Se dispara cuando el jugador pulsa el boton Next Law.</summary>
        public event Action OnNextLawPressed;

        #endregion

        #region Cached Components
        #endregion

        #region State

        private bool _isVisible;
        private float _targetAlpha;

        #endregion

        #region Public API

        /// <summary>Indica si el menu de muneca esta visible en este momento.</summary>
        public bool IsVisible => _isVisible;

        /// <summary>
        /// Actualiza el icono del boton Pause segun el estado de pausa.
        /// Llamar desde KeplerSceneConnector tras cada toggle.
        /// </summary>
        public void SetPauseIcon(bool isPaused)
        {
            if (_pauseButtonIcon == null)
                return;

            _pauseButtonIcon.sprite = isPaused ? _iconPlay : _iconPause;
        }

        /// <summary>
        /// Actualiza el icono del boton Toggle Orbits segun la visibilidad actual.
        /// Llamar desde KeplerSceneConnector tras cada toggle.
        /// </summary>
        public void SetOrbitIcon(bool isVisible)
        {
            if (_orbitButtonIcon == null)
                return;

            _orbitButtonIcon.sprite = isVisible ? _iconOrbitVisible : _iconOrbitHidden;
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            RegisterButtonListeners();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            _isVisible = false;

            Debug.Log($"{LOG_TAG} Initialized -- wrist menu ready.");
        }

        private void Update()
        {
            EvaluateVisibility();
            ApplyFade();
        }

        private void OnDestroy()
        {
            UnregisterButtonListeners();
        }

        #endregion

        #region Internals

        private void EvaluateVisibility()
        {
            if (_cameraTransform == null || _palmTransform == null)
                return;

            Vector3 cameraOffset = _cameraTransform.position - _palmTransform.position;
            Vector3 palmToCam = cameraOffset.normalized;
            Vector3 palmNormal = _palmTransform.forward;
            float dot = Vector3.Dot(palmNormal, palmToCam);
            float distanceSqr = cameraOffset.sqrMagnitude;
            float maxDistanceSqr = _maxDistance * _maxDistance;

            bool shouldShow = dot >= _dotThreshold && distanceSqr <= maxDistanceSqr;

            if (shouldShow != _isVisible)
            {
                _isVisible = shouldShow;
                _targetAlpha = _isVisible ? 1f : 0f;
                if (_canvasGroup != null)
                {
                    _canvasGroup.interactable = _isVisible;
                    _canvasGroup.blocksRaycasts = _isVisible;
                }

                Debug.Log($"{LOG_TAG} Menu {(_isVisible ? "ON" : "OFF")} -- dot: {dot:F2}, distSqr: {distanceSqr:F3}.");
            }
        }

        private void ApplyFade()
        {
            if (_canvasGroup == null)
                return;

            _canvasGroup.alpha = Mathf.Lerp(
                _canvasGroup.alpha,
                _targetAlpha,
                Time.deltaTime * FADE_SPEED
            );
        }

        private void RegisterButtonListeners()
        {
            if (_btnBack != null)
                _btnBack.onClick.AddListener(HandleBackPressed);

            if (_btnPause != null)
                _btnPause.onClick.AddListener(HandlePausePressed);

            if (_btnToggleOrbits != null)
                _btnToggleOrbits.onClick.AddListener(HandleToggleOrbitsPressed);

            if (_btnSpawnPlanet != null)
                _btnSpawnPlanet.onClick.AddListener(HandleSpawnPlanetPressed);

            if (_btnNextLaw != null)
                _btnNextLaw.onClick.AddListener(HandleNextLawPressed);
        }

        private void UnregisterButtonListeners()
        {
            if (_btnBack != null)
                _btnBack.onClick.RemoveListener(HandleBackPressed);

            if (_btnPause != null)
                _btnPause.onClick.RemoveListener(HandlePausePressed);

            if (_btnToggleOrbits != null)
                _btnToggleOrbits.onClick.RemoveListener(HandleToggleOrbitsPressed);

            if (_btnSpawnPlanet != null)
                _btnSpawnPlanet.onClick.RemoveListener(HandleSpawnPlanetPressed);

            if (_btnNextLaw != null)
                _btnNextLaw.onClick.RemoveListener(HandleNextLawPressed);
        }

        private void HandleBackPressed()
        {
            Debug.Log($"{LOG_TAG} Back button pressed.");
            OnBackPressed?.Invoke();
        }

        private void HandlePausePressed()
        {
            Debug.Log($"{LOG_TAG} Pause button pressed.");
            OnPausePressed?.Invoke();
        }

        private void HandleToggleOrbitsPressed()
        {
            Debug.Log($"{LOG_TAG} Toggle orbits button pressed.");
            OnToggleOrbitsPressed?.Invoke();
        }

        private void HandleSpawnPlanetPressed()
        {
            Debug.Log($"{LOG_TAG} Spawn planet button pressed.");
            OnSpawnPlanetPressed?.Invoke();
        }

        private void HandleNextLawPressed()
        {
            Debug.Log($"{LOG_TAG} Next law button pressed.");
            OnNextLawPressed?.Invoke();
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_cameraTransform == null)
                Debug.LogWarning($"{LOG_TAG} _cameraTransform is not assigned.", this);

            if (_palmTransform == null)
                Debug.LogWarning($"{LOG_TAG} _palmTransform is not assigned.", this);

            if (_wristCanvas == null)
                Debug.LogWarning($"{LOG_TAG} _wristCanvas is not assigned.", this);

            if (_canvasGroup == null)
                Debug.LogWarning($"{LOG_TAG} _canvasGroup is not assigned.", this);

            if (_btnBack == null)
                Debug.LogWarning($"{LOG_TAG} _btnBack is not assigned.", this);

            if (_btnPause == null)
                Debug.LogWarning($"{LOG_TAG} _btnPause is not assigned.", this);

            if (_btnToggleOrbits == null)
                Debug.LogWarning($"{LOG_TAG} _btnToggleOrbits is not assigned.", this);

            if (_btnSpawnPlanet == null)
                Debug.LogWarning($"{LOG_TAG} _btnSpawnPlanet is not assigned.", this);

            if (_btnNextLaw == null)
                Debug.LogWarning($"{LOG_TAG} _btnNextLaw is not assigned.", this);

            if (_iconPause == null)
                Debug.LogWarning($"{LOG_TAG} _iconPause is not assigned.", this);

            if (_iconPlay == null)
                Debug.LogWarning($"{LOG_TAG} _iconPlay is not assigned.", this);

            if (_pauseButtonIcon == null)
                Debug.LogWarning($"{LOG_TAG} _pauseButtonIcon is not assigned.", this);

            if (_iconOrbitVisible == null)
                Debug.LogWarning($"{LOG_TAG} _iconOrbitVisible is not assigned.", this);

            if (_iconOrbitHidden == null)
                Debug.LogWarning($"{LOG_TAG} _iconOrbitHidden is not assigned.", this);

            if (_orbitButtonIcon == null)
                Debug.LogWarning($"{LOG_TAG} _orbitButtonIcon is not assigned.", this);
        }

        #endregion
    }
}
