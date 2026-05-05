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

        [Tooltip("Generador de orbita en world space para planetas spawneados en KeplerLab.")]
        [SerializeField] private KeplerOrbitSplineGenerator _keplerSplineGenerator;

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
            // Generador nuevo (world space) tiene prioridad
            if (_keplerSplineGenerator != null)
                _keplerSplineGenerator.GenerateOrbit(
                    semiMajorAxis,
                    eccentricity,
                    periapsisDirection,
                    orbitNormal);
            else if (_splineGenerator != null)
                _splineGenerator.UpdateOrbit(semiMajorAxis, eccentricity, periapsisDirection);

            if (_orbitLineRenderer != null)
                _orbitLineRenderer.Redraw();

            if (_splineAnimate == null || _splineContainer == null)
            {
                Debug.LogWarning($"{LOG_TAG} _splineAnimate or _splineContainer is not assigned.", this);
                return;
            }

            _splineAnimate.Container = _splineContainer;
            _splineAnimate.Duration = orbitalPeriod;
            _splineAnimate.Loop = SplineAnimate.LoopMode.Loop;
            _splineAnimate.enabled = true;

            // Los knots del spline estan distribuidos uniformemente en *eccentric*
            // anomaly E (ver OrbitalSplineGenerator/KeplerOrbitSplineGenerator),
            // mientras que el OrbitalLauncher nos pasa la *true* anomaly nu.
            // Convertimos nu -> E para que NormalizedTime caiga exactamente en el
            // punto del spline que corresponde a la posicion donde el jugador
            // solto el planeta. Para circular (e=0) E == nu, asi que esto se
            // reduce a la formula vieja sin sorpresas.
            float E = TrueToEccentricAnomaly(trueAnomalyAtLaunch, eccentricity);
            _splineAnimate.NormalizedTime = E / (2f * Mathf.PI);
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

        /// <summary>
        /// Convierte true anomaly (nu) a eccentric anomaly (E) usando la formula
        /// estandar tan(E/2) = sqrt((1-e)/(1+e)) * tan(nu/2). Devuelve un valor
        /// en [0, 2pi) para que se pueda dividir directamente entre 2pi y usar
        /// como NormalizedTime en SplineAnimate.
        /// </summary>
        private static float TrueToEccentricAnomaly(float nu, float e)
        {
            float twoPi = 2f * Mathf.PI;
            // Atan2 devuelve un angulo en [-pi, pi]; lo desplazamos a [0, 2pi).
            float E = 2f * Mathf.Atan2(
                Mathf.Sqrt(1f - e) * Mathf.Sin(nu * 0.5f),
                Mathf.Sqrt(1f + e) * Mathf.Cos(nu * 0.5f));
            if (E < 0f) E += twoPi;
            // nu puede venir > 2pi (caso rdotv < 0 en OrbitalLauncher); normaliza.
            return E % twoPi;
        }

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