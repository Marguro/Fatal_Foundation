using UnityEngine;

namespace Managers
{
    public static class FrameRateBootstrap
    {
        private const int TargetFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyFrameRateLimit()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFrameRate;

#if UNITY_EDITOR
            Debug.Log($"[FrameRateBootstrap] Applied frame cap: target={Application.targetFrameRate}, vSync={QualitySettings.vSyncCount}");
#endif
        }
    }
}


