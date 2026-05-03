using TMPro;
using UnityEngine;
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

        [Tooltip("Layer mask the ray tests against. Leave Everything if no specific layer is set.")]
        [SerializeField] private LayerMask _layerMask = ~0;

        #endregion

        #region Events
        #endregion

        #region State

        private GameObject       _currentTarget;
        private TextMeshProUGUI  _currentLabel;
        private GameObject       _currentPlanetLabel;

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

            if (Physics.Raycast(ray, out RaycastHit hit, _rayDistance, _layerMask))
            {
                GameObject target = hit.collider.gameObject;

                if (_currentTarget == target) return;

                PlanetProxy proxy = target.GetComponent<PlanetProxy>();
                if (proxy != null)
                {
                    HideCurrentLabel();
                    _currentTarget = target;

                    Canvas canvas = target.GetComponentInChildren<Canvas>(true);
                    if (canvas != null)
                    {
                        _currentPlanetLabel = canvas.gameObject;
                        _currentPlanetLabel.SetActive(true);

                        TextMeshProUGUI label = canvas.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (label != null)
                        {
                            label.text   = proxy.PlanetName;
                            _currentLabel = label;
                        }
                    }

                    _dataCard.UpdateData(proxy);
                    Debug.Log($"{LOG_TAG} Pointing at planet: {target.name}.");
                    return;
                }
            }

            if (_currentTarget != null)
            {
                HideCurrentLabel();
                _currentTarget = null;
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

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_dataCard == null)
                Debug.LogWarning($"{LOG_TAG} _dataCard is not assigned.", this);
        }

        #endregion
    }
}
