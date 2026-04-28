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
    [AddComponentMenu("ProyectoVR/UI/Wrist Menu Controller")]
    public class WristMenuController : MonoBehaviour
    {
        #region Constants

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

        #endregion

        #region Events

        /// <summary>Se dispara cuando el jugador pulsa el boton Volver.</summary>
        public event Action OnBackPressed;

        /// <summary>Se dispara cuando el jugador pulsa el boton Pausa.</summary>
        public event Action OnPausePressed;

        /// <summary>Se dispara cuando el jugador pulsa el boton Toggle Orbits.</summary>
        public event Action OnToggleOrbitsPressed;

        #endregion

        #region State

        private bool _isVisible;
        private float _targetAlpha;

        #endregion

        #region Public API

        /// <summary>Indica si el menu de muneca esta visible en este momento.</summary>
        public bool IsVisible => _isVisible;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            RegisterButtonListeners();

            // Empezar oculto
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _isVisible = false;

            Debug.Log("[WristMenuController] Initialized -- wrist menu ready.");
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

        /// <summary>
        /// Calcula si el jugador esta mirando la muneca usando Vector3.Dot.
        /// Condicion 1: el vector camara->palma apunta hacia la camara (dot > umbral).
        /// Condicion 2: la palma esta suficientemente cerca de la cabeza.
        /// </summary>
        private void EvaluateVisibility()
        {
            if (_cameraTransform == null || _palmTransform == null)
                return;

            // Vector desde la palma hacia la camara, normalizado
            Vector3 palmToCam = (_cameraTransform.position - _palmTransform.position).normalized;

            // Normal de la palma: el eje que apunta "hacia arriba" desde la palma
            // En XR Toolkit el eje forward del controller apunta hacia donde mira la palma
            Vector3 palmNormal = _palmTransform.forward;

            // Dot product: 1 = alineados perfectamente, 0 = perpendiculares, -1 = opuestos
            float dot = Vector3.Dot(palmNormal, palmToCam);

            // Distancia palma-cabeza para evitar falsos positivos con el brazo estirado
            float distance = Vector3.Distance(_palmTransform.position, _cameraTransform.position);

            bool shouldShow = dot >= _dotThreshold && distance <= _maxDistance;

            if (shouldShow != _isVisible)
            {
                _isVisible = shouldShow;
                _targetAlpha = _isVisible ? 1f : 0f;
                _canvasGroup.interactable = _isVisible;
                _canvasGroup.blocksRaycasts = _isVisible;

                Debug.Log($"[WristMenuController] Menu {(_isVisible ? "ON" : "OFF")} -- dot: {dot:F2}, dist: {distance:F2}.");
            }
        }

        /// <summary>
        /// Suaviza la transicion de opacidad del CanvasGroup usando Lerp.
        /// </summary>
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
        }

        private void UnregisterButtonListeners()
        {
            if (_btnBack != null)
                _btnBack.onClick.RemoveListener(HandleBackPressed);

            if (_btnPause != null)
                _btnPause.onClick.RemoveListener(HandlePausePressed);

            if (_btnToggleOrbits != null)
                _btnToggleOrbits.onClick.RemoveListener(HandleToggleOrbitsPressed);
        }

        private void HandleBackPressed()
        {
            Debug.Log("[WristMenuController] Back button pressed.");
            OnBackPressed?.Invoke();
        }

        private void HandlePausePressed()
        {
            Debug.Log("[WristMenuController] Pause button pressed.");
            OnPausePressed?.Invoke();
        }

        private void HandleToggleOrbitsPressed()
        {
            Debug.Log("[WristMenuController] Toggle orbits button pressed.");
            OnToggleOrbitsPressed?.Invoke();
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_cameraTransform == null)
                Debug.LogWarning("[WristMenuController] _cameraTransform is not assigned.", this);

            if (_palmTransform == null)
                Debug.LogWarning("[WristMenuController] _palmTransform is not assigned.", this);

            if (_wristCanvas == null)
                Debug.LogWarning("[WristMenuController] _wristCanvas is not assigned.", this);

            if (_canvasGroup == null)
                Debug.LogWarning("[WristMenuController] _canvasGroup is not assigned.", this);

            if (_btnBack == null)
                Debug.LogWarning("[WristMenuController] _btnBack is not assigned.", this);

            if (_btnPause == null)
                Debug.LogWarning("[WristMenuController] _btnPause is not assigned.", this);

            if (_btnToggleOrbits == null)
                Debug.LogWarning("[WristMenuController] _btnToggleOrbits is not assigned.", this);
        }

        #endregion
    }
}