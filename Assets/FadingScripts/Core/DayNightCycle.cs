using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Day/night cycle for Fading. Put this on the FadingHouse object.
// The game always starts at night:
//   Night 0 (Chapter Zero) -> Day 1 -> Night 1 -> Day 2 -> Night 2 -> Day 3 -> Night 3 (final night)
// Call AdvancePhase() from your game logic when a night is over (e.g. when the candle burns out).
// Days move on by themselves after Day Seconds. Press N while testing to skip ahead.
public class DayNightCycle : MonoBehaviour
{
    public enum Phase { Night0, Day1, Night1, Day2, Night2, Day3, Night3 }

    [Tooltip("Directional light used as sun/moon. Left empty = uses the scene's sun, or creates one.")]
    public Light sun;
    public float transitionSeconds = 3f;
    public bool autoAdvanceDays = true;
    public float daySeconds = 20f;
    [Tooltip("Press N to jump to the next phase (for testing).")]
    public bool debugSkipKey = true;
    public bool showPhaseTitle = true;

    [Header("Night")]
    public Color nightLightColor = new Color(0.55f, 0.65f, 0.95f);
    public float nightIntensity = 0.2f;
    public Color nightAmbient = new Color(0.04f, 0.05f, 0.09f);
    public float nightSkyExposure = 0.12f;
    public Vector3 moonAngles = new Vector3(35f, 210f, 0f);

    [Header("Day")]
    public Color dayLightColor = new Color(1f, 0.9f, 0.75f);
    public float dayIntensity = 1.1f;
    public Color dayAmbient = new Color(0.42f, 0.4f, 0.36f);
    public float daySkyExposure = 1.2f;
    public Vector3 sunAngles = new Vector3(40f, 120f, 0f);

    [Tooltip("Fires every time the phase changes (0 = Night 0 ... 6 = Night 3).")]
    public UnityEvent<int> onPhaseChanged = new UnityEvent<int>();

    public Phase Current { get; private set; }
    public bool IsNight { get { return Current == Phase.Night0 || Current == Phase.Night1 || Current == Phase.Night2 || Current == Phase.Night3; } }

    GameObject packing1, packing2, packing3, grandmaStuff, van, radio, radioBox, musicBox, musicBoxBox;
    Material sky;
    Coroutine fade, dayTimer;
    float titleTime = -10f;
    GUIStyle titleStyle;

    void Awake()
    {
        packing1 = Find("Packing_Night1");
        packing2 = Find("Packing_Night2");
        packing3 = Find("Packing_Night3");
        grandmaStuff = Find("Grandma_Belongings");
        van = Find("MovingVan");
        radio = Find("INT_Mom_Radio");
        radioBox = Find("Box_Kitchen_Radio");
        musicBox = Find("INT_Child_MusicBox");
        musicBoxBox = Find("Box_Child_MusicBox");

        if (sun == null) sun = RenderSettings.sun;
        if (sun == null)
        {
            GameObject g = new GameObject("Sun_Moon");
            sun = g.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.shadows = LightShadows.Soft;
        RenderSettings.sun = sun;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        if (RenderSettings.skybox != null)
        {
            sky = new Material(RenderSettings.skybox);   // copy, so the project's skybox asset isn't changed
            RenderSettings.skybox = sky;
        }
        SetPhase(Phase.Night0, true);                       // the game always starts at night
    }

    void Update()
    {
        if (debugSkipKey && SkipPressed()) AdvancePhase();
    }

    bool SkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.N);
#endif
    }

    // Move to the next phase. Does nothing after Night 3.
    public void AdvancePhase()
    {
        if (Current < Phase.Night3) SetPhase(Current + 1, false);
    }

    public void SetPhase(Phase phase, bool instant)
    {
        Current = phase;
        ApplyWorld(phase);
        bool night = IsNight;
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeLighting(night, instant ? 0f : transitionSeconds));
        if (dayTimer != null) StopCoroutine(dayTimer);
        if (!night && autoAdvanceDays) dayTimer = StartCoroutine(EndDayAfter(daySeconds));
        titleTime = Time.time;
        onPhaseChanged.Invoke((int)phase);
    }

    public static string PhaseName(Phase p)
    {
        switch (p)
        {
            case Phase.Night0: return "Chapter Zero";
            case Phase.Day1: return "Day 1";
            case Phase.Night1: return "Night 1";
            case Phase.Day2: return "Day 2";
            case Phase.Night2: return "Night 2";
            case Phase.Day3: return "Day 3";
            default: return "Night 3";
        }
    }

    // What the house looks like in each phase.
    void ApplyWorld(Phase p)
    {
        SetActive(packing1, p >= Phase.Day1);          // boxes packed during Day 1 are there from then on
        SetActive(packing2, p >= Phase.Day2);
        SetActive(packing3, p >= Phase.Day3);
        SetActive(grandmaStuff, p == Phase.Night0);    // Grandma leaves the morning after Chapter Zero
        SetActive(van, p >= Phase.Day3);               // moving van arrives for the last day and night

        bool radioPacked = p >= Phase.Day3;            // radio and music box are packed on the last day
        SetActive(radio, !radioPacked);
        SetActive(radioBox, radioPacked);
        bool musicPacked = p >= Phase.Day3;
        SetActive(musicBox, !musicPacked);
        SetActive(musicBoxBox, musicPacked);

        foreach (DoorToggle d in GetComponentsInChildren<DoorToggle>(true)) d.CloseInstant();   // every phase starts with doors shut
    }

    IEnumerator FadeLighting(bool night, float time)
    {
        Color c0 = sun.color, a0 = RenderSettings.ambientLight;
        float i0 = sun.intensity;
        Quaternion r0 = sun.transform.rotation;
        float e0 = SkyExposure();

        Color c1 = night ? nightLightColor : dayLightColor;
        Color a1 = night ? nightAmbient : dayAmbient;
        float i1 = night ? nightIntensity : dayIntensity;
        Quaternion r1 = Quaternion.Euler(night ? moonAngles : sunAngles);
        float e1 = night ? nightSkyExposure : daySkyExposure;

        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / time);
            sun.color = Color.Lerp(c0, c1, k);
            sun.intensity = Mathf.Lerp(i0, i1, k);
            sun.transform.rotation = Quaternion.Slerp(r0, r1, k);
            RenderSettings.ambientLight = Color.Lerp(a0, a1, k);
            SetSkyExposure(Mathf.Lerp(e0, e1, k));
            yield return null;
        }
        sun.color = c1; sun.intensity = i1; sun.transform.rotation = r1;
        RenderSettings.ambientLight = a1;
        SetSkyExposure(e1);
        DynamicGI.UpdateEnvironment();
        fade = null;
    }

    IEnumerator EndDayAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        dayTimer = null;
        AdvancePhase();
    }

    float SkyExposure() { return (sky != null && sky.HasProperty("_Exposure")) ? sky.GetFloat("_Exposure") : 1f; }
    void SetSkyExposure(float v) { if (sky != null && sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", v); }

    GameObject Find(string name)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }

    static void SetActive(GameObject g, bool on) { if (g != null && g.activeSelf != on) g.SetActive(on); }

    void OnGUI()
    {
        if (!showPhaseTitle) return;
        float age = Time.time - titleTime;
        if (age > 4f) return;
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 42 };
        }
        float alpha = age < 0.8f ? age / 0.8f : (age > 3f ? 4f - age : 1f);
        titleStyle.normal.textColor = new Color(1f, 0.95f, 0.85f, alpha);
        GUI.Label(new Rect(0, Screen.height * 0.2f, Screen.width, 60), PhaseName(Current), titleStyle);
    }
}
