using UnityEngine;

// Goes in: nowhere (a static helper). The player's settings from the pause menu, saved between runs:
//   Fog         1 = the scene's own fog, 0 = no fog, 2 = twice as thick
//   Lighting    1 = normal, higher = brighter world (sun/moon, ambient light and the player's candle)
//   Sensitivity degrees the camera turns per mouse count
// ChapterRules calls ApplyAll() when a chapter starts; the pause menu calls the single Apply... methods while you drag a slider.
public static class GameSettings
{
    const string FogKey = "fading_fog";
    const string LightingKey = "fading_lighting";
    const string SensitivityKey = "fading_sensitivity";

    public const float DefaultSensitivity = 0.1f;

    static float sceneFogDensity;
    static bool sceneFogOn;

    public static float Fog
    {
        get { return PlayerPrefs.GetFloat(FogKey, 1f); }
        set { PlayerPrefs.SetFloat(FogKey, value); }
    }

    public static float Lighting
    {
        get { return PlayerPrefs.GetFloat(LightingKey, 1f); }
        set { PlayerPrefs.SetFloat(LightingKey, value); }
    }

    public static float Sensitivity
    {
        get { return PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity); }
        set { PlayerPrefs.SetFloat(SensitivityKey, value); }
    }

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
        RenderSettings.fog = Fog > 0.02f;
        RenderSettings.fogDensity = sceneFogDensity * Fog;
    }

    public static void ApplyLighting()
    {
        CandleLight.GlobalScale = Lighting;
        DayNightCycle cycle = Object.FindFirstObjectByType<DayNightCycle>();
        if (cycle != null) cycle.SetBrightness(Lighting);
    }

    public static void ApplySensitivity()
    {
        FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
        if (player != null && PlayerPrefs.HasKey(SensitivityKey)) player.lookSensitivity = Sensitivity;
    }

    public static void Save() { PlayerPrefs.Save(); }
}
