using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Goes in: nowhere (it starts by itself, ONLY in a phone build; on PC and in the Editor it does nothing).
// Makes the game run smoothly on Android phones by drawing a bit less:
//   - draws the 3D picture at a lower resolution (text and buttons stay sharp), no HDR, no anti-aliasing
//   - shorter, hard (cheaper) shadows; lamps and candles never cast shadows; at most 2 lamps light each object
//   - the camera does not draw very far away objects
//   - heavy screen effects (bloom quality, depth of field, motion blur, film grain, lens flare) are switched off
// Lights, cameras and effects made later (cutscenes, the candle...) are checked again every 2 seconds.
public class MobilePerformance : MonoBehaviour
{
    const float RenderScale = 0.7f;           // 70% resolution for the 3D view
    const float ShadowDistance = 18f;
    const float ViewDistance = 90f;           // the whole house and yard still fit in this

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
#if UNITY_EDITOR || !(UNITY_ANDROID || UNITY_IOS)
        return;
#else
        if (!Application.isMobilePlatform) return;
#endif
#pragma warning disable CS0162
        TunePipeline();
        GameObject g = new GameObject("MobilePerformance");
        DontDestroyOnLoad(g);
        g.AddComponent<MobilePerformance>();
#pragma warning restore CS0162
    }

    static void TunePipeline()
    {
        QualitySettings.vSyncCount = 0;                       // phones ignore V-Sync; the frame rate below is used instead
        Application.targetFrameRate = 60;
        QualitySettings.lodBias = 0.6f;

        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) return;
        urp.renderScale = RenderScale;
        urp.msaaSampleCount = 1;
        urp.supportsHDR = false;
        urp.shadowDistance = ShadowDistance;
        urp.shadowCascadeCount = 1;
        urp.maxAdditionalLightsCount = 2;
    }

    void Start() { InvokeRepeating(nameof(TuneScene), 0f, 2f); }

    void TuneScene()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Directional) { if (l.shadows == LightShadows.Soft) l.shadows = LightShadows.Hard; }
            else if (l.shadows != LightShadows.None) l.shadows = LightShadows.None;
        }

        foreach (Camera c in Camera.allCameras)
        {
            if (c.farClipPlane > ViewDistance) c.farClipPlane = ViewDistance;
            c.allowMSAA = false;
            c.allowHDR = false;
        }

        foreach (Volume v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (v.sharedProfile == null && v.profile == null) continue;
            VolumeProfile p = v.HasInstantiatedProfile() ? v.profile : v.sharedProfile;   // the build's copy, never saved
            Bloom bloom;
            if (p.TryGet(out bloom)) bloom.highQualityFiltering.Override(false);
            Off<DepthOfField>(p);
            Off<MotionBlur>(p);
            Off<FilmGrain>(p);
            Off<ScreenSpaceLensFlare>(p);
            Off<ChromaticAberration>(p);
        }
    }

    static void Off<T>(VolumeProfile p) where T : VolumeComponent
    {
        T effect;
        if (p.TryGet(out effect) && effect.active) effect.active = false;
    }
}
