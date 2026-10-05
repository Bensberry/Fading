using UnityEngine;

// Goes in: nowhere (it runs by itself when the game starts).
// Small settings that make the game run smoother without changing how it looks:
//   - V-Sync on: the game no longer renders hundreds of unneeded frames per second (which heats the GPU and stutters)
public static class PerformanceTuner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        if (Application.isMobilePlatform) return;            // phones: MobilePerformance sets 60 frames per second instead
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = -1;
    }
}
