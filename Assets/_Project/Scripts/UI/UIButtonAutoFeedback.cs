using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using _Project.Scripts.Core;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Applies hover visual feedback and hover SFX to every Unity UI Button under a root transform.
    /// Attach once to a UI root (for example a wrist-menu Canvas) to avoid per-button setup.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/UI Button Auto Feedback")]
    public sealed class UIButtonAutoFeedback : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[UIButtonAutoFeedback]";

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Target Root")]
        [Tooltip("Root transform scanned for Button components. If null, this object is used.")]
        [SerializeField] private Transform _buttonsRoot;

        [Tooltip("If enabled, inactive child objects are included in the button scan.")]
        [SerializeField] private bool _includeInactiveButtons = true;

        [Header("Visual Feedback")]
        [Tooltip("Tint blended into each button's normal color for highlighted and selected states.")]
        [SerializeField] private Color _highlightTint = new Color(0.70f, 0.95f, 1.00f, 1.00f);

        [Tooltip("Blend factor used between each button normal color and Highlight Tint.")]
        [SerializeField, Range(0f, 1f)] private float _highlightTintBlend = 0.35f;

        [Tooltip("Extra brightness multiplier applied to highlighted and selected states.")]
        [SerializeField, Range(1f, 2f)] private float _highlightBrightness = 1.15f;

        [Tooltip("Brightness multiplier applied to pressed state.")]
        [SerializeField, Range(0.5f, 1.2f)] private float _pressedBrightness = 0.92f;

        [Tooltip("Fade duration used by Unity button color transitions.")]
        [SerializeField, Range(0f, 0.5f)] private float _fadeDuration = 0.08f;

        [Tooltip("Uniform scale multiplier applied while the pointer hovers a button.")]
        [SerializeField, Range(1f, 1.25f)] private float _hoverScaleMultiplier = 1.06f;

        [Tooltip("Forces all managed buttons to use Color Tint transition so glow is visible.")]
        [SerializeField] private bool _forceColorTintTransition = true;

        [Header("Audio Feedback")]
        [Tooltip("Plays UI hover SFX when pointer enters any managed button.")]
        [SerializeField] private bool _playHoverSound = true;

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private readonly HashSet<int> _hoverVisualRegisteredButtons = new();
        private readonly HashSet<int> _hoverSoundRegisteredButtons = new();
        private readonly Dictionary<int, Graphic[]> _buttonGraphics = new();
        private readonly Dictionary<int, Color[]> _buttonBaseColors = new();
        private readonly Dictionary<int, Transform> _buttonTransforms = new();
        private readonly Dictionary<int, Vector3> _buttonBaseScales = new();

        #endregion

        #region Public API ------------------------------------------------------

        /// <summary>
        /// Re-applies visual and audio feedback to all buttons under the configured root.
        /// Useful if buttons are spawned dynamically at runtime.
        /// </summary>
        public void ApplyFeedbackToButtons()
        {
            Transform root = _buttonsRoot != null ? _buttonsRoot : transform;
            Button[] buttons = root.GetComponentsInChildren<Button>(_includeInactiveButtons);

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                CacheButtonGraphics(button);
                ConfigureButtonVisualState(button);
                AddPointerHoverVisual(button);

                if (_playHoverSound)
                    AddPointerEnterHoverSound(button);
            }

            Debug.Log($"{LOG_TAG} Feedback applied -- {buttons.Length} button(s) configured.");
        }

        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            ValidateReferences();
            ApplyFeedbackToButtons();
        }

        private void OnDisable()
        {
            RestoreAllRuntimeTints();
        }

        #endregion

        #region Internals -------------------------------------------------------

        private void CacheButtonGraphics(Button button)
        {
            int buttonId = button.GetInstanceID();
            Graphic[] graphics = button.GetComponentsInChildren<Graphic>(_includeInactiveButtons);
            Color[] baseColors = new Color[graphics.Length];

            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                    continue;

                baseColors[i] = graphic.color;
            }

            _buttonGraphics[buttonId] = graphics;
            _buttonBaseColors[buttonId] = baseColors;
            _buttonTransforms[buttonId] = button.transform;
            _buttonBaseScales[buttonId] = button.transform.localScale;

            if (button.targetGraphic == null)
            {
                for (int i = 0; i < graphics.Length; i++)
                {
                    if (graphics[i] == null)
                        continue;

                    button.targetGraphic = graphics[i];
                    break;
                }
            }
        }

        private void ConfigureButtonVisualState(Button button)
        {
            ColorBlock colors = button.colors;
            Color normalColor = colors.normalColor;

            Color highlightedColor = Color.Lerp(normalColor, _highlightTint, _highlightTintBlend) * _highlightBrightness;
            Color pressedColor = normalColor * _pressedBrightness;

            highlightedColor.a = normalColor.a;
            pressedColor.a = normalColor.a;

            colors.highlightedColor = highlightedColor;
            colors.selectedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.fadeDuration = _fadeDuration;

            if (_forceColorTintTransition)
                button.transition = Selectable.Transition.ColorTint;

            button.colors = colors;
        }

        private void AddPointerHoverVisual(Button button)
        {
            if (button == null)
                return;

            int buttonId = button.GetInstanceID();
            if (_hoverVisualRegisteredButtons.Contains(buttonId))
                return;

            EventTrigger trigger = GetOrCreateEventTrigger(button);
            trigger.triggers.Add(CreateTriggerEntry(EventTriggerType.PointerEnter, _ => ApplyRuntimeHoverTint(buttonId)));
            trigger.triggers.Add(CreateTriggerEntry(EventTriggerType.PointerExit, _ => RestoreRuntimeTint(buttonId)));

            _hoverVisualRegisteredButtons.Add(buttonId);
        }

        private void AddPointerEnterHoverSound(Button button)
        {
            if (button == null)
                return;

            int buttonId = button.GetInstanceID();
            if (_hoverSoundRegisteredButtons.Contains(buttonId))
                return;

            EventTrigger trigger = GetOrCreateEventTrigger(button);
            trigger.triggers.Add(CreateTriggerEntry(EventTriggerType.PointerEnter, _ => AudioManager.Instance?.PlayUIHover()));

            _hoverSoundRegisteredButtons.Add(buttonId);
        }

        private EventTrigger GetOrCreateEventTrigger(Button button)
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = button.gameObject.AddComponent<EventTrigger>();

            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();

            return trigger;
        }

        private static EventTrigger.Entry CreateTriggerEntry(EventTriggerType eventType, UnityAction<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = eventType
            };

            entry.callback.AddListener(callback);
            return entry;
        }

        private void ApplyRuntimeHoverTint(int buttonId)
        {
            if (!_buttonGraphics.TryGetValue(buttonId, out Graphic[] graphics) ||
                !_buttonBaseColors.TryGetValue(buttonId, out Color[] baseColors))
                return;

            int count = Mathf.Min(graphics.Length, baseColors.Length);
            for (int i = 0; i < count; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                    continue;

                graphic.color = ComputeHighlightedColor(baseColors[i]);
            }

            ApplyHoverScale(buttonId, true);
        }

        private void RestoreRuntimeTint(int buttonId)
        {
            if (!_buttonGraphics.TryGetValue(buttonId, out Graphic[] graphics) ||
                !_buttonBaseColors.TryGetValue(buttonId, out Color[] baseColors))
                return;

            int count = Mathf.Min(graphics.Length, baseColors.Length);
            for (int i = 0; i < count; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                    continue;

                graphic.color = baseColors[i];
            }

            ApplyHoverScale(buttonId, false);
        }

        private void RestoreAllRuntimeTints()
        {
            foreach (int buttonId in _buttonGraphics.Keys)
                RestoreRuntimeTint(buttonId);
        }

        private Color ComputeHighlightedColor(Color baseColor)
        {
            Color highlightedColor = Color.Lerp(baseColor, _highlightTint, _highlightTintBlend) * _highlightBrightness;
            highlightedColor.r = Mathf.Clamp01(highlightedColor.r);
            highlightedColor.g = Mathf.Clamp01(highlightedColor.g);
            highlightedColor.b = Mathf.Clamp01(highlightedColor.b);
            highlightedColor.a = baseColor.a;
            return highlightedColor;
        }

        private void ApplyHoverScale(int buttonId, bool isHovered)
        {
            if (!_buttonTransforms.TryGetValue(buttonId, out Transform buttonTransform) ||
                buttonTransform == null ||
                !_buttonBaseScales.TryGetValue(buttonId, out Vector3 baseScale))
                return;

            buttonTransform.localScale = isHovered ? baseScale * _hoverScaleMultiplier : baseScale;
        }

        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_buttonsRoot == null)
                _buttonsRoot = transform;
        }

        #endregion
    }
}
