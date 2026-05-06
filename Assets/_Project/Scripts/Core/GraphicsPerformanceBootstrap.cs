using UnityEngine;

namespace _Project.Scripts.Core
{
    [AddComponentMenu("")]
    public static class GraphicsPerformanceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureDesktopRuntime()
        {
            if (!IsDesktopRuntime())
                return;

            int pcQualityIndex = System.Array.IndexOf(QualitySettings.names, "PC");
            if (pcQualityIndex >= 0 && QualitySettings.GetQualityLevel() != pcQualityIndex)
                QualitySettings.SetQualityLevel(pcQualityIndex, applyExpensiveChanges: true);

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }

        private static bool IsDesktopRuntime()
        {
            RuntimePlatform platform = Application.platform;
            return platform == RuntimePlatform.WindowsPlayer
                || platform == RuntimePlatform.WindowsEditor
                || platform == RuntimePlatform.LinuxPlayer
                || platform == RuntimePlatform.LinuxEditor
                || platform == RuntimePlatform.OSXPlayer
                || platform == RuntimePlatform.OSXEditor;
        }
    }
}
