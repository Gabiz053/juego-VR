using TMPro;
using UnityEngine;
using _Project.Scripts.Core;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Casts a ray from the right hand. When it hits an object with a PlanetProxy,
    /// the PlanetDataCard and the floating 3-D name label are shown.
    /// GetComponent is only called when the ray target changes, never every frame.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Planet Pointer")]
    public class PlanetPointer : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[PlanetPointer]";

        #endregion

        #region Inspector

        [Header("References")]
        [Tooltip("Data panel that floats near the hand.")]
        [SerializeField] private PlanetDataCard _dataCard;

        [Header("Raycast")]
        [Tooltip("Maximum raycast distance in metres.")]
        [SerializeField] private float _rayDistance = 100f;

        [Tooltip("Sphere radius used for hit detection. Higher values make aiming easier.")]
        [SerializeField] private float _rayRadius = 0.08f;

        [Tooltip("Layer mask the ray tests against. Leave Everything if no specific layer is set.")]
        [SerializeField] private LayerMask _layerMask = ~0;

        [Tooltip("Extra distance penalty applied to satellites when selecting a target. Helps prioritize parent planets (e.g., Earth over Moon).")]
        [SerializeField, Min(0f)] private float _satelliteSelectionPenalty = 3f;

        #endregion

        #region Events
        #endregion

        #region Cached Components
        #endregion

        #region State

        private GameObject       _currentTarget;
        private TextMeshProUGUI  _currentLabel;
        private GameObject       _currentPlanetLabel;
        private readonly RaycastHit[] _raycastHits = new RaycastHit[32];

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            Debug.Log($"{LOG_TAG} Initialized.");
        }

        private void Update()
        {
            CastRay();
        }

        #endregion

        #region Internals

        private void CastRay()
        {
            Ray ray = new Ray(transform.position, transform.forward);
            int hitCount = _rayRadius > 0f
                ? Physics.SphereCastNonAlloc(
                    ray,
                    _rayRadius,
                    _raycastHits,
                    _rayDistance,
                    _layerMask,
                    QueryTriggerInteraction.Collide)
                : Physics.RaycastNonAlloc(
                    ray,
                    _raycastHits,
                    _rayDistance,
                    _layerMask,
                    QueryTriggerInteraction.Collide);

            PlanetProxy nearestProxy = null;
            GameObject nearestPlanetObject = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _raycastHits[i];
                GameObject hitObject = hit.collider.gameObject;

                if (!TryGetPlanetProxy(hitObject, out PlanetProxy proxy))
                    continue;

                float score = hit.distance;
                if (HasParentPlanetProxy(proxy))
                    score += _satelliteSelectionPenalty;

                if (score >= bestScore)
                    continue;

                bestScore = score;
                nearestProxy = proxy;
                nearestPlanetObject = proxy.gameObject;
            }

            if (nearestProxy != null)
            {
                if (_currentTarget == nearestPlanetObject)
                    return;

                HideCurrentLabel();
                _currentTarget = nearestPlanetObject;

                Canvas canvas = GetLabelCanvasForProxy(nearestProxy);
                if (canvas != null)
                {
                    _currentPlanetLabel = canvas.gameObject;
                    _currentPlanetLabel.SetActive(true);

                    TextMeshProUGUI label = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (label != null)
                    {
                        label.text   = nearestProxy.PlanetName;
                        _currentLabel = label;
                    }
                }

                AudioManager.Instance?.PlayUIHover();

                if (_dataCard != null)
                    _dataCard.UpdateData(nearestProxy);
                Debug.Log($"{LOG_TAG} Pointing at planet: {_currentTarget.name}.");
                return;
            }

            if (_currentTarget != null)
            {
                HideCurrentLabel();
                _currentTarget = null;
                if (_dataCard != null)
                    _dataCard.Hide();
                Debug.Log($"{LOG_TAG} No planet targeted.");
            }
        }

        private void HideCurrentLabel()
        {
            if (_currentPlanetLabel != null)
            {
                _currentPlanetLabel.SetActive(false);
                _currentPlanetLabel = null;
            }
            _currentLabel  = null;
            _currentTarget = null;
        }

        private static bool TryGetPlanetProxy(GameObject hitObject, out PlanetProxy proxy)
        {
            proxy = hitObject.GetComponent<PlanetProxy>();
            if (proxy != null)
                return true;

            proxy = hitObject.GetComponentInParent<PlanetProxy>();
            return proxy != null;
        }

        private static bool HasParentPlanetProxy(PlanetProxy proxy)
        {
            if (proxy == null || proxy.transform.parent == null)
                return false;

            PlanetProxy parentProxy = proxy.transform.parent.GetComponentInParent<PlanetProxy>();
            return parentProxy != null;
        }

        private static Canvas GetLabelCanvasForProxy(PlanetProxy proxy)
        {
            if (proxy == null)
                return null;

            Canvas[] canvases = proxy.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null)
                    continue;

                PlanetProxy ownerProxy = canvas.GetComponentInParent<PlanetProxy>();
                if (ownerProxy == proxy)
                    return canvas;
            }

            return null;
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_dataCard == null)
                Debug.LogWarning($"{LOG_TAG} _dataCard is not assigned.", this);
            if (_rayDistance <= 0f)
                Debug.LogWarning($"{LOG_TAG} _rayDistance should be greater than zero.", this);
            if (_rayRadius < 0f)
                Debug.LogWarning($"{LOG_TAG} _rayRadius should not be negative.", this);
            if (_satelliteSelectionPenalty < 0f)
                Debug.LogWarning($"{LOG_TAG} _satelliteSelectionPenalty should not be negative.", this);
        }

        #endregion
    }
}
