using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Goes in: nowhere. It starts by itself on phones and tablets (Android), and does nothing on PC.
// On-screen TOUCH CONTROLS:
//   joystick (bottom left, fixed)   walk
//   anywhere else                   drag to look around
//   a quick tap on something        touches it (= F on that object: PlayerInteractor uses TapPosition)
//   buttons (right side)            HINT (= H), RUN (on / off), II (pause, = Esc)
//   during cutscenes          SKIP (= Space)        during the tutorial   SKIP TUTORIAL (= Tab)
// Other scripts ask it what happened this frame (MobileControls.InteractPressed, .Move, .LookDegrees ...), so every
// keyboard control in the game also works with touch. MobileControls.Label("F") gives the right word for hints on screen.
public class MobileControls : MonoBehaviour
{
    public static bool Active { get; private set; }

    public static Vector2 Move { get; private set; }               // -1 .. 1 (x = sideways, y = forward)
    public static Vector2 LookDegrees { get; private set; }        // how far to turn this frame (x = yaw, y = pitch)
    public static float LookPixels { get; private set; }           // how much the player dragged this frame (for the tutorial)
    public static bool Running { get; private set; }

    static int interactFrame = -10, hintFrame = -10, pauseFrame = -10, skipFrame = -10, tutorialSkipFrame = -10;
    public static bool InteractPressed { get { return interactFrame == Time.frameCount; } }
    public static bool HintPressed { get { return hintFrame == Time.frameCount; } }
    public static bool PausePressed { get { return pauseFrame == Time.frameCount; } }
    public static bool SkipPressed { get { return skipFrame == Time.frameCount; } }
    public static bool TutorialSkipPressed { get { return tutorialSkipFrame == Time.frameCount; } }
    public static Vector2 TapPosition { get; private set; }       // where the last tap was (screen pixels)

    const float TapSeconds = 0.3f;            // a touch shorter than this...
    const float TapInches = 0.15f;            // ...that moved less than this is a tap, not a drag

    // While the tutorial runs, PrologueTutorial switches this on (shows the SKIP TUTORIAL button).
    public static bool TutorialShowing;

    const float DegreesPerInch = 75f;         // dragging one inch (2.5 cm) of screen turns this far (the same on every phone / tablet)

    int moveTouch = -1, lookTouch = -1;
    readonly System.Collections.Generic.HashSet<int> known = new System.Collections.Generic.HashSet<int>();   // fingers already handled
    struct TapCandidate { public float start; public Vector2 from; public bool moved; }
    readonly System.Collections.Generic.Dictionary<int, TapCandidate> taps = new System.Collections.Generic.Dictionary<int, TapCandidate>();
    Vector2 movePosition;
    Texture2D circle;

    // ---------- start by itself on touch devices
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        // Only in a real phone build. Never in the Unity Editor (even with the Android build target or the Device Simulator)
        // and never on Windows, so the mouse and keyboard always work there.
#if UNITY_EDITOR || !(UNITY_ANDROID || UNITY_IOS)
        return;
#else
        if (!Application.isMobilePlatform) return;
#endif
#pragma warning disable CS0162
        GameObject g = new GameObject("MobileControls");
        DontDestroyOnLoad(g);
        g.AddComponent<MobileControls>();
        Active = true;

        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.AutoRotation;               // landscape only, either way round
        Screen.sleepTimeout = SleepTimeout.NeverSleep;                      // (the frame rate is set by MobilePerformance)
#pragma warning restore CS0162
    }

    // The word to show for a key in hints: "[F]" on PC, "TAP" on a phone.
    public static string Label(string key)
    {
        if (!Active) return "[" + key + "]";
        switch (key)
        {
            case "F": return "TAP";
            case "H": return "HINT";
            case "Esc": return "II";
            case "Space": return "SKIP";
            case "Tab": return "SKIP TUTORIAL";
            case "Shift": return "RUN";
            default: return key;
        }
    }

    void Awake() { circle = MakeCircle(128); }

    bool InGame { get { return SceneManager.GetActiveScene().name.StartsWith("Chapter"); } }

    // ---------- reading the touches
    void Update()
    {
        Vector2 look = Vector2.zero;
        float lookPixels = 0f;
        bool moveSeen = false, lookSeen = false;

        if (Touchscreen.current != null && InGame)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                int id = touch.touchId.ReadValue();
                var phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.None) continue;
                Vector2 pos = touch.position.ReadValue();
                bool ended = phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled;

                if (ended) { known.Remove(id); EndTap(id, pos); }
                else if (known.Add(id)) Begin(id, pos);                       // a new finger (even if it already moved this frame)
                else WatchTap(id, pos);

                if (id == moveTouch)
                {
                    if (ended) moveTouch = -1;
                    else { movePosition = pos; moveSeen = true; }
                }
                else if (id == lookTouch)
                {
                    if (ended) lookTouch = -1;
                    else
                    {
                        Vector2 d = touch.delta.ReadValue();
                        look += d * DegreesPerPixel() * SensitivityFactor();
                        lookPixels += d.magnitude;
                        lookSeen = true;
                    }
                }
            }
        }
        if (!moveSeen) moveTouch = -1;
        if (!lookSeen) lookTouch = -1;

        Move = moveTouch >= 0 ? Vector2.ClampMagnitude((movePosition - PadCentre()) / PadRadius(), 1f) : Vector2.zero;
        LookDegrees = new Vector2(look.x, look.y);
        LookPixels = lookPixels;
    }

    static float SensitivityFactor() { return Mathf.Lerp(0.4f, 2f, GameSettings.Sensitivity / 100f); }

    // Screens report their pixels per inch; if one does not, a 6-inch-wide screen is assumed.
    static float DegreesPerPixel()
    {
        return DegreesPerInch / Dpi();
    }

    // A new finger: a button, the joystick, or looking (and maybe a tap).
    void Begin(int id, Vector2 pos)
    {
        Vector2 gui = new Vector2(pos.x, Screen.height - pos.y);           // OnGUI counts from the top
        if (CutsceneRunner.IsPlaying) { if (Hit(SkipRect(), gui)) skipFrame = Time.frameCount; return; }
        if (PauseMenu.IsOpen) return;                                      // the pause menu's own buttons handle touches

        if (Hit(PauseRect(), gui)) { pauseFrame = Time.frameCount; return; }
        if (TutorialShowing && Hit(TutorialRect(), gui)) { tutorialSkipFrame = Time.frameCount; return; }
        if (Hit(HintRect(), gui)) { hintFrame = Time.frameCount; return; }
        if (Hit(RunRect(), gui)) { Running = !Running; return; }

        if (moveTouch < 0 && Vector2.Distance(pos, PadCentre()) < PadRadius() * 1.6f) { moveTouch = id; movePosition = pos; return; }
        if (lookTouch < 0) lookTouch = id;
        taps[id] = new TapCandidate { start = Time.unscaledTime, from = pos };
    }

    // A finger that moves too far is a drag (looking), not a tap.
    void WatchTap(int id, Vector2 pos)
    {
        TapCandidate t;
        if (!taps.TryGetValue(id, out t) || t.moved) return;
        if (Vector2.Distance(pos, t.from) > TapInches * Dpi()) { t.moved = true; taps[id] = t; }
    }

    // Finger lifted: if it was short and still, it was a tap on whatever is under it.
    void EndTap(int id, Vector2 pos)
    {
        TapCandidate t;
        if (!taps.TryGetValue(id, out t)) return;
        taps.Remove(id);
        if (t.moved || Time.unscaledTime - t.start > TapSeconds || Vector2.Distance(pos, t.from) > TapInches * Dpi()) return;
        if (CutsceneRunner.IsPlaying || PauseMenu.IsOpen) return;
        TapPosition = pos;
        interactFrame = Time.frameCount;
    }

    static float Dpi() { return Screen.dpi > 50f ? Screen.dpi : Screen.width / 6f; }

    // The fixed joystick, bottom left (screen pixels, measured from the bottom like touches are).
    static Vector2 PadCentre() { return new Vector2(19f * U, 19f * U); }
    static float PadRadius() { return 12f * U; }

    static bool Hit(Rect r, Vector2 p) { return r.Contains(p); }

    // ---------- where the buttons are (screen-size independent)
    static float U { get { return Screen.height / 100f; } }        // 1 unit = 1% of the screen height
    static Rect HintRect() { return new Rect(Screen.width - 22f * U, Screen.height - 24f * U, 18f * U, 18f * U); }
    static Rect RunRect() { return new Rect(Screen.width - 22f * U, Screen.height - 44f * U, 15f * U, 15f * U); }
    static Rect PauseRect() { return new Rect(Screen.width - 12f * U, 2f * U, 10f * U, 10f * U); }
    static Rect SkipRect() { return new Rect(Screen.width - 26f * U, 3f * U, 22f * U, 9f * U); }
    static Rect TutorialRect() { return new Rect(2f * U, 3f * U, 34f * U, 9f * U); }

    // ---------- drawing
    GUIStyle label;

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;     // only drawing here: skip the extra layout / input passes
        if (!InGame) return;
        if (label == null) label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        label.fontSize = Mathf.RoundToInt(2.6f * U);

        if (CutsceneRunner.IsPlaying) { Pill(SkipRect(), "SKIP", 0.35f); return; }
        if (PauseMenu.IsOpen) return;

        Round(HintRect(), "HINT", 0.3f);
        Round(RunRect(), Running ? "RUN\nON" : "RUN", Running ? 0.45f : 0.25f);
        Round(PauseRect(), "II", 0.3f);
        if (TutorialShowing) Pill(TutorialRect(), "SKIP TUTORIAL", 0.3f);

        // the fixed joystick: its ring always shows, the knob follows the thumb
        float r = PadRadius();
        Vector2 o = new Vector2(PadCentre().x, Screen.height - PadCentre().y);
        Vector2 k = o + new Vector2(Move.x, -Move.y) * r;
        Draw(new Rect(o.x - r, o.y - r, 2f * r, 2f * r), new Color(1f, 1f, 1f, moveTouch >= 0 ? 0.18f : 0.1f));
        Draw(new Rect(k.x - r * 0.42f, k.y - r * 0.42f, r * 0.84f, r * 0.84f), new Color(1f, 0.9f, 0.75f, moveTouch >= 0 ? 0.5f : 0.3f));
    }

    void Round(Rect r, string text, float alpha)
    {
        Draw(r, new Color(0.08f, 0.07f, 0.06f, alpha + 0.15f));
        Draw(new Rect(r.x + r.width * 0.06f, r.y + r.height * 0.06f, r.width * 0.88f, r.height * 0.88f), new Color(1f, 0.85f, 0.6f, alpha * 0.5f));
        label.normal.textColor = new Color(1f, 0.95f, 0.88f, 0.95f);
        GUI.Label(r, text, label);
    }

    void Pill(Rect r, string text, float alpha)
    {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, alpha + 0.2f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
        label.normal.textColor = new Color(1f, 0.95f, 0.88f, 0.95f);
        GUI.Label(r, text, label);
    }

    void Draw(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, circle);
        GUI.color = old;
    }

    static Texture2D MakeCircle(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * size * 0.5f)));
            }
        t.Apply();
        return t;
    }
}
