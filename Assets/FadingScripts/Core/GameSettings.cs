using UnityEngine;

// Goes in: nowhere (a static helper). The player's settings from the pause menu, saved between runs.
// Every setting is a number from 0 to 100 (what the sliders show):
//   Fog         50 = the scene's own fog (default), 0 = no fog, 100 = twice as thick
//   Lighting    50 = normal (default), 0 = darker, 100 = much brighter (sun/moon, ambient light and the player's candle)
//   Sensitivity 0 = very slow, 100 = very fast (default 35, which turns 0.1 degrees per mouse count like most FPS games)
// ChapterRules calls ApplyAll() when a chapter starts; the pause menu calls the single Apply... methods while you drag a slider.
public static class GameSettings
{
    const string FogKey = "fading_fog_percent";
    const string LightingKey = "fading_lighting_percent";
    const string SensitivityKey = "fading_sensitivity_percent";

    public const float DefaultFog = 50f;
    public const float DefaultLighting = 50f;
    public const float DefaultSensitivity = 35f;

    static float sceneFogDensity;
    static bool sceneFogOn;

    public static float Fog
    {
        get { return PlayerPrefs.GetFloat(FogKey, DefaultFog); }
        set { PlayerPrefs.SetFloat(FogKey, value); }
    }

    public static float Lighting
    {
        get { return PlayerPrefs.GetFloat(LightingKey, DefaultLighting); }
        set { PlayerPrefs.SetFloat(LightingKey, value); }
    }

    public static float Sensitivity
    {
        get { return PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity); }
        set { PlayerPrefs.SetFloat(SensitivityKey, value); }
    }

    // ---------- slider value (0-100) -> real value
    public static float FogScale { get { return Fog / 50f; } }                                  // 0 .. 1 (scene's fog) .. 2

    public static float LightingScale                                                           // 0.5 .. 1 (normal) .. 2.5
    {
        get { return Lighting <= 50f ? Mathf.Lerp(0.5f, 1f, Lighting / 50f) : Mathf.Lerp(1f, 2.5f, (Lighting - 50f) / 50f); }
    }

    public static float SensitivityDegrees { get { return Mathf.Lerp(0.02f, 0.25f, Sensitivity / 100f); } }   // degrees per mouse count

    // Remember how the scene's fog was set up, so "1" always means "as the scene designer made it".
    public static void CaptureSceneDefaults()
    {
        sceneFogDensity = RenderSettings.fogDensity;
        sceneFogOn = RenderSettings.fog;
    }

    public static void ApplyAll()
    {
        ApplyFog();
        ApplyLighting();
        ApplySensitivity();
    }

    public static void ApplyFog()
    {
        if (!sceneFogOn) return;                       // this scene has no fog to adjust
        RenderSettings.fog = FogScale > 0.02f;
        RenderSettings.fogDensity = sceneFogDensity * FogScale;
    }

    public static void ApplyLighting()
    {
        CandleLight.GlobalScale = LightingScale;
        DayNightCycle cycle = Object.FindFirstObjectByType<DayNightCycle>();
        if (cycle != null) cycle.SetBrightness(LightingScale);
    }

    public static void ApplySensitivity()
    {
        FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
        if (player != null && PlayerPrefs.HasKey(SensitivityKey)) player.lookSensitivity = SensitivityDegrees;
    }

    public static void Save() { PlayerPrefs.Save(); }
}
