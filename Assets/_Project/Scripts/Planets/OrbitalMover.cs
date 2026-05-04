using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Moves a planet along a Keplerian orbit spline using SplineAnimate.
    /// Delegates spline geometry to OrbitalSplineGenerator and line drawing to OrbitLineRenderer.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Orbital Mover")]
    public sealed class OrbitalMover : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[OrbitalMover]";

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Generates the orbit spline geometry when the planet is released.")]
        [SerializeField] private OrbitalSplineGenerator _splineGenerator;

        [Tooltip("Draws the orbit line.")]
        [SerializeField] private OrbitLineRenderer _orbitLineRenderer;

        [Tooltip("SplineAnimate en este mismo GameObject para seguir la orbita.")]
        [SerializeField] private SplineAnimate _splineAnimate;

        [Tooltip("SplineContainer en MarsOrbit que define la orbita.")]
        [SerializeField] private SplineContainer _splineContainer;

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region Public API

        /// <summary>
        /// Sets all Keplerian orbital elements and starts the orbit simulation.
        /// Also triggers spline and line-renderer updates.
        /// </summary>
        public void SetOrbitalElements(
            float semiMajorAxis,
            float eccentricity,
            float orbitalPeriod,
            float trueAnomalyAtLaunch,
            Vector3 orbitNormal,
            Vector3 periapsisDirection)
        {
            if (_splineGenerator != null)
                _splineGenerator.UpdateOrbit(semiMajorAxis, eccentricity, periapsisDirection);

            if (_orbitLineRenderer != null)
                _orbitLineRenderer.Redraw();

            if (_splineAnimate == null || _splineContainer == null)
            {
                Debug.LogWarning($"{LOG_TAG} _splineAnimate or _splineContainer is not assigned.", this);
                return;
            }

            _splineAnimate.Container = _splineContainer;
            _splineAnimate.Duration  = orbitalPeriod;
            _splineAnimate.Loop      = SplineAnimate.LoopMode.Loop;
            _splineAnimate.enabled   = true;
            _splineAnimate.Play();

            Debug.Log($"{LOG_TAG} Orbit set -- following spline, T={orbitalPeriod:F1}s.");
        }

        /// <summary>Stops the orbit simulation and hides the orbit line.</summary>
        public void StopOrbit()
        {
            if (_splineAnimate != null)
                _splineAnimate.enabled = false;

            if (_orbitLineRenderer != null)
                _orbitLineRenderer.Hide();

            Debug.Log($"{LOG_TAG} Orbit stopped.");
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            Debug.Log($"{LOG_TAG} Initialized.");
        }

        private void Update() { }

        #endregion

        #region Internals
        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_splineGenerator == null)
                Debug.LogWarning($"{LOG_TAG} _splineGenerator is not assigned.", this);
            if (_orbitLineRenderer == null)
                Debug.LogWarning($"{LOG_TAG} _orbitLineRenderer is not assigned.", this);
            if (_splineAnimate == null)
                Debug.LogWarning($"{LOG_TAG} _splineAnimate is not assigned.", this);
            if (_splineContainer == null)
                Debug.LogWarning($"{LOG_TAG} _splineContainer is not assigned.", this);
        }

        #endregion
    }
}