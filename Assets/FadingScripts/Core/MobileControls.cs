using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Goes in: nowhere. It starts by itself on phones and tablets (Android), and does nothing on PC.
// On-screen TOUCH CONTROLS:
//   left half of the screen   a joystick appears where your thumb lands: walk
//   right half of the screen  drag to look around
//   buttons (right side)      TOUCH (= F), HINT (= H), RUN (on / off), II (pause, = Esc)
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

    // While the tutorial runs, PrologueTutorial switches this on (shows the SKIP TUTORIAL button).
    public static bool TutorialShowing;

    const float FullSwipeDegrees = 160f;      // dragging across the whole screen width turns this far

    int moveTouch = -1, lookTouch = -1;
    readonly System.Collections.Generic.HashSet<int> known = new System.Collections.Generic.HashSet<int>();   // fingers already handled
    Vector2 moveOrigin, movePosition;
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
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 60;
#pragma warning restore CS0162
    }

    // The word to show for a key in hints: "[F]" on PC, "TOUCH" on a phone.
    public static string Label(string key)
    {
        if (!Active) return "[" + key + "]";
        switch (key)
        {
            case "F": return "TOUCH";
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

                if (ended) { known.Remove(id); }
                else if (known.Add(id)) Begin(id, pos);                       // a new finger (even if it already moved this frame)

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
                        look += d / Screen.width * FullSwipeDegrees * SensitivityFactor();
                        lookPixels += d.magnitude;
                        lookSeen = true;
                    }
                }
            }
        }
        if (!moveSeen) moveTouch = -1;
        if (!lookSeen) lookTouch = -1;

        float radius = Screen.height * 0.11f;
        Move = moveTouch >= 0 ? Vector2.ClampMagnitude((movePosition - moveOrigin) / radius, 1f) : Vector2.zero;
        LookDegrees = new Vector2(look.x, look.y);
        LookPixels = lookPixels;
    }

    static float SensitivityFactor() { return Mathf.Lerp(0.4f, 2f, GameSettings.Sensitivity / 100f); }

    // A new finger: a button, the joystick (left half) or looking (right half).
    void Begin(int id, Vector2 pos)
    {
        Vector2 gui = new Vector2(pos.x, Screen.height - pos.y);           // OnGUI counts from the top
        if (CutsceneRunner.IsPlaying) { if (Hit(SkipRect(), gui)) skipFrame = Time.frameCount; return; }
        if (PauseMenu.IsOpen) return;                                      // the pause menu's own buttons handle touches

        if (Hit(PauseRect(), gui)) { pauseFrame = Time.frameCount; return; }
        if (TutorialShowing && Hit(TutorialRect(), gui)) { tutorialSkipFrame = Time.frameCount; return; }
        if (Hit(InteractRect(), gui)) { interactFrame = Time.frameCount; return; }
        if (Hit(HintRect(), gui)) { hintFrame = Time.frameCount; return; }
        if (Hit(RunRect(), gui)) { Running = !Running; return; }

        if (pos.x < Screen.width * 0.45f) { if (moveTouch < 0) { moveTouch = id; moveOrigin = movePosition = pos; } }
        else if (lookTouch < 0) lookTouch = id;
    }

    static bool Hit(Rect r, Vector2 p) { return r.Contains(p); }

    // ---------- where the buttons are (screen-size independent)
    static float U { get { return Screen.height / 100f; } }        // 1 unit = 1% of the screen height
    static Rect InteractRect() { return new Rect(Screen.width - 26f * U, Screen.height - 30f * U, 22f * U, 22f * U); }
    static Rect HintRect() { return new Rect(Screen.width - 20f * U, Screen.height - 50f * U, 14f * U, 14f * U); }
    static Rect RunRect() { return new Rect(Screen.width - 44f * U, Screen.height - 20f * U, 14f * U, 14f * U); }
    static Rect PauseRect() { return new Rect(Screen.width - 12f * U, 2f * U, 10f * U, 10f * U); }
    static Rect SkipRect() { return new Rect(Screen.width - 26f * U, 3f * U, 22f * U, 9f * U); }
    static Rect TutorialRect() { return new Rect(2f * U, 3f * U, 34f * U, 9f * U); }

    // ---------- drawing
    GUIStyle label;

    void OnGUI()
    {
        if (!InGame) return;
        if (label == null) label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        label.fontSize = Mathf.RoundToInt(2.6f * U);

        if (CutsceneRunner.IsPlaying) { Pill(SkipRect(), "SKIP", 0.35f); return; }
        if (PauseMenu.IsOpen) return;

        Round(InteractRect(), "TOUCH", 0.4f);
        Round(HintRect(), "HINT", 0.3f);
        Round(RunRect(), Running ? "RUN\nON" : "RUN", Running ? 0.45f : 0.25f);
        Round(PauseRect(), "II", 0.3f);
        if (TutorialShowing) Pill(TutorialRect(), "SKIP TUTORIAL", 0.3f);

        if (moveTouch >= 0)                                                 // the joystick under the thumb
        {
            float r = Screen.height * 0.11f;
            Vector2 o = new Vector2(moveOrigin.x, Screen.height - moveOrigin.y);
            Vector2 k = o + new Vector2(Move.x, -Move.y) * r;
            Draw(new Rect(o.x - r, o.y - r, 2f * r, 2f * r), new Color(1f, 1f, 1f, 0.15f));
            Draw(new Rect(k.x - r * 0.4f, k.y - r * 0.4f, r * 0.8f, r * 0.8f), new Color(1f, 0.9f, 0.75f, 0.45f));
        }
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
