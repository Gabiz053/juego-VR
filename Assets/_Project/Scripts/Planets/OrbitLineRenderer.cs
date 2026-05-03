using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Draws an orbit line along a SplineContainer using a LineRenderer.
    /// Waits for the spline to be populated before rendering, then draws once.
    /// Exposes Redraw() and Hide() for external orbit visibility control.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    [AddComponentMenu("ProyectoVR/Planets/Orbit Line Renderer")]
    public class OrbitLineRenderer : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[OrbitLineRenderer]";
        private const int MIN_RESOLUTION = 3;

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("SplineContainer that defines the orbit path to draw.")]
        [SerializeField] private SplineContainer splineContainer;

        [Header("Settings")]
        [Tooltip("Number of points sampled along the spline. Higher = smoother orbit line.")]
        [SerializeField] private int resolution = 128;

        [Tooltip("Width of the orbit line in world units.")]
        [SerializeField, Range(0.001f, 1f)] private float lineWidth = 0.05f;

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private LineRenderer _lr;
        private Coroutine _redrawCoroutine;

        #endregion

        #region Public API

        /// <summary>Re-enables the renderer and redraws the orbit on the next frame.</summary>
        public void Redraw()
        {
            if (splineContainer == null || splineContainer.Spline.Count == 0) return;

            if (_redrawCoroutine != null)
                StopCoroutine(_redrawCoroutine);

            _redrawCoroutine = StartCoroutine(RedrawNextFrame());
        }

        /// <summary>Hides the orbit line without destroying it.</summary>
        public void Hide()
        {
            if (_lr == null) _lr = GetComponent<LineRenderer>();
            _lr.enabled = false;
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _lr = GetComponent<LineRenderer>();
            StartCoroutine(WaitAndDraw());
            ValidateReferences();
        }

        #endregion

        #region Internals

        private IEnumerator WaitAndDraw()
        {
            while (splineContainer == null || splineContainer.Spline.Count == 0)
                yield return null;

            yield return null;
            DrawOrbit();
        }

        private IEnumerator RedrawNextFrame()
        {
            yield return null;
            if (_lr == null) _lr = GetComponent<LineRenderer>();
            _lr.enabled = true;
            DrawOrbit();
            _redrawCoroutine = null;
        }

        private void DrawOrbit()
        {
            if (splineContainer == null) return;
            if (splineContainer.Spline.Count == 0) return;
            if (_lr == null) _lr = GetComponent<LineRenderer>();

            int pointCount = Mathf.Max(resolution, MIN_RESOLUTION);

            _lr.loop          = true;
            _lr.useWorldSpace = true;
            _lr.positionCount = pointCount;
            _lr.startWidth    = lineWidth;
            _lr.endWidth      = lineWidth;

            for (int i = 0; i < pointCount; i++)
            {
                float t = (float)i / pointCount;
                splineContainer.Evaluate(t, out var pos, out _, out _);
                _lr.SetPosition(i, new Vector3(pos.x, pos.y, pos.z));
            }
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (splineContainer == null)
                Debug.LogWarning($"{LOG_TAG} splineContainer is not assigned.", this);
            if (resolution < MIN_RESOLUTION)
                Debug.LogWarning($"{LOG_TAG} resolution is below {MIN_RESOLUTION} -- it will be clamped at runtime.", this);
        }

        #endregion
    }
}
