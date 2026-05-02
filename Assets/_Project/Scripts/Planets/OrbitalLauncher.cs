using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Detecta cuando el jugador suelta un planeta, calcula los elementos orbitales
/// kepleranos y los pasa a OrbitalMover.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("ProyectoVR/Interaction/Orbital Launcher")]
[RequireComponent(typeof(OrbitalMover))]
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public sealed class OrbitalLauncher : MonoBehaviour
{
    #region Constants

    private const float TWO_PI = 2f * Mathf.PI;
    private const float MIN_RADIUS = 0.01f;
    private const float MIN_SPEED = 0.01f;

    #endregion

    #region Inspector

    [Header("References")]
    [Tooltip("Transform del Sol.")]
    [SerializeField] private Transform _sunTransform;

    [Header("Orbital Parameters")]
    [Tooltip("Si es true calcula GM automaticamente para garantizar orbita estable.")]
    [SerializeField] private bool _autoComputeGM = true;

    [Tooltip("GM manual. Solo se usa si Auto Compute GM es false.")]
    [SerializeField] private float _sunGM = 100f;

    [Tooltip("Si es true mantiene la orbita en el plano XZ.")]
    [SerializeField] private bool _forceXZPlane = true;

    #endregion

    #region Cached Components

    private OrbitalMover _orbitalMover;
    private XRGrabInteractable _grabInteractable;
    private Rigidbody _rigidbody;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        _orbitalMover = GetComponent<OrbitalMover>();
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rigidbody = GetComponent<Rigidbody>();
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
        Debug.Log("[OrbitalLauncher] Planet grabbed -- orbit stopped.");
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        Vector3 releasePosition = transform.position;
        Vector3 releaseVelocity = _rigidbody.linearVelocity;
        _rigidbody.isKinematic = true;
        ComputeAndBeginOrbit(releasePosition, releaseVelocity);
    }

    private void ComputeAndBeginOrbit(Vector3 worldPosition, Vector3 worldVelocity)
    {
        Vector3 sunPos = _sunTransform.position;
        Vector3 r = worldPosition - sunPos;
        float rMag = Mathf.Max(r.magnitude, MIN_RADIUS);

        if (_forceXZPlane)
        {
            r = new Vector3(r.x, 0f, r.z);
            rMag = Mathf.Max(r.magnitude, MIN_RADIUS);
            worldVelocity = new Vector3(worldVelocity.x, 0f, worldVelocity.z);
        }

        float vMag = worldVelocity.magnitude;

        // GM: automatico o manual
        float gm = _autoComputeGM ? vMag * vMag * rMag : _sunGM;

        // Si velocidad muy baja, calcular orbita circular perfecta
        if (vMag < MIN_SPEED)
        {
            gm = _sunGM > 0 ? _sunGM : rMag * rMag;
            worldVelocity = CircularVelocity(r, rMag, gm);
            vMag = worldVelocity.magnitude;
            gm = _autoComputeGM ? vMag * vMag * rMag : _sunGM;
            Debug.Log("[OrbitalLauncher] Velocidad baja -- usando orbita circular.");
        }

        Vector3 v = worldVelocity;
        Vector3 hVec = Vector3.Cross(r, v);
        if (hVec.magnitude < 1e-5f)
        {
            v = CircularVelocity(r, rMag, gm);
            hVec = Vector3.Cross(r, v);
            vMag = v.magnitude;
        }

        Vector3 orbitNormal = hVec.normalized;

        float energy = 0.5f * vMag * vMag - gm / rMag;
        if (energy >= 0f)
        {
            gm = vMag * vMag * rMag * 1.1f;
            energy = 0.5f * vMag * vMag - gm / rMag;
        }

        float semiMajorAxis = -gm / (2f * energy);

        float rdotv = Vector3.Dot(r, v);
        Vector3 eVec = (1f / gm) * ((vMag * vMag - gm / rMag) * r - rdotv * v);
        float eccentricity = Mathf.Clamp(eVec.magnitude, 0f, 0.99f);

        Vector3 periapsisDirection = eccentricity > 0.001f
            ? eVec.normalized
            : GetArbitraryPerpendicular(orbitNormal);

        float orbitalPeriod = TWO_PI * Mathf.Sqrt(semiMajorAxis * semiMajorAxis * semiMajorAxis / gm);

        float cosNu0 = eccentricity > 0.001f
            ? Mathf.Clamp(Vector3.Dot(eVec.normalized, r.normalized), -1f, 1f)
            : 0f;
        float nu0 = Mathf.Acos(cosNu0);
        if (rdotv < 0f) nu0 = TWO_PI - nu0;

        // Nota: SetOrbitalElements ya no recibe argumentOfPeriapsis porque
        // periapsisDirection lo encapsula todo
        _orbitalMover.SetOrbitalElements(
            semiMajorAxis,
            eccentricity,
            orbitalPeriod,
            nu0,
            orbitNormal,
            periapsisDirection);

        Debug.Log($"[OrbitalLauncher] Released -- r={rMag:F2} v={vMag:F2} GM={gm:F2} a={semiMajorAxis:F2} e={eccentricity:F3} T={orbitalPeriod:F1}s.");
    }

    private Vector3 CircularVelocity(Vector3 relativePosition, float radius, float gm)
    {
        float speed = Mathf.Sqrt(gm / radius);
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
            Debug.LogWarning("[OrbitalLauncher] _sunTransform is not assigned.", this);
        if (_orbitalMover == null)
            Debug.LogWarning("[OrbitalLauncher] OrbitalMover is not assigned.", this);
        if (_grabInteractable == null)
            Debug.LogWarning("[OrbitalLauncher] XRGrabInteractable is not assigned.", this);
        if (_rigidbody == null)
            Debug.LogWarning("[OrbitalLauncher] Rigidbody is not assigned.", this);
    }

    #endregion
}
