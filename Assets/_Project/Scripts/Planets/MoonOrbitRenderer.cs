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

        private LineRenderer lineRenderer;
        private bool _ready;

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            lineRenderer              = GetComponent<LineRenderer>();
            lineRenderer.loop         = true;
            lineRenderer.positionCount = segments;
            lineRenderer.startWidth   = 0.05f;
            lineRenderer.endWidth     = 0.05f;
            lineRenderer.useWorldSpace = true;

            StartCoroutine(WaitAndDraw());
            ValidateReferences();
        }

        private void Update()
        {
            if (!_ready) return;
            if (splineContainer == null || splineContainer.Spline.Count == 0) return;

            for (int i = 0; i < segments; i++)
            {
                float t = (float)i / segments;
                splineContainer.Evaluate(t, out var pos, out _, out _);
                lineRenderer.SetPosition(i, new Vector3(pos.x, pos.y, pos.z));
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
        }

        #endregion
    }
}
