using UnityEngine;

namespace _Project.Scripts.Interaction
{
    /// <summary>
    /// Lightweight data component. Add to each planet GameObject and fill in the
    /// scene name to load when the player clicks (selects) the planet.
    ///
    /// Read by <see cref="PlanetClickTeleporter"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ProyectoVR/Interaction/Planet Scene Link")]
    public sealed class PlanetSceneLink : MonoBehaviour
    {
        [Tooltip("Exact scene name as registered in Build Settings (e.g. 'Tierra', 'Marte', 'Jupiter').")]
        public string sceneName;
    }
}
