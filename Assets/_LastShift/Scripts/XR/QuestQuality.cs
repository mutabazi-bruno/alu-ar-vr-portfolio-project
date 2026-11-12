using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastShift.XR
{
    /// <summary>
    /// On a standalone headset build (Quest), Unity picks the Mobile quality level, which renders
    /// at 80% resolution with no anti-aliasing: soft and jagged up close. This sets full resolution
    /// and 4x MSAA for the headset only, at runtime, so the shared URP assets stay untouched.
    /// </summary>
    static class QuestQuality
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Apply()
        {
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = 1f;
                urp.msaaSampleCount = 4;
            }
            UnityEngine.XR.XRSettings.eyeTextureResolutionScale = 1f;
            Debug.Log("QuestQuality: full resolution, 4x MSAA");
        }
#endif
    }
}
