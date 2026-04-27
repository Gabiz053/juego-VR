using UnityEngine;

namespace _Project.Scripts.UI
{
    /// <summary>
    /// Hace que el objeto mire siempre de frente al jugador (billboard).
    /// Asignar al PlanetLabelPivot de cada planeta.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/UI/Billboard Label")]
    public class BillboardLabel : MonoBehaviour
    {
        #region Unity Lifecycle

        private void LateUpdate()
        {
            if (Camera.main == null) return;

            // Copiar solo la rotacion Y de la camara para que mire al jugador
            // manteniendose vertical (sin inclinarse con la cabeza)
            transform.rotation = Quaternion.Euler(
                0f,
                Camera.main.transform.eulerAngles.y,
                0f
            );
        }

        #endregion
    }
}