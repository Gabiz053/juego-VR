using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>
    /// Rotates this GameObject every frame so it always faces the player camera.
    /// Attach to any world-space label, sign, or icon that should be readable from any angle.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Core/BillboardFace")]
    public sealed class BillboardFace : MonoBehaviour
    {
        #region Constants -------------------------------------------------------

        private const string LOG_TAG = "[BillboardFace]";

        #endregion

        #region Inspector -------------------------------------------------------

        [Header("Axis")]
        [Tooltip("When enabled the object only rotates around the Y axis so text stays upright. When disabled it tilts to fully face the camera (useful for labels on the ceiling or floor).")]
        [SerializeField] private bool _lockYAxis = true;

        #endregion

        #region Events ----------------------------------------------------------
        // No events.
        #endregion

        #region Cached Components -----------------------------------------------

        private Camera _mainCamera;

        #endregion

        #region Public API ------------------------------------------------------
        // No public API.
        #endregion

        #region Unity Lifecycle -------------------------------------------------

        private void Start()
        {
            _mainCamera = Camera.main;
            ValidateReferences();
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null) return;

            // Direction FROM camera TO this object — makes forward point AWAY from camera
            // so the text's readable face (-Z side in TMP 3D) faces the player.
            var dir = transform.position - _mainCamera.transform.position;

            if (_lockYAxis)
                dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f) return;

            transform.rotation = Quaternion.LookRotation(dir);
        }

        #endregion

        #region Internals -------------------------------------------------------
        // No internals.
        #endregion

        #region Validation ------------------------------------------------------

        private void ValidateReferences()
        {
            if (_mainCamera == null)
                Debug.LogWarning($"{LOG_TAG} Camera.main not found -- will retry each frame.", this);
        }

        #endregion
    }
}
