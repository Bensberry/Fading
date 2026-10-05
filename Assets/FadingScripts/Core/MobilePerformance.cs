using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Goes in: nowhere (it starts by itself, ONLY in a phone / tablet build; on PC and in the Editor it does nothing).
// AUTOMATIC QUALITY, so the game runs smoothly on ANY phone:
//   1. It guesses a starting level from the device (how much memory it has).
//   2. While you play it measures the frame rate. Too slow (under 45 fps for 3 seconds) -> one level lower.
//      Smooth for a long time (58+ fps for 15 seconds) -> one level higher, but never back to a level that was too slow.
//   3. The level it ends on is saved, so the next time the game starts there straight away.
// Levels (3 = best):
//   3  85% resolution (less on huge tablet screens), soft shadows 25 m, bloom, 3 lamps per object, view 90 m
//   2  ~2.2 megapixels, hard shadows 15 m, bloom, 2 lamps per object, view 70 m
//   1  ~1.4 megapixels, no shadows, no bloom, 1 lamp per object, view 55 m, small yard things vanish sooner
//   0  ~0.9 megapixels, like 1 but no screen effects at all
// Mom and Luna: 2 bones per vertex instead of 4, Luna's teeth and tongue (hidden in her mouth) are not drawn.
// Always: small yard things (flowers, rocks, bushes) are not drawn far away (14-32 m), no anti-aliasing, lamps and candles never cast shadows, depth of field / motion blur / film grain / lens flare off.
public class MobilePerformance : MonoBehaviour
{
    public static int Level { get; private set; } = -1;

    const string SaveKey = "fading_mobile_quality_v2";       // (v2: the old saved level could be stuck at 0)
    const float TooSlowFps = 45f, SmoothFps = 58f;
    const float CheckSeconds = 3f, SmoothSecondsToRise = 15f;

    static readonly float[] Megapixels = { 0.9f, 1.4f, 2.2f, 3.3f };
    static readonly float[] ShadowDistance = { 0f, 0f, 15f, 25f };
    static readonly int[] LampsPerObject = { 1, 1, 2, 3 };
    static readonly float[] ViewDistance = { 50f, 55f, 70f, 90f };
    static readonly float[] LodBias = { 0.5f, 0.6f, 0.8f, 1f };
    static readonly float[] SmallThingsDistance = { 14f, 18f, 24f, 32f };     // flowers, rocks, bushes, fence (Playground.SmallThingsLayer)

    int ceiling = 3;                       // a level that was too slow is never tried again (this session)
    float measureStart, smoothSince;
    int frames;
    readonly Dictionary<Camera, float> farClip = new Dictionary<Camera, float>();
    readonly HashSet<Light> softened = new HashSet<Light>();
    readonly HashSet<Bloom> bloomsOff = new HashSet<Bloom>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
#if UNITY_EDITOR || !(UNITY_ANDROID || UNITY_IOS)
        return;
#else
        if (!Application.isMobilePlatform) return;
#endif
#pragma warning disable CS0162
        QualitySettings.vSyncCount = 0;                // phones ignore V-Sync; Application.targetFrameRate is used instead
        GameObject g = new GameObject("MobilePerformance");
        DontDestroyOnLoad(g);
        g.AddComponent<MobilePerformance>().SetLevel(StartLevel());
#pragma warning restore CS0162
    }

    // The saved level, or a guess from the device's memory.
    static int StartLevel()
    {
        int saved = PlayerPrefs.GetInt(SaveKey, -1);
        if (saved >= 0) return Mathf.Clamp(saved, 0, 3);
        int memoryMb = SystemInfo.systemMemorySize;
        if (memoryMb < 3500) return 0;
        if (memoryMb < 5500) return 1;
        return 2;                                      // even strong devices start at 2 and earn level 3
    }

    void SetLevel(int level)
    {
        Level = Mathf.Clamp(level, 0, 3);
        PlayerPrefs.SetInt(SaveKey, Level);
        PlayerPrefs.Save();

        Application.targetFrameRate = 60;          // never capped lower, so a device can always climb back to a better level
        QualitySettings.lodBias = LodBias[Level];
        UniversalRenderPipelineAsset urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.renderScale = ScaleFor(Megapixels[Level]);
            urp.msaaSampleCount = 1;
            urp.shadowDistance = ShadowDistance[Level];
            urp.shadowCascadeCount = 1;
            urp.maxAdditionalLightsCount = LampsPerObject[Level];
        }
        TuneScene();
        RestartMeasuring();
    }

    // The share of the screen's resolution that gives about this many megapixels (never above 85%).
    static float ScaleFor(float megapixels)
    {
        float screen = Screen.width * (float)Screen.height / 1000000f;
        if (screen <= 0f) return 0.75f;
        return Mathf.Clamp(Mathf.Sqrt(megapixels / screen), 0.5f, 0.85f);
    }

    void Start() { InvokeRepeating(nameof(TuneScene), 1f, 2f); }

    // ---------- measuring the frame rate
    void RestartMeasuring()
    {
        measureStart = Time.unscaledTime;
        smoothSince = Time.unscaledTime;
        frames = 0;
    }

    void OnEnable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m)
    {
        TuneScene();
        measureStart = Time.unscaledTime + 3f;       // loading a scene always hitches: wait 3 s before judging
        smoothSince = measureStart;
        frames = 0;
    }

    void Update()
    {
        if (Time.timeScale == 0f || Time.unscaledTime < measureStart) return;    // paused or just loaded
        frames++;
        float seconds = Time.unscaledTime - measureStart;
        if (seconds < CheckSeconds) return;

        float fps = frames / seconds;
        frames = 0;
        measureStart = Time.unscaledTime;
        float wanted = TooSlowFps;

        if (fps < wanted && Level > 0)
        {
            ceiling = Level - 1;
            SetLevel(Level - 1);
        }
        else if (fps < SmoothFps) smoothSince = Time.unscaledTime;
        else if (Level < ceiling && Time.unscaledTime - smoothSince >= SmoothSecondsToRise) SetLevel(Level + 1);
    }

    // ---------- a hidden readout for testing: tap the screen with THREE fingers to show / hide "Quality 2   57 fps".
    // While it shows, tapping the readout itself hides / shows Mom and Luna (to see how much they cost).
    bool showReadout, threeFingersDown, familyHidden;
    float shownFps, fpsTimer;
    int fpsFrames;
    GUIStyle readoutStyle;

    void LateUpdate()
    {
        int fingers = 0;
        if (UnityEngine.InputSystem.Touchscreen.current != null)
            foreach (var t in UnityEngine.InputSystem.Touchscreen.current.touches) if (t.press.isPressed) fingers++;
        if (fingers >= 3 && !threeFingersDown) { showReadout = !showReadout; if (showReadout) recordUntil = Time.unscaledTime + 6f; }
        if (Time.unscaledTime < recordUntil) RecordFrame();
        threeFingersDown = fingers >= 3;
        if (showReadout && fingers == 1 && UnityEngine.InputSystem.Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 p = UnityEngine.InputSystem.Touchscreen.current.primaryTouch.position.ReadValue();
            if (ReadoutRect().Contains(new Vector2(p.x, Screen.height - p.y))) { familyHidden = !familyHidden; HideFamily(familyHidden); }
        }

        fpsFrames++;
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.5f) { shownFps = fpsFrames / fpsTimer; fpsFrames = 0; fpsTimer = 0f; }
    }

    // For testing: for 6 seconds after the readout opens, one line per frame goes to the phone's log (adb logcat),
    // with the frame time, how far the player's body moved / turned, the camera, the touch look and the candle light.
    float recordUntil;
    Vector3 lastBodyPos;
    float lastYaw;

    void RecordFrame()
    {
        FirstPersonController player = FindAnyObjectByType<FirstPersonController>();
        Camera cam = Camera.main;
        if (player == null || cam == null) return;
        Transform body = player.transform;
        float yaw = body.eulerAngles.y;
        GameObject flame = GameObject.Find("CandleFlame");
        Light glow = flame != null ? flame.GetComponent<Light>() : null;
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null,
            "[REC] dt {0:F1} sdt {1:F1} move {2:F3} turn {3:F2} look {4:F2},{5:F2} joy {6:F2},{7:F2} y {8:F3} camY {9:F3} light {10:F2} lightPos {11}",
            Time.unscaledDeltaTime * 1000f, Time.smoothDeltaTime * 1000f,
            Vector3.Distance(new Vector3(body.position.x, 0f, body.position.z), new Vector3(lastBodyPos.x, 0f, lastBodyPos.z)),
            Mathf.DeltaAngle(lastYaw, yaw), MobileControls.LookDegrees.x, MobileControls.LookDegrees.y,
            MobileControls.Move.x, MobileControls.Move.y, body.position.y, cam.transform.position.y,
            glow != null ? glow.intensity : -1f, flame != null ? (flame.transform.position - cam.transform.position).ToString("F3") : "-");
        lastBodyPos = body.position;
        lastYaw = yaw;
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;     // only drawing here: skip the extra layout / input passes
        if (!showReadout) return;
        if (readoutStyle == null) readoutStyle = new GUIStyle(GUI.skin.label);
        readoutStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.025f);
        readoutStyle.normal.textColor = Color.yellow;
        GUI.Label(ReadoutRect(), "Quality " + Level + "   " + Mathf.RoundToInt(shownFps) + " fps" +
                  (familyHidden ? "   (Mom + Luna hidden)" : "   (tap: hide Mom + Luna)"), readoutStyle);
    }

    static Rect ReadoutRect() { return new Rect(Screen.width * 0.3f, Screen.height * 0.005f, Screen.width * 0.45f, Screen.height * 0.06f); }

    static void HideFamily(bool hide)
    {
        foreach (GrandmaAI m in FindObjectsByType<GrandmaAI>(FindObjectsSortMode.None))
            foreach (Renderer r in m.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = hide;
        foreach (BabyAI b in FindObjectsByType<BabyAI>(FindObjectsSortMode.None))
            foreach (Renderer r in b.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = hide;
    }

    // Mom and Luna cost less to animate: fewer bones per vertex, and Luna's teeth and tongue are not drawn.
    static void TuneFamily()
    {
        foreach (GrandmaAI m in FindObjectsByType<GrandmaAI>(FindObjectsSortMode.None)) TuneCharacter(m.gameObject);
        foreach (BabyAI b in FindObjectsByType<BabyAI>(FindObjectsSortMode.None)) TuneCharacter(b.gameObject);
    }

    static void TuneCharacter(GameObject who)
    {
        foreach (SkinnedMeshRenderer r in who.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            r.quality = SkinQuality.Bone2;
            string n = r.name.ToLower();
            if ((n.Contains("teeth") || n.Contains("tongue")) && r.enabled) r.enabled = false;
        }
    }

    // ---------- lights, cameras and screen effects (also the ones made later: checked every 2 seconds)
    void TuneScene()
    {
        TuneFamily();
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional) { if (l.shadows != LightShadows.None) l.shadows = LightShadows.None; }
            else if (Level < 3 && l.shadows == LightShadows.Soft) { l.shadows = LightShadows.Hard; softened.Add(l); }
            else if (Level == 3 && softened.Remove(l)) l.shadows = LightShadows.Soft;
        }

        foreach (Camera c in Camera.allCameras)
        {
            if (!farClip.ContainsKey(c)) farClip[c] = c.farClipPlane;
            c.farClipPlane = Mathf.Min(farClip[c], ViewDistance[Level]);
            c.allowMSAA = false;
            float[] cull = new float[32];
            cull[Playground.SmallThingsLayer] = SmallThingsDistance[Level];
            c.layerCullDistances = cull;
            UniversalAdditionalCameraData data = c.GetUniversalAdditionalCameraData();
            if (data != null && c.cameraType == CameraType.Game && farClip[c] > 1f) data.renderPostProcessing = Level > 0;
        }

        foreach (Volume v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            VolumeProfile p = v.HasInstantiatedProfile() ? v.profile : v.sharedProfile;   // the build's copy, never saved
            if (p == null) continue;
            Bloom bloom;
            if (p.TryGet(out bloom))
            {
                bloom.highQualityFiltering.Override(false);
                if (Level <= 1 && bloom.active) { bloom.active = false; bloomsOff.Add(bloom); }
                else if (Level >= 2 && bloomsOff.Remove(bloom)) bloom.active = true;
            }
            Off<DepthOfField>(p);
            Off<MotionBlur>(p);
            Off<FilmGrain>(p);
            Off<ScreenSpaceLensFlare>(p);
            Off<ChromaticAberration>(p);
        }
        softened.RemoveWhere(l => l == null);
        bloomsOff.RemoveWhere(b => b == null);
        List<Camera> gone = new List<Camera>();
        foreach (Camera c in farClip.Keys) if (c == null) gone.Add(c);
        foreach (Camera c in gone) farClip.Remove(c);
    }

    static void Off<T>(VolumeProfile p) where T : VolumeComponent
    {
        T effect;
        if (p.TryGet(out effect) && effect.active) effect.active = false;
    }
}
