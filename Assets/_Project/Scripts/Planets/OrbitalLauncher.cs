using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace _Project.Scripts.Planets
{
    /// <summary>
    /// Detects when the player releases a planet, computes Keplerian orbital elements
    /// from the release position and velocity, and forwards them to OrbitalMover.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Planets/Orbital Launcher")]
    [RequireComponent(typeof(OrbitalMover))]
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class OrbitalLauncher : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[OrbitalLauncher]";
        private const float TWO_PI = 2f * Mathf.PI;
        private const float MIN_RADIUS = 0.01f;
        private const float MIN_SPEED = 0.01f;

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Transform of the Sun (orbit focus).")]
        [SerializeField] private Transform _sunTransform;

        [Header("Orbital Parameters")]
        [Tooltip("If true, computes GM automatically from release velocity to guarantee a stable orbit.")]
        [SerializeField] private bool _autoComputeGM = true;

        [Tooltip("Manual GM value. Only used when Auto Compute GM is disabled.")]
        [SerializeField] private float _sunGM = 100f;

        [Tooltip("If true, constrains the orbit to the XZ plane (ignores vertical velocity).")]
        [SerializeField] private bool _forceXZPlane = true;

        #endregion

        #region Events
        #endregion

        #region Cached Components

        private OrbitalMover       _orbitalMover;
        private XRGrabInteractable _grabInteractable;
        private Rigidbody          _rigidbody;

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _orbitalMover     = GetComponent<OrbitalMover>();
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _rigidbody        = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            ValidateReferences();
            _grabInteractable.selectEntered.AddListener(OnGrabbed);
            _grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void OnDestroy()
        {
            if (_grabInteractable == null) return;
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        #endregion

        #region Internals

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            _orbitalMover.StopOrbit();
            _rigidbody.isKinematic = false;
            Debug.Log($"{LOG_TAG} Planet grabbed -- orbit stopped.");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            Vector3 releasePosition = transform.position;
            Vector3 releaseVelocity = _rigidbody.linearVelocity;
            _rigidbody.isKinematic  = true;
            ComputeAndBeginOrbit(releasePosition, releaseVelocity);
        }

        private void ComputeAndBeginOrbit(Vector3 worldPosition, Vector3 worldVelocity)
        {
            Vector3 sunPos = _sunTransform.position;
            Vector3 r      = worldPosition - sunPos;
            float   rMag   = Mathf.Max(r.magnitude, MIN_RADIUS);

            if (_forceXZPlane)
            {
                r             = new Vector3(r.x, 0f, r.z);
                rMag          = Mathf.Max(r.magnitude, MIN_RADIUS);
                worldVelocity = new Vector3(worldVelocity.x, 0f, worldVelocity.z);
            }

            float vMag = worldVelocity.magnitude;
            float gm   = _autoComputeGM ? vMag * vMag * rMag : _sunGM;

            if (vMag < MIN_SPEED)
            {
                gm            = _sunGM > 0 ? _sunGM : rMag * rMag;
                worldVelocity = CircularVelocity(r, rMag, gm);
                vMag          = worldVelocity.magnitude;
                gm            = _autoComputeGM ? vMag * vMag * rMag : _sunGM;
                Debug.Log($"{LOG_TAG} Low release speed -- computing circular orbit.");
            }

            Vector3 v    = worldVelocity;
            Vector3 hVec = Vector3.Cross(r, v);
            if (hVec.magnitude < 1e-5f)
            {
                v    = CircularVelocity(r, rMag, gm);
                hVec = Vector3.Cross(r, v);
                vMag = v.magnitude;
            }

            Vector3 orbitNormal = hVec.normalized;
            float   energy      = 0.5f * vMag * vMag - gm / rMag;

            if (energy >= 0f)
            {
                gm     = vMag * vMag * rMag * 1.1f;
                energy = 0.5f * vMag * vMag - gm / rMag;
            }

            float   semiMajorAxis = -gm / (2f * energy);
            float   rdotv         = Vector3.Dot(r, v);
            Vector3 eVec          = (1f / gm) * ((vMag * vMag - gm / rMag) * r - rdotv * v);
            float   eccentricity  = Mathf.Clamp(eVec.magnitude, 0f, 0.99f);

            Vector3 periapsisDirection = eccentricity > 0.001f
                ? eVec.normalized
                : GetArbitraryPerpendicular(orbitNormal);

            float orbitalPeriod = TWO_PI * Mathf.Sqrt(semiMajorAxis * semiMajorAxis * semiMajorAxis / gm);

            float cosNu0 = eccentricity > 0.001f
                ? Mathf.Clamp(Vector3.Dot(eVec.normalized, r.normalized), -1f, 1f)
                : 0f;
            float nu0 = Mathf.Acos(cosNu0);
            if (rdotv < 0f) nu0 = TWO_PI - nu0;

            _orbitalMover.SetOrbitalElements(
                semiMajorAxis,
                eccentricity,
                orbitalPeriod,
                nu0,
                orbitNormal,
                periapsisDirection);

            Debug.Log($"{LOG_TAG} Released -- r={rMag:F2} v={vMag:F2} GM={gm:F2} a={semiMajorAxis:F2} e={eccentricity:F3} T={orbitalPeriod:F1}s.");
        }

        private Vector3 CircularVelocity(Vector3 relativePosition, float radius, float gm)
        {
            float   speed   = Mathf.Sqrt(gm / radius);
            Vector3 tangent = _forceXZPlane
                ? new Vector3(-relativePosition.z, 0f, relativePosition.x).normalized
                : Vector3.Cross(Vector3.up, relativePosition).normalized;
            return tangent * speed;
        }

        private static Vector3 GetArbitraryPerpendicular(Vector3 v)
        {
            Vector3 candidate = Mathf.Abs(v.x) < 0.9f ? Vector3.right : Vector3.up;
            return Vector3.Cross(v, candidate).normalized;
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_sunTransform == null)
                Debug.LogWarning($"{LOG_TAG} _sunTransform is not assigned.", this);
            if (_orbitalMover == null)
                Debug.LogWarning($"{LOG_TAG} _orbitalMover is not assigned.", this);
            if (_grabInteractable == null)
                Debug.LogWarning($"{LOG_TAG} _grabInteractable is not assigned.", this);
            if (_rigidbody == null)
                Debug.LogWarning($"{LOG_TAG} _rigidbody is not assigned.", this);
        }

        #endregion
    }
}
