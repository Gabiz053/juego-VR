using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Wrist Menu Controller")]
    public class WristMenuController : MonoBehaviour
    {
        private const string LOG_TAG = "[WristMenuController]";
        private const float FADE_SPEED = 8f;

        [Header("References")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Transform _palmTransform;
        [SerializeField] private Canvas _wristCanvas;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Visibility Settings")]
        [SerializeField][Range(0f, 1f)] private float _dotThreshold = 0.3f;
        [SerializeField][Range(0.1f, 2f)] private float _maxDistance = 1.5f;

        [Header("Buttons")]
        [SerializeField] private Button _btnBack;
        [SerializeField] private Button _btnPause;
        [SerializeField] private Button _btnToggleOrbits;

        [Header("Pause Button Icons")]
        [SerializeField] private Sprite _iconPause;
        [SerializeField] private Sprite _iconPlay;
        [SerializeField] private Image _pauseButtonIcon;

        [Header("Orbit Button Icons")]
        [SerializeField] private Sprite _iconOrbitVisible;
        [SerializeField] private Sprite _iconOrbitHidden;
        [SerializeField] private Image _orbitButtonIcon;

        public event Action OnBackPressed;
        public event Action OnPausePressed;
        public event Action OnToggleOrbitsPressed;

        private bool _isVisible;
        private float _targetAlpha;

        public bool IsVisible => _isVisible;

        public void SetPauseIcon(bool isPaused)
        {
            if (_pauseButtonIcon != null)
                _pauseButtonIcon.sprite = isPaused ? _iconPlay : _iconPause;
        }

        public void SetOrbitIcon(bool isVisible)
        {
            if (_orbitButtonIcon != null)
                _orbitButtonIcon.sprite = isVisible ? _iconOrbitVisible : _iconOrbitHidden;
        }

        private void Start()
        {
            ValidateReferences();
            RegisterButtonListeners();

            HideMenuInstant();

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

        private void EvaluateVisibility()
        {
            if (_cameraTransform == null || _palmTransform == null)
                return;

            Vector3 cameraOffset = _cameraTransform.position - _palmTransform.position;
            Vector3 palmToCamera = cameraOffset.normalized;

            /*
             * IMPORTANTE:
             * En muchos rigs XR, el eje UP del PalmAnchor apunta hacia el dorso.
             * Por eso usamos -up para representar la palma mirando hacia la cámara.
             *
             * Si no aparece, prueba estas variantes:
             * Vector3 palmNormal = _palmTransform.up;
             * Vector3 palmNormal = _palmTransform.forward;
             * Vector3 palmNormal = -_palmTransform.forward;
             */
            Vector3 palmNormal = -_palmTransform.up;

            float dot = Vector3.Dot(palmNormal, palmToCamera);
            float distanceSqr = cameraOffset.sqrMagnitude;
            float maxDistanceSqr = _maxDistance * _maxDistance;

            bool shouldShow = dot >= _dotThreshold && distanceSqr <= maxDistanceSqr;

            if (shouldShow == _isVisible)
                return;

            SetMenuVisible(shouldShow);

            Debug.Log($"{LOG_TAG} Menu {(_isVisible ? "ON" : "OFF")} -- dot: {dot:F2}, dist: {Mathf.Sqrt(distanceSqr):F2}m.");
        }

        private void SetMenuVisible(bool visible)
        {
            _isVisible = visible;
            _targetAlpha = visible ? 1f : 0f;

            if (_wristCanvas != null)
                _wristCanvas.enabled = true;

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }

            if (visible)
                AudioManager.Instance?.PlayUIMenuOpen();
        }

        private void ApplyFade()
        {
            if (_canvasGroup == null || _wristCanvas == null)
                return;

            _canvasGroup.alpha = Mathf.Lerp(
                _canvasGroup.alpha,
                _targetAlpha,
                Time.deltaTime * FADE_SPEED
            );

            if (!_isVisible && _canvasGroup.alpha <= 0.01f)
            {
                _canvasGroup.alpha = 0f;
                _wristCanvas.enabled = false;
            }
        }

        private void HideMenuInstant()
        {
            _isVisible = false;
            _targetAlpha = 0f;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_wristCanvas != null)
                _wristCanvas.enabled = false;
        }

        private void RegisterButtonListeners()
        {
            if (_btnBack != null)
                _btnBack.onClick.AddListener(HandleBackPressed);

            if (_btnPause != null)
                _btnPause.onClick.AddListener(HandlePausePressed);

            if (_btnToggleOrbits != null)
                _btnToggleOrbits.onClick.AddListener(HandleToggleOrbitsPressed);

            AddButtonHoverSound(_btnBack);
            AddButtonHoverSound(_btnPause);
            AddButtonHoverSound(_btnToggleOrbits);
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
            AudioManager.Instance?.PlayUIClick();
            Debug.Log($"{LOG_TAG} Back button pressed.");
            OnBackPressed?.Invoke();
        }

        private void HandlePausePressed()
        {
            AudioManager.Instance?.PlayUIClick();
            Debug.Log($"{LOG_TAG} Pause button pressed.");
            OnPausePressed?.Invoke();
        }

        private void HandleToggleOrbitsPressed()
        {
            AudioManager.Instance?.PlayUIClick();
            Debug.Log($"{LOG_TAG} Toggle orbits button pressed.");
            OnToggleOrbitsPressed?.Invoke();
        }

        private static void AddButtonHoverSound(Button button)
        {
            if (button == null)
                return;

            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();

            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };

            entry.callback.AddListener(_ => AudioManager.Instance?.PlayUIHover());
            trigger.triggers.Add(entry);
        }

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
        }
    }
}