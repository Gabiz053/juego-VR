using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Wrist Menu Controller Kepler")]
    public class WristMenuControllerKepler : MonoBehaviour
    {
        #region Constants
        private const string LOG_TAG = "[WristMenuControllerKepler]";
        private const float SHOW_DURATION = 0.28f;
        private const float HIDE_DURATION = 0.18f;
        private const float DEBOUNCE_TIME = 0.35f;
        #endregion

        #region Inspector
        [Header("References")]
        [Tooltip("Main camera / XR Head transform.")]
        [SerializeField] private Transform _cameraTransform;
        [Tooltip("Palm anchor of the left controller. Try LeftHand Controller > Palm Anchor.")]
        [SerializeField] private Transform _palmTransform;
        [Tooltip("World-space canvas that shows the menu.")]
        [SerializeField] private Canvas _wristCanvas;
        [Tooltip("CanvasGroup for fade.")]
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Visibility Settings")]
        [Tooltip("Dot product to OPEN the menu (palm must face camera this much). 0.5 = 60°, 0.65 = 49°.")]
        [SerializeField][Range(0f, 1f)] private float _showThreshold = 0.6f;
        [Tooltip("Dot product to CLOSE the menu. Lower than show to avoid flickering.")]
        [SerializeField][Range(0f, 1f)] private float _hideThreshold = 0.25f;
        [Tooltip("Maximum palm-to-camera distance for the menu to appear.")]
        [SerializeField][Range(0.1f, 2f)] private float _maxDistance = 0.9f;
        [Tooltip("Which axis of the palm transform points away from the palm surface toward the user's face. Try each until it works.")]
        [SerializeField] private PalmAxis _palmNormalAxis = PalmAxis.MinusUp;

        [Header("Buttons")]
        [SerializeField] private Button _btnBack;
        [SerializeField] private Button _btnPause;
        [SerializeField] private Button _btnToggleOrbits;
        [SerializeField] private Button _btnSpawnPlanet;
        [SerializeField] private Button _btnNextLaw;

        [Header("Pause Button Icons")]
        [SerializeField] private Sprite _iconPause;
        [SerializeField] private Sprite _iconPlay;
        [SerializeField] private Image _pauseButtonIcon;

        [Header("Orbit Button Icons")]
        [SerializeField] private Sprite _iconOrbitVisible;
        [SerializeField] private Sprite _iconOrbitHidden;
        [SerializeField] private Image _orbitButtonIcon;
        #endregion

        #region Events
        public event Action OnBackPressed;
        public event Action OnPausePressed;
        public event Action OnToggleOrbitsPressed;
        public event Action OnSpawnPlanetPressed;
        public event Action OnNextLawPressed;
        #endregion

        #region Public API
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

        public void SetSpawnButtonInteractable(bool interactable)
        {
            if (_btnSpawnPlanet != null)
                _btnSpawnPlanet.interactable = interactable;

            Debug.Log($"{LOG_TAG} Spawn button interactable: {interactable}.");
        }
        #endregion

        #region Cached Components
        private Transform _canvasTransform;
        private Vector3 _baseScale;
        #endregion

        public enum PalmAxis { Up, MinusUp, Forward, MinusForward }

        private bool _isVisible;
        private float _debounceTimer;
        private Coroutine _animCoroutine;

        #region Unity Lifecycle
        private void Start()
        {
            _canvasTransform = _wristCanvas != null ? _wristCanvas.transform : null;
            _baseScale = _canvasTransform != null ? _canvasTransform.localScale : Vector3.one;

            ValidateReferences();
            RegisterButtonListeners();
            EnsureAutoButtonFeedback();
            HideMenuInstant();

            Debug.Log($"{LOG_TAG} Initialized -- wrist menu ready.");
        }

        private void Update()
        {
            EvaluateGesture();
            FaceCamera();
        }

        private void OnDestroy()
        {
            UnregisterButtonListeners();
        }
        #endregion

        #region Internals
        private void EvaluateGesture()
        {
            if (_cameraTransform == null || _palmTransform == null)
                return;

            Vector3 cameraOffset = _cameraTransform.position - _palmTransform.position;
            float distanceSqr = cameraOffset.sqrMagnitude;

            float threshold = _isVisible ? _hideThreshold : _showThreshold;
            bool inRange = distanceSqr <= _maxDistance * _maxDistance;
            float dot = Vector3.Dot(GetPalmNormal(), cameraOffset.normalized);
            bool gestureHeld = inRange && dot >= threshold;

            if (gestureHeld && !_isVisible)
            {
                _debounceTimer += Time.unscaledDeltaTime;
                if (_debounceTimer >= DEBOUNCE_TIME)
                {
                    _debounceTimer = 0f;
                    ShowMenu();
                }
            }
            else if (!gestureHeld && _isVisible)
            {
                _debounceTimer = 0f;
                HideMenu();
            }
            else if (!gestureHeld)
            {
                _debounceTimer = 0f;
            }
        }

        private Vector3 GetPalmNormal()
        {
            return _palmNormalAxis switch
            {
                PalmAxis.Up           =>  _palmTransform.up,
                PalmAxis.MinusUp      => -_palmTransform.up,
                PalmAxis.Forward      =>  _palmTransform.forward,
                PalmAxis.MinusForward => -_palmTransform.forward,
                _                    => -_palmTransform.up,
            };
        }

        private void FaceCamera()
        {
            if (!_isVisible || _canvasTransform == null || _cameraTransform == null)
                return;

            Vector3 dir = _canvasTransform.position - _cameraTransform.position;
            if (dir.sqrMagnitude > 0.0001f)
                _canvasTransform.rotation = Quaternion.LookRotation(dir);
        }

        private void ShowMenu()
        {
            if (_isVisible) return;
            _isVisible = true;

            if (_wristCanvas != null) _wristCanvas.enabled = true;
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateShow());

            AudioManager.Instance?.PlayUIMenuOpen();
            Debug.Log($"{LOG_TAG} Menu ON.");
        }

        private void HideMenu()
        {
            if (!_isVisible) return;
            _isVisible = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateHide());

            Debug.Log($"{LOG_TAG} Menu OFF.");
        }

        private IEnumerator AnimateShow()
        {
            if (_canvasTransform != null)
                _canvasTransform.localScale = _baseScale * 0.55f;

            float elapsed = 0f;
            while (elapsed < SHOW_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / SHOW_DURATION);

                if (_canvasGroup != null)
                    _canvasGroup.alpha = Mathf.Clamp01(t * 2f);

                if (_canvasTransform != null)
                    _canvasTransform.localScale = _baseScale * EaseOutBack(t);

                yield return null;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            if (_canvasTransform != null)
                _canvasTransform.localScale = _baseScale;
        }

        private IEnumerator AnimateHide()
        {
            float startAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
            Vector3 startScale = _canvasTransform != null ? _canvasTransform.localScale : _baseScale;

            float elapsed = 0f;
            while (elapsed < HIDE_DURATION)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / HIDE_DURATION);

                if (_canvasGroup != null)
                    _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                if (_canvasTransform != null)
                    _canvasTransform.localScale = Vector3.Lerp(startScale, _baseScale * 0.7f, t);

                yield return null;
            }

            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            if (_canvasTransform != null) _canvasTransform.localScale = _baseScale;
            if (_wristCanvas != null) _wristCanvas.enabled = false;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        private void HideMenuInstant()
        {
            _isVisible = false;
            _debounceTimer = 0f;

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            if (_canvasTransform != null)
                _canvasTransform.localScale = _baseScale;
            if (_wristCanvas != null)
                _wristCanvas.enabled = false;
        }

        private void RegisterButtonListeners()
        {
            if (_btnBack != null)         _btnBack.onClick.AddListener(HandleBackPressed);
            if (_btnPause != null)        _btnPause.onClick.AddListener(HandlePausePressed);
            if (_btnToggleOrbits != null) _btnToggleOrbits.onClick.AddListener(HandleToggleOrbitsPressed);
            if (_btnSpawnPlanet != null)  _btnSpawnPlanet.onClick.AddListener(HandleSpawnPlanetPressed);
            if (_btnNextLaw != null)      _btnNextLaw.onClick.AddListener(HandleNextLawPressed);
        }

        private void UnregisterButtonListeners()
        {
            if (_btnBack != null)         _btnBack.onClick.RemoveListener(HandleBackPressed);
            if (_btnPause != null)        _btnPause.onClick.RemoveListener(HandlePausePressed);
            if (_btnToggleOrbits != null) _btnToggleOrbits.onClick.RemoveListener(HandleToggleOrbitsPressed);
            if (_btnSpawnPlanet != null)  _btnSpawnPlanet.onClick.RemoveListener(HandleSpawnPlanetPressed);
            if (_btnNextLaw != null)      _btnNextLaw.onClick.RemoveListener(HandleNextLawPressed);
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

        private void HandleSpawnPlanetPressed()
        {
            AudioManager.Instance?.PlayUIClick();
            Debug.Log($"{LOG_TAG} Spawn planet button pressed.");
            OnSpawnPlanetPressed?.Invoke();
        }

        private void HandleNextLawPressed()
        {
            AudioManager.Instance?.PlayUIClick();
            Debug.Log($"{LOG_TAG} Next law button pressed.");
            OnNextLawPressed?.Invoke();
        }

        private void EnsureAutoButtonFeedback()
        {
            if (_wristCanvas == null) return;
            if (_wristCanvas.GetComponent<UIButtonAutoFeedback>() != null) return;
            _wristCanvas.gameObject.AddComponent<UIButtonAutoFeedback>();
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
            if (_btnSpawnPlanet == null)
                Debug.LogWarning($"{LOG_TAG} _btnSpawnPlanet is not assigned.", this);
            if (_btnNextLaw == null)
                Debug.LogWarning($"{LOG_TAG} _btnNextLaw is not assigned.", this);
        }
        #endregion
    }
}
