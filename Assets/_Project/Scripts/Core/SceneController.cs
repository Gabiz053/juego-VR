using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Persistent singleton that handles all scene transitions with a fade-to-black overlay.
    /// Loads scenes asynchronously to prevent VR frame freezes.
    /// Access via SceneController.Instance — never use FindObjectOfType.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/SceneController")]
    public sealed class SceneController : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[SceneController]";

        // Distance from camera to place the fade quad (just past the near clip plane).
        private const float FADE_CANVAS_DISTANCE = 0.35f;

        // World-space size of the fade quad in meters. 2x2 m covers any VR FOV at 0.35 m.
        private const float FADE_CANVAS_SIZE = 2f;

        // Pause at full black between fade-out and fade-in (cinematic breath between scenes).
        private const float FADE_HOLD_DURATION = 0.05f;

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Fade Configuration")]
        [Tooltip("Seconds each fade phase (fade-to-black and fade-in) takes. Uses smooth easing.")]
        [SerializeField] private float _fadeDuration = 0.4f;

        #endregion

        #region Events ----------------------------------------------------------

        /// <summary>Raised at the start of every scene transition, before the fade begins.</summary>
        public event Action OnTransitionStarted;

        /// <summary>Raised once the new scene is active and the fade-in is complete.</summary>
        public event Action OnTransitionCompleted;

        #endregion

        #region Cached Components -----------------------------------------------

        private static SceneController _instance;

        private bool _isTransitioning;
        private CanvasGroup _canvasGroup;
        private Transform _fadeCanvasTransform;
        private readonly WaitForSecondsRealtime _fadeHoldWait = new(FADE_HOLD_DURATION);

        #endregion

        #region Public API ------------------------------------------------------

        /// <summary>
        /// Global access point. Non-null after Awake on the first scene load.
        /// </summary>
        public static SceneController Instance => _instance;

        /// <summary>True while a transition is in progress. Use to block repeated calls.</summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>
        /// Loads <paramref name="sceneName"/> asynchronously with a fade-to-black transition,
        /// then updates the <see cref="GameManager"/> to <paramref name="newState"/>.
        /// Silently ignored if a transition is already running.
        /// </summary>
        public void LoadScene(string sceneName, GameState newState)
        {
            if (_isTransitioning)
            {
                Debug.Log($"{LOG_TAG} Transition already in progress -- ignoring request for '{sceneName}'.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning($"{LOG_TAG} LoadScene called with empty scene name.", this);
                return;
            }

            StartCoroutine(LoadSceneRoutine(sceneName, newState));
        }

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log($"{LOG_TAG} Duplicate detected -- destroying redundant instance.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();

            Debug.Log($"{LOG_TAG} Initialized.");
        }

        private void Start()
        {
            ValidateReferences();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        #endregion

        #region Internals -------------------------------------------------------

        private IEnumerator LoadSceneRoutine(string sceneName, GameState newState)
        {
            _isTransitioning = true;
            OnTransitionStarted?.Invoke();
            Debug.Log($"{LOG_TAG} Transition started -- loading '{sceneName}'.");

            // Begin loading in the background before the fade starts so both happen in parallel.
            var operation = SceneManager.LoadSceneAsync(sceneName);

            if (operation == null)
            {
                Debug.LogWarning($"{LOG_TAG} Scene '{sceneName}' not found. Add it to File > Build Profiles.", this);
                _isTransitioning = false;
                OnTransitionCompleted?.Invoke();
                yield break;
            }

            operation.allowSceneActivation = false;

            // Fade to black while the scene loads in the background.
            yield return StartCoroutine(FadeRoutine(0f, 1f));

            // Wait until Unity has fully loaded the scene data (progress caps at 0.9 when
            // allowSceneActivation is false — that means the scene is ready to activate).
            yield return new WaitUntil(() => operation.progress >= 0.9f);

            // Activate the scene. This swaps out the old scene's objects.
            operation.allowSceneActivation = true;
            yield return new WaitUntil(() => operation.isDone);

            // Give the new scene one frame to run its Awake/Start methods.
            yield return null;

            // Notify GameManager of the new context.
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(newState);
            else
                Debug.LogWarning($"{LOG_TAG} GameManager.Instance is null after loading '{sceneName}'.", this);

            // Brief hold at full black — gives the scene a frame to settle visually.
            yield return _fadeHoldWait;

            // Fade back in using the new scene's camera.
            yield return StartCoroutine(FadeRoutine(1f, 0f));

            _isTransitioning = false;
            OnTransitionCompleted?.Invoke();
            Debug.Log($"{LOG_TAG} Transition completed -- scene: '{sceneName}', state: {newState}.");
        }

        private IEnumerator FadeRoutine(float from, float to)
        {
            float elapsed = 0f;
            _canvasGroup.alpha = from;

            while (elapsed < _fadeDuration)
            {
                UpdateFadeCanvasPosition();
                _canvasGroup.alpha = Mathf.SmoothStep(from, to, elapsed / _fadeDuration);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            _canvasGroup.alpha = to;
            UpdateFadeCanvasPosition();
        }

        private void UpdateFadeCanvasPosition()
        {
            if (Camera.main == null)
                return;

            var cam = Camera.main.transform;
            _fadeCanvasTransform.position = cam.position + cam.forward * FADE_CANVAS_DISTANCE;
            _fadeCanvasTransform.rotation = cam.rotation;
        }

        /// <summary>
        /// Builds the fade overlay at runtime so no prefab or scene setup is required.
        /// The canvas lives as a child of this GameObject (DontDestroyOnLoad).
        /// </summary>
        private void CreateFadeCanvas()
        {
            // Parent canvas GameObject.
            var canvasGO = new GameObject("FadeCanvas");
            canvasGO.transform.SetParent(transform, worldPositionStays: false);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 999;

            // Size the canvas large enough to cover any VR FOV at FADE_CANVAS_DISTANCE.
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(FADE_CANVAS_SIZE, FADE_CANVAS_SIZE);
            canvasGO.transform.localScale = Vector3.one;

            _fadeCanvasTransform = canvasGO.transform;

            // Full-canvas black image as a child.
            var imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(canvasGO.transform, worldPositionStays: false);

            var image = imageGO.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            var imgRT = imageGO.GetComponent<RectTransform>();
            imgRT.anchorMin = Vector2.zero;
            imgRT.anchorMax = Vector2.one;
            imgRT.sizeDelta = Vector2.zero;
            imgRT.anchoredPosition = Vector2.zero;

            // CanvasGroup on the parent drives overall alpha. Starts invisible.
            _canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            Debug.Log($"{LOG_TAG} Fade canvas created.");
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_canvasGroup == null)
                Debug.LogWarning($"{LOG_TAG} _canvasGroup is not assigned.", this);

            if (_fadeCanvasTransform == null)
                Debug.LogWarning($"{LOG_TAG} _fadeCanvasTransform is not assigned.", this);
        }

        #endregion
    }
}
