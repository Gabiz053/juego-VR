using System;
using System.Collections.Generic;
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

        #region Events

        /// <summary>Se dispara cuando el planeta entra en orbita. Pasa semiMajorAxis y orbitalPeriod.</summary>
        public event System.Action<float, float> OnOrbitLaunched;

        #endregion
        #region Constants

        private const string LOG_TAG = "[OrbitalLauncher]";
        private const float TWO_PI = 2f * Mathf.PI;
        private const float MIN_RADIUS = 0.01f;
        private const float MIN_SPEED = 0.01f;
        private const float MIN_GM = 0.001f;
        private const float MIN_SQR_MAGNITUDE = 1e-6f;

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

        [Tooltip("If true, constrains the orbit to the XZ plane (ignores height and vertical velocity). " +
                 "Disable this to preserve the real 3D release position and allow orbits above/below the floor.")]
        [SerializeField] private bool _forceXZPlane = false;

        [Header("Grab Interaction")]
        [Tooltip("If enabled, multiple interactors can hold the planet simultaneously (two-hand grab).")]
        [SerializeField] private bool _allowTwoHandGrab = true;

        [Tooltip("If enabled, while grabbed the planet ignores collision response so it can pass through the crystal shell.")]
        [SerializeField] private bool _allowPassThroughWhileGrabbed = true;

        #endregion

        #region Cached Components

        private OrbitalMover       _orbitalMover;
        private XRGrabInteractable _grabInteractable;
        private Rigidbody          _rigidbody;
        private Renderer           _sunRenderer;
        private readonly List<Collider> _planetColliders = new();
        private readonly List<bool> _colliderIsTriggerDefaults = new();
        private bool _rigidbodyDetectCollisionsDefault;
        private bool _hasRigidbodyDetectCollisionsDefault;

        #endregion

        #region State

        private bool    _isGrabbed;
        private Vector3 _previousPosition;
        private Vector3 _releaseVelocity;

        #endregion


        #region Unity Lifecycle

        private void Awake()
        {
            _orbitalMover     = GetComponent<OrbitalMover>();
            _grabInteractable = GetComponent<XRGrabInteractable>();
            _rigidbody        = GetComponent<Rigidbody>();
            CachePlanetColliders();
        }

        private void Start()
        {
            TryResolveSunReference();
            CacheSunRenderer();
            ConfigureGrabInteraction();
            ValidateReferences();

            _previousPosition      = transform.position;

            if (_grabInteractable == null) return;
            _grabInteractable.selectEntered.AddListener(OnGrabbed);
            _grabInteractable.selectExited.AddListener(OnReleased);
        }

        private void Update()
        {
            if (_isGrabbed)
                _releaseVelocity = (transform.position - _previousPosition) / Time.deltaTime;

            _previousPosition = transform.position;
        }

        private void OnDestroy()
        {
            _isGrabbed = false;
            SetPassThroughWhileGrabbed(false);

            if (_grabInteractable == null) return;
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        #endregion

        #region Internals

        private void OnGrabbed(SelectEnterEventArgs args)
        {
            _ = args;
            _isGrabbed    = true;
            _releaseVelocity = Vector3.zero;
            SetPassThroughWhileGrabbed(true);

            if (_orbitalMover != null)
                _orbitalMover.StopOrbit();

            Debug.Log($"{LOG_TAG} Planet grabbed -- orbit stopped.");
        }

        private void OnReleased(SelectExitEventArgs args)
        {
            _ = args;
            if (_grabInteractable != null && _grabInteractable.isSelected)
            {
                Debug.Log($"{LOG_TAG} Release received but object is still held by another hand -- keeping grab state.");
                return;
            }

            _isGrabbed = false;
            SetPassThroughWhileGrabbed(false);

            if (_sunTransform == null || _orbitalMover == null)
            {
                Debug.LogWarning($"{LOG_TAG} Missing references -- orbit launch aborted.", this);
                return;
            }

            ComputeAndBeginOrbit(transform.position, _releaseVelocity);
        }

        private void ComputeAndBeginOrbit(Vector3 worldPosition, Vector3 worldVelocity)
        {
            Vector3 sunPos = GetSunFocusPosition();
            Vector3 r      = worldPosition - sunPos;
            float   rMag   = Mathf.Max(r.magnitude, MIN_RADIUS);

            if (_forceXZPlane)
            {
                r             = new Vector3(r.x, 0f, r.z);
                rMag          = Mathf.Max(r.magnitude, MIN_RADIUS);
                worldVelocity = new Vector3(worldVelocity.x, 0f, worldVelocity.z);
            }

            float vMag = worldVelocity.magnitude;
            float gm   = _autoComputeGM
                ? Mathf.Max(vMag * vMag * rMag, MIN_GM)
                : Mathf.Max(_sunGM, MIN_GM);

            if (vMag < MIN_SPEED)
            {
                gm            = Mathf.Max(_sunGM > 0f ? _sunGM : rMag * rMag, MIN_GM);
                worldVelocity = CircularVelocity(r, rMag, gm);
                vMag          = worldVelocity.magnitude;
                gm            = _autoComputeGM
                    ? Mathf.Max(vMag * vMag * rMag, MIN_GM)
                    : Mathf.Max(_sunGM, MIN_GM);
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
                gm     = Mathf.Max(vMag * vMag * rMag * 1.1f, MIN_GM);
                energy = 0.5f * vMag * vMag - gm / rMag;
            }

            float   semiMajorAxis = Mathf.Max(-gm / (2f * energy), MIN_RADIUS);
            float   rdotv         = Vector3.Dot(r, v);
            Vector3 eVec          = (1f / gm) * ((vMag * vMag - gm / rMag) * r - rdotv * v);
            float   eccentricity  = Mathf.Clamp(eVec.magnitude, 0f, 0.99f);

            // Para orbitas casi circulares (el caso "el jugador suelta el planeta
            // sin lanzarlo") el vector excentricidad es ~0 y su direccion es
            // numericamente inestable. Usamos la direccion radial del punto de
            // soltado como direccion de "periapsis" -- de esta forma el planeta
            // empieza la orbita exactamente en el punto donde se solto (nu0 = 0).
            Vector3 periapsisDirection = eccentricity > 0.001f
                ? eVec.normalized
                : r.normalized;

            float orbitalPeriod = TWO_PI * Mathf.Sqrt(semiMajorAxis * semiMajorAxis * semiMajorAxis / gm);

            // Para circular: nu0 = 0 (planeta en periapsis = punto de soltado).
            // Para elipses: angulo entre la direccion al planeta y la direccion al periapsis.
            float cosNu0 = eccentricity > 0.001f
                ? Mathf.Clamp(Vector3.Dot(eVec.normalized, r.normalized), -1f, 1f)
                : 1f;
            float nu0 = Mathf.Acos(cosNu0);
            if (rdotv < 0f) nu0 = TWO_PI - nu0;

            _orbitalMover.SetOrbitalElements(
                semiMajorAxis,
                eccentricity,
                orbitalPeriod,
                nu0,
                orbitNormal,
                periapsisDirection);

            OnOrbitLaunched?.Invoke(semiMajorAxis, orbitalPeriod);

            Debug.Log($"{LOG_TAG} Released -- r={rMag:F2} v={vMag:F2} GM={gm:F2} a={semiMajorAxis:F2} e={eccentricity:F3} T={orbitalPeriod:F1}s.");


        }

        private Vector3 CircularVelocity(Vector3 relativePosition, float radius, float gm)
        {
            float   speed   = Mathf.Sqrt(gm / radius);
            Vector3 tangent = _forceXZPlane
                ? new Vector3(-relativePosition.z, 0f, relativePosition.x).normalized
                : Vector3.Cross(Vector3.up, relativePosition).normalized;

            if (tangent.sqrMagnitude < MIN_SQR_MAGNITUDE)
                tangent = Vector3.Cross(Vector3.right, relativePosition).normalized;

            if (tangent.sqrMagnitude < MIN_SQR_MAGNITUDE)
                tangent = Vector3.forward;

            return tangent * speed;
        }

        private void ConfigureGrabInteraction()
        {
            if (_grabInteractable == null)
                return;

            if (_allowTwoHandGrab)
                TrySetEnumMember(_grabInteractable, "selectMode", "Multiple");

            if (_allowPassThroughWhileGrabbed)
                TrySetEnumMember(_grabInteractable, "movementType", "Instantaneous");
        }

        private static bool TrySetEnumMember(object target, string memberName, string enumValueName)
        {
            if (target == null)
                return false;

            Type targetType = target.GetType();

            var propertyInfo = targetType.GetProperty(memberName);
            if (propertyInfo != null && propertyInfo.CanWrite && propertyInfo.PropertyType.IsEnum)
                return TryAssignEnumValue(propertyInfo.PropertyType, enumValueName, value => propertyInfo.SetValue(target, value));

            var fieldInfo = targetType.GetField(memberName);
            if (fieldInfo != null && fieldInfo.FieldType.IsEnum)
                return TryAssignEnumValue(fieldInfo.FieldType, enumValueName, value => fieldInfo.SetValue(target, value));

            return false;
        }

        private static bool TryAssignEnumValue(Type enumType, string enumValueName, Action<object> setter)
        {
            string[] names = Enum.GetNames(enumType);
            for (int i = 0; i < names.Length; i++)
            {
                if (!string.Equals(names[i], enumValueName, StringComparison.Ordinal))
                    continue;

                object parsedValue = Enum.Parse(enumType, enumValueName);
                setter(parsedValue);
                return true;
            }

            return false;
        }

        private void CachePlanetColliders()
        {
            _planetColliders.Clear();
            _colliderIsTriggerDefaults.Clear();

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                _planetColliders.Add(collider);
                _colliderIsTriggerDefaults.Add(collider.isTrigger);
            }

            if (_rigidbody != null)
            {
                _rigidbodyDetectCollisionsDefault = _rigidbody.detectCollisions;
                _hasRigidbodyDetectCollisionsDefault = true;
            }
        }

        private void SetPassThroughWhileGrabbed(bool enabled)
        {
            if (!_allowPassThroughWhileGrabbed)
                return;

            for (int i = 0; i < _planetColliders.Count; i++)
            {
                Collider collider = _planetColliders[i];
                if (collider == null)
                    continue;

                bool defaultIsTrigger = _colliderIsTriggerDefaults[i];
                collider.isTrigger = enabled || defaultIsTrigger;
            }

            if (_rigidbody != null)
            {
                if (!_hasRigidbodyDetectCollisionsDefault)
                {
                    _rigidbodyDetectCollisionsDefault = _rigidbody.detectCollisions;
                    _hasRigidbodyDetectCollisionsDefault = true;
                }

                _rigidbody.detectCollisions = enabled ? false : _rigidbodyDetectCollisionsDefault;
            }
        }

        private void TryResolveSunReference()
        {
            if (_sunTransform != null) return;

            Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            Transform fallback = null;

            for (int i = 0; i < sceneTransforms.Length; i++)
            {
                Transform candidate = sceneTransforms[i];
                string candidateName = candidate.name.ToLowerInvariant();
                bool isLikelySun =
                    candidateName == "sun" ||
                    candidateName == "sol" ||
                    candidateName.Contains("sun") ||
                    candidateName.Contains("sol");

                if (!isLikelySun) continue;

                if (candidate.GetComponentInChildren<Renderer>() != null)
                {
                    _sunTransform = candidate;
                    Debug.Log($"{LOG_TAG} Auto-assigned _sunTransform: {_sunTransform.name}.");
                    return;
                }

                if (fallback == null)
                    fallback = candidate;
            }

            if (fallback != null)
            {
                _sunTransform = fallback;
                Debug.Log($"{LOG_TAG} Auto-assigned fallback _sunTransform: {_sunTransform.name}.");
            }
        }

        private void CacheSunRenderer()
        {
            _sunRenderer = _sunTransform != null ? _sunTransform.GetComponentInChildren<Renderer>() : null;
        }

        private Vector3 GetSunFocusPosition()
        {
            if (_sunTransform == null) return Vector3.zero;
            if (_sunRenderer == null) CacheSunRenderer();
            return _sunRenderer != null ? _sunRenderer.bounds.center : _sunTransform.position;
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
