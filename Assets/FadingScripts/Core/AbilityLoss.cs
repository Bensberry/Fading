using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Goes in: nowhere by hand. ChapterRules adds it in every chapter.
// The ghost slowly loses himself. One thing fades away in each chapter, and what is lost stays lost:
//   Chapter 1  VISION   the colour drains out of the world and the edges of your sight go dark
//   Chapter 2  SPEED    your body gets heavy, you walk and run slower
//   Chapter 3  HEARING  every sound turns dull and quiet
// At the start of a chapter the new loss creeps in slowly (RampSeconds) instead of switching on at once.
// Change the numbers in the three tables below (0 = nothing lost, 1 = fully lost) to make it harsher or gentler.
public class AbilityLoss : MonoBehaviour
{
    //                                   Ch0   Ch1   Ch2   Ch3
    static readonly float[] Vision  = { 0f,   0.6f, 0.6f, 0.6f };
    static readonly float[] Speed   = { 0f,   0f,   0.4f, 0.4f };       // 0.4 = 40% slower
    static readonly float[] Hearing = { 0f,   0f,   0f,   0.7f };

    static readonly string[] Messages =
    {
        "",
        "The colours are draining out of the world...",
        "My legs feel so heavy...",
        "The sounds are fading away...",
    };

    const float RampSeconds = 30f;

    // FirstPersonController multiplies its walking and running speed by this (1 = normal).
    public static float SpeedMultiplier = 1f;

    int chapter;
    float timer;
    ColorAdjustments colour;
    Vignette vignette;
    AudioLowPassFilter lowPass;

    public static void StartFor(GameObject host, int chapterNumber)
    {
        AbilityLoss a = host.AddComponent<AbilityLoss>();
        a.chapter = Mathf.Clamp(chapterNumber, 0, Vision.Length - 1);
    }

    void Start()
    {
        FindEffects();
        Apply(0f);
        if (chapter > 0 && Messages[chapter].Length > 0) Invoke("ShowMessage", 6f);
    }

    void ShowMessage() { FadingHud.Toast(Messages[chapter], 5f); }

    void Update()
    {
        if (timer < RampSeconds)
        {
            timer += Time.deltaTime;
            Apply(Mathf.SmoothStep(0f, 1f, timer / RampSeconds));
        }
    }

    void OnDestroy()
    {
        SpeedMultiplier = 1f;                 // never carry a slowdown into the main menu
        AudioListener.volume = 1f;
    }

    // k = 0: how things were at the end of the previous chapter ... k = 1: the full loss of this chapter.
    void Apply(float k)
    {
        int before = Mathf.Max(0, chapter - 1);
        float vision = Mathf.Lerp(Vision[before], Vision[chapter], k);
        float speed = Mathf.Lerp(Speed[before], Speed[chapter], k);
        float hearing = Mathf.Lerp(Hearing[before], Hearing[chapter], k);

        SetVision(vision);
        SpeedMultiplier = 1f - speed;
        SetHearing(hearing);
    }

    // ---------- vision: desaturate + darken + a dark tunnel around the edges
    void FindEffects()
    {
        Volume volume = FindFirstObjectByType<Volume>();
        if (volume == null)
        {
            GameObject g = new GameObject("VisionLossVolume");
            volume = g.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }
        VolumeProfile profile = volume.profile;          // a private copy, the project's asset is never changed
        if (!profile.TryGet(out colour)) colour = profile.Add<ColorAdjustments>(true);
        if (!profile.TryGet(out vignette)) vignette = profile.Add<Vignette>(true);
    }

    void SetVision(float loss)
    {
        if (colour == null || vignette == null) return;
        colour.saturation.Override(-90f * loss);
        colour.postExposure.Override(-0.9f * loss);
        vignette.color.Override(Color.black);
        vignette.smoothness.Override(0.7f);
        vignette.intensity.Override(0.15f + 0.5f * loss);
    }

    // ---------- hearing: quieter, and the high sounds disappear (a low-pass filter on the listener)
    void SetHearing(float loss)
    {
        AudioListener.volume = 1f - 0.5f * loss;
        if (loss <= 0.001f && lowPass == null) return;

        if (lowPass == null)
        {
            AudioListener listener = FindFirstObjectByType<AudioListener>();
            if (listener == null) return;
            lowPass = listener.GetComponent<AudioLowPassFilter>();
            if (lowPass == null) lowPass = listener.gameObject.AddComponent<AudioLowPassFilter>();
        }
        lowPass.cutoffFrequency = Mathf.Lerp(22000f, 1000f, Mathf.Sqrt(loss));
    }
}
