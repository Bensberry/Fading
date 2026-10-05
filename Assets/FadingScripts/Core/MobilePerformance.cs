using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Goes in: nowhere (it starts by itself, ONLY in a phone build; on PC and in the Editor it does nothing).
// Makes the game run smoothly on Android phones by drawing a bit less:
//   - draws the 3D picture at 85% resolution, less on very big screens like tablets (text and buttons stay sharp), no anti-aliasing
//   - shadows as far as on PC (25 m); lamps and candles never cast shadows; at most 3 lamps light each object
//   - the camera does not draw very far away objects
//   - heavy screen effects (bloom quality, depth of field, motion blur, film grain, lens flare) are switched off
// Lights, cameras and effects made later (cutscenes, the candle...) are checked again every 2 seconds.
public class MobilePerformance : MonoBehaviour
{
    const float RenderScale = 0.85f;          // 85% resolution for the 3D view (a phone screen is small and sharp)
    const float MaxMegapixels = 3.3f;         // tablets (e.g. 3000 x 2120): at most about this many pixels are drawn
    const float ShadowDistance = 25f;          // the same as on PC
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

        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) return;
        urp.renderScale = ScaleFor(Screen.width, Screen.height);
        urp.msaaSampleCount = 1;
        urp.shadowDistance = ShadowDistance;
        urp.shadowCascadeCount = 1;
        urp.maxAdditionalLightsCount = 3;
    }

    // 85% on a phone; on a huge tablet screen a bit less, so the picture never has more than MaxMegapixels.
    static float ScaleFor(int width, int height)
    {
        float megapixels = width * (float)height / 1000000f;
        if (megapixels <= 0f) return RenderScale;
        return Mathf.Clamp(Mathf.Sqrt(MaxMegapixels / megapixels), 0.65f, RenderScale);
    }

    void Start() { InvokeRepeating(nameof(TuneScene), 0f, 2f); }

    void TuneScene()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional && l.shadows != LightShadows.None) l.shadows = LightShadows.None;
        }

        foreach (Camera c in Camera.allCameras)
        {
            if (c.farClipPlane > ViewDistance) c.farClipPlane = ViewDistance;
            c.allowMSAA = false;
        }

        foreach (Volume v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            VolumeProfile p = v.HasInstantiatedProfile() ? v.profile : v.sharedProfile;   // the build's copy, never saved
            if (p == null) continue;
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
