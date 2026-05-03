using UnityEngine;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Rotates the object to always face the player camera (billboard).
    /// Only the Y-axis is matched so the label stays upright regardless of head tilt.
    /// Attach to the PlanetLabelPivot of each planet.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Billboard Label")]
    public class BillboardLabel : MonoBehaviour
    {
        #region Constants

        private const string LOG_TAG = "[BillboardLabel]";

        #endregion

        #region Inspector
        #endregion

        #region Events
        #endregion

        #region Cached Components

        private Camera _mainCamera;

        #endregion

        #region Public API
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            ValidateReferences();
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            transform.rotation = Quaternion.Euler(
                0f,
                _mainCamera.transform.eulerAngles.y,
                0f
            );
        }

        #endregion

        #region Internals
        #endregion

        #region Validation

        private void ValidateReferences()
        {
            if (_mainCamera == null)
                Debug.LogWarning($"{LOG_TAG} No Main Camera found -- billboard will not rotate.", this);
        }

        #endregion
    }
}
