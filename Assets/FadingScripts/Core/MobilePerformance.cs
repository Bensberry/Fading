using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Goes in: nowhere (it starts by itself, on every platform).
// OLD PHONE MODE (Settings in the pause menu). OFF = the game looks exactly as designed (the default).
// ON = lighter graphics for older phones, with a GRAPHICS slider (0 Lowest, 1 Low, 2 Medium, 3 High):
//   3 High    85% resolution (less on huge tablet screens), shadows 25 m, glow (bloom), 3 lamps per object, view 90 m
//   2 Medium  ~2.2 megapixels, hard shadows 15 m, glow, 2 lamps per object, view 70 m
//   1 Low     ~1.4 megapixels, no shadows, no glow, 1 lamp per object, view 55 m
//   0 Lowest  ~0.9 megapixels, like Low but no screen effects at all
// On every level: zone culling (ZoneCulling: inside the house the yard is not drawn, outside the rooms are not),
// small yard things (flowers, rocks, bushes) are not drawn far away, no anti-aliasing, lamps and candles cast no
// shadows, depth of field / motion blur / film grain / lens flare off, Mom and Luna are cheaper to animate.
// Switching it OFF puts every original setting back. Phones always aim for 60 frames per second.
public class MobilePerformance : MonoBehaviour
{
    const string ModeKey = "fading_old_phone_mode";
    const string LevelKey = "fading_graphics_level";

    public static readonly string[] LevelNames = { "LOWEST", "LOW", "MEDIUM", "HIGH" };

    public static bool OldPhoneMode
    {
        get { return PlayerPrefs.GetInt(ModeKey, 0) == 1; }
        set { PlayerPrefs.SetInt(ModeKey, value ? 1 : 0); Apply(); }
    }

    public static int Level
    {
        get { return Mathf.Clamp(PlayerPrefs.GetInt(LevelKey, 2), 0, 3); }
        set { PlayerPrefs.SetInt(LevelKey, Mathf.Clamp(value, 0, 3)); Apply(); }
    }

    static readonly float[] Megapixels = { 0.9f, 1.4f, 2.2f, 3.3f };
    static readonly float[] ShadowDistance = { 0f, 0f, 15f, 25f };
    static readonly int[] LampsPerObject = { 1, 1, 2, 3 };
    static readonly float[] ViewDistance = { 50f, 55f, 70f, 90f };
    static readonly float[] SmallThingsDistance = { 14f, 18f, 24f, 32f };     // Playground.SmallThingsLayer

    static MobilePerformance instance;

    // The original settings, so OFF can put them back.
    float renderScale, shadowDistance, lodBias;
    int msaa, cascades, lamps;
    bool captured;
    readonly Dictionary<Light, LightShadows> lightShadows = new Dictionary<Light, LightShadows>();
    readonly Dictionary<Camera, float> farClip = new Dictionary<Camera, float>();
    readonly Dictionary<Camera, bool> postProcessing = new Dictionary<Camera, bool>();
    readonly HashSet<VolumeComponent> effectsOff = new HashSet<VolumeComponent>();
    readonly Dictionary<SkinnedMeshRenderer, SkinQuality> skinQuality = new Dictionary<SkinnedMeshRenderer, SkinQuality>();
    readonly HashSet<Renderer> mouthHidden = new HashSet<Renderer>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        if (Application.isMobilePlatform)
        {
            QualitySettings.vSyncCount = 0;            // phones ignore V-Sync and default to 30 fps: ask for 60
            Application.targetFrameRate = 60;
        }
        GameObject g = new GameObject("GraphicsMode");
        DontDestroyOnLoad(g);
        instance = g.AddComponent<MobilePerformance>();
    }

    public static void Apply() { if (instance != null) instance.Tune(); }

    // The render pipeline settings are a project file: in the Unity Editor a change made while playing would be
    // saved into it. So the original values always go back when the game stops.
    void OnApplicationQuit()
    {
        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (!captured || urp == null) return;
        urp.renderScale = renderScale;
        urp.msaaSampleCount = msaa;
        urp.shadowDistance = shadowDistance;
        urp.shadowCascadeCount = cascades;
        urp.maxAdditionalLightsCount = lamps;
        QualitySettings.lodBias = lodBias;
    }

    void Start() { InvokeRepeating(nameof(Tune), 0.5f, 2f); }        // also catches lights / cameras made later

    void Tune()
    {
        Capture();
        bool on = OldPhoneMode;
        int level = Level;

        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.renderScale = on ? Mathf.Min(renderScale, ScaleFor(Megapixels[level])) : renderScale;
            urp.msaaSampleCount = on ? 1 : msaa;
            urp.shadowDistance = on ? Mathf.Min(shadowDistance, ShadowDistance[level]) : shadowDistance;
            urp.shadowCascadeCount = on ? 1 : cascades;
            urp.maxAdditionalLightsCount = on ? Mathf.Min(lamps, LampsPerObject[level]) : lamps;
        }
        QualitySettings.lodBias = lodBias;

        TuneLights(on, level);
        TuneCameras(on, level);
        TuneEffects(on, level);
        TuneFamily(on);
    }

    void Capture()
    {
        if (captured) return;
        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) return;
        renderScale = urp.renderScale;
        msaa = urp.msaaSampleCount;
        shadowDistance = urp.shadowDistance;
        cascades = urp.shadowCascadeCount;
        lamps = urp.maxAdditionalLightsCount;
        lodBias = QualitySettings.lodBias;
        captured = true;
    }

    // The share of the screen's resolution that gives about this many megapixels (never above 85%).
    static float ScaleFor(float megapixels)
    {
        float screen = Screen.width * (float)Screen.height / 1000000f;
        if (screen <= 0f) return 0.75f;
        return Mathf.Clamp(Mathf.Sqrt(megapixels / screen), 0.5f, 0.85f);
    }

    void TuneLights(bool on, int level)
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (!lightShadows.ContainsKey(l)) lightShadows[l] = l.shadows;
            LightShadows original = lightShadows[l];
            LightShadows wanted = original;
            if (on && l.type != LightType.Directional) wanted = LightShadows.None;
            else if (on && level < 3 && original == LightShadows.Soft) wanted = LightShadows.Hard;
            if (l.shadows != wanted) l.shadows = wanted;
        }
        RemoveGone(lightShadows);
    }

    void TuneCameras(bool on, int level)
    {
        foreach (Camera c in Camera.allCameras)
        {
            if (!farClip.ContainsKey(c)) farClip[c] = c.farClipPlane;
            c.farClipPlane = on ? Mathf.Min(farClip[c], ViewDistance[level]) : farClip[c];
            float[] cull = new float[32];
            if (on) cull[Playground.SmallThingsLayer] = SmallThingsDistance[level];
            c.layerCullDistances = cull;

            UniversalAdditionalCameraData data = c.GetUniversalAdditionalCameraData();
            if (data == null || c.cameraType != CameraType.Game) continue;
            if (!postProcessing.ContainsKey(c)) postProcessing[c] = data.renderPostProcessing;
            data.renderPostProcessing = on && level == 0 ? false : postProcessing[c];
        }
        RemoveGone(farClip);
        RemoveGone(postProcessing);
    }

    void TuneEffects(bool on, int level)
    {
        foreach (Volume v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            VolumeProfile p = v.HasInstantiatedProfile() ? v.profile : v.sharedProfile;   // the build's copy, never saved
            if (p == null) continue;
            Switch<Bloom>(p, on && level <= 1);
            Switch<DepthOfField>(p, on);
            Switch<MotionBlur>(p, on);
            Switch<FilmGrain>(p, on);
            Switch<ScreenSpaceLensFlare>(p, on);
            Switch<ChromaticAberration>(p, on);
        }
        effectsOff.RemoveWhere(e => e == null);
    }

    // off = true: switch the effect off (remembered); off = false: switch it back on if we switched it off.
    void Switch<T>(VolumeProfile p, bool off) where T : VolumeComponent
    {
        T effect;
        if (!p.TryGet(out effect)) return;
        if (off && effect.active) { effect.active = false; effectsOff.Add(effect); }
        else if (!off && effectsOff.Remove(effect)) effect.active = true;
    }

    // Mom and Luna: 2 bones per vertex, and Luna's teeth and tongue (hidden in her mouth) are not drawn.
    void TuneFamily(bool on)
    {
        List<GameObject> family = new List<GameObject>();
        foreach (GrandmaAI m in FindObjectsByType<GrandmaAI>(FindObjectsSortMode.None)) family.Add(m.gameObject);
        foreach (BabyAI b in FindObjectsByType<BabyAI>(FindObjectsSortMode.None)) family.Add(b.gameObject);
        foreach (GameObject who in family)
            foreach (SkinnedMeshRenderer r in who.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!skinQuality.ContainsKey(r)) skinQuality[r] = r.quality;
                r.quality = on ? SkinQuality.Bone2 : skinQuality[r];
                string n = r.name.ToLower();
                if (!n.Contains("teeth") && !n.Contains("tongue")) continue;
                if (on && r.enabled) { r.enabled = false; mouthHidden.Add(r); }
                else if (!on && mouthHidden.Remove(r)) r.enabled = true;
            }
        RemoveGone(skinQuality);
        mouthHidden.RemoveWhere(r => r == null);
    }

    static void RemoveGone<TKey, TValue>(Dictionary<TKey, TValue> d) where TKey : Object
    {
        List<TKey> gone = new List<TKey>();
        foreach (TKey k in d.Keys) if (k == null) gone.Add(k);
        foreach (TKey k in gone) d.Remove(k);
    }
}
