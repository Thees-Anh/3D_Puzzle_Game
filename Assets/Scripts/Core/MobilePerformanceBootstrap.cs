using UnityEngine;

namespace PuzzleRoom.Core
{
    public static class MobilePerformanceBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            if (!Application.isMobilePlatform) return;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}
