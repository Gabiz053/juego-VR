using System;
using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Persistent singleton that owns the global game state.
    /// Survives scene transitions via DontDestroyOnLoad.
    /// Access via GameManager.Instance — never use FindObjectOfType.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/GameManager")]
    public sealed class GameManager : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[GameManager]";

        #endregion

        #region Inspector

        [Header("Initial Configuration")]
        [Tooltip("Game state assigned on the very first launch, before any scene transition.")]
        [SerializeField] private GameState _initialState = GameState.MainMenu;

        #endregion

        #region Events

        /// <summary>
        /// Raised every time the game state changes. Passes the new <see cref="GameState"/>.
        /// Subscribe in OnEnable, unsubscribe in OnDisable.
        /// </summary>
        public event Action<GameState> OnGameStateChanged;

        #endregion

        #region Cached Components

        private static GameManager _instance;
        private GameState _currentState;

        #endregion

        #region Public API

        /// <summary>
        /// Global access point. Guaranteed non-null after Awake on the first scene load.
        /// </summary>
        public static GameManager Instance => _instance;

        /// <summary>
        /// Current application state. Read-only; use <see cref="SetState"/> to change it.
        /// </summary>
        public GameState CurrentState => _currentState;

        /// <summary>
        /// Transitions to <paramref name="newState"/>.
        /// No-ops silently if already in that state.
        /// Raises <see cref="OnGameStateChanged"/> on every real transition.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (_currentState == newState) return;

            var previous = _currentState;
            _currentState = newState;
            Debug.Log($"{LOG_TAG} State changed -- {previous} -> {newState}.");
            OnGameStateChanged?.Invoke(newState);
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.Log($"{LOG_TAG} Duplicate detected -- destroying redundant instance.");
                Destroy(gameObject);
                return;
            }

            _instance = this;
            transform.SetParent(null); // DontDestroyOnLoad only works on root GameObjects.
            DontDestroyOnLoad(gameObject);
            _currentState = _initialState;

            Debug.Log($"{LOG_TAG} Initialized -- initial state: {_currentState}.");
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

        #region Internals
        #endregion

        #region Validation

        private void ValidateReferences() { }

        #endregion
    }
}
