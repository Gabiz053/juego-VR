using UnityEngine;

namespace _Project.Scripts.Core
{
    /// <summary>Datos estaticos cross-escena de bajo acoplamiento.</summary>
    public static class SessionContext
    {
        public static Vector3 MainMenuSpawnPosition { get; set; } = Vector3.zero;
        public static Quaternion MainMenuSpawnRotation { get; set; } = Quaternion.identity;
        public static bool HasMainMenuSpawnOverride { get; private set; }

        public static void SetMainMenuSpawn(Vector3 position, Quaternion rotation)
        {
            MainMenuSpawnPosition = position;
            MainMenuSpawnRotation = rotation;
            HasMainMenuSpawnOverride = true;
        }

        public static void ClearMainMenuSpawnOverride()
        {
            HasMainMenuSpawnOverride = false;
        }
    }
}
