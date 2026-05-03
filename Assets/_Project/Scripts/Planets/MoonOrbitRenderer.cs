using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Draws the Moon's orbit line by sampling a SplineContainer every frame.
    /// Redraws each frame because the spline follows the Earth as it orbits the Sun.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(LineRenderer))]
    [AddComponentMenu("ProyectoVR/Planets/Moon Orbit Renderer")]
    public class MoonOrbitRenderer : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[MoonOrbitRenderer]";
        private const int MIN_SEGMENTS = 3;
        private const float ORBIT_LINE_WIDTH = 0.05f;

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("SplineContainer that defines the Moon's orbit path.")]
        [SerializeField] private SplineContainer splineContainer;

        [Header("Settings")]
        [Tooltip("Number of points sampled along the spline per frame.")]
        [SerializeField] private int segments = 64;

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private LineRenderer _lineRenderer;
        private bool _ready;
        private int _segmentCount;

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _segmentCount = Mathf.Max(segments, MIN_SEGMENTS);

            _lineRenderer               = GetComponent<LineRenderer>();
            _lineRenderer.loop          = true;
            _lineRenderer.positionCount = _segmentCount;
            _lineRenderer.startWidth    = ORBIT_LINE_WIDTH;
            _lineRenderer.endWidth      = ORBIT_LINE_WIDTH;
            _lineRenderer.useWorldSpace = true;

            StartCoroutine(WaitAndDraw());
            ValidateReferences();
        }

        private void Update()
        {
            if (!_ready) return;
            if (splineContainer == null || splineContainer.Spline.Count == 0) return;

            for (int i = 0; i < _segmentCount; i++)
            {
                float t = (float)i / _segmentCount;
                splineContainer.Evaluate(t, out var pos, out _, out _);
                _lineRenderer.SetPosition(i, new Vector3(pos.x, pos.y, pos.z));
            }
        }

        #endregion

        #region Internals

        private IEnumerator WaitAndDraw()
        {
            while (splineContainer == null || splineContainer.Spline.Count == 0)
                yield return null;
            yield return null;
            _ready = true;
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (splineContainer == null)
                Debug.LogWarning($"{LOG_TAG} splineContainer is not assigned.", this);
            if (segments < MIN_SEGMENTS)
                Debug.LogWarning($"{LOG_TAG} segments is below {MIN_SEGMENTS} -- it will be clamped at runtime.", this);
        }

        #endregion
    }
}
