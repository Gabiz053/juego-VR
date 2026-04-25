using UnityEngine;
using _Project.Scripts.Interaction;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Lanza un raycast desde la mano derecha. Cuando apunta a un objeto
    /// con tag "Planet", muestra el PlanetDataCard con sus datos.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Planet Pointer")]
    public class PlanetPointer : MonoBehaviour
    {
        #region Inspector

        [Header("References")]
        [Tooltip("El panel de datos que flota en la mano.")]
        [SerializeField] private PlanetDataCard _dataCard;

        [Header("Raycast")]
        [Tooltip("Distancia maxima del raycast en metros.")]
        [SerializeField] private float _rayDistance = 100f;

        [Tooltip("Layer mask contra la que lanza el ray. Dejar en Everything si no tienes layer especifico.")]
        [SerializeField] private LayerMask _layerMask = ~0;

        #endregion

        #region State

        private GameObject _currentTarget;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            ValidateReferences();
            Debug.Log("[PlanetPointer] Initialized.");
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

                if (target.CompareTag("Planet"))
                {
                    if (_currentTarget != target)
                    {
                        _currentTarget = target;

                        PlanetProxy proxy = target.GetComponent<PlanetProxy>();
                        if (proxy != null)
                        {
                            _dataCard.UpdateData(proxy);
                        }

                        Debug.Log($"[PlanetPointer] Pointing at planet: {target.name}.");
                    }
                    return;
                }
            }

            // El ray no golpea ningun planeta
            if (_currentTarget != null)
            {
                _currentTarget = null;
                _dataCard.Hide();
                Debug.Log("[PlanetPointer] No planet targeted.");
            }
        }

        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_dataCard == null)
                Debug.LogWarning("[PlanetPointer] _dataCard is not assigned.", this);
        }

        #endregion
    }
}