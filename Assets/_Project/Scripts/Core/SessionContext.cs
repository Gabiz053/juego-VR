using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>Datos estaticos cross-escena de bajo acoplamiento.</summary>
    public static class SessionContext
    {
        public static Vector3 MainMenuSpawnPosition { get; set; } = Vector3.zero;
        public static Quaternion MainMenuSpawnRotation { get; set; } = Quaternion.identity;
    }
}