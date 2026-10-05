using UnityEngine;

// Goes in: nowhere. It creates itself the first time another script calls FadingHud.Toast(...) etc.
// Draws the small on-screen messages of the game with OnGUI (no Canvas needed):
//   Toast      = a short popup in the upper middle ("The door won't open", candle hints)
//   Objective  = one line at the bottom (tutorial steps, what to do next)
//   Candle     = the candle hint indicator in the bottom-left corner
public class FadingHud : MonoBehaviour
{
    static FadingHud instance;

    static FadingHud Instance
    {
        get
        {
            if (instance == null) instance = new GameObject("FadingHud").AddComponent<FadingHud>();
            return instance;
        }
    }

    string toast = "";
    float toastStart = -100f, toastLength;
    string objective = "";
    float objectiveTime = -100f;
    bool objectiveSticky;
    const float ObjectiveSeconds = 12f;                   // a goal line fades away after this (it comes back when it changes)
    bool skipHint;
    string subtitleSpeaker = "", subtitleText = "";
    float subtitleStart = -100f, subtitleLength;

    bool progressShown;
    float momBar01, lunaBar01;
    bool gainMom, gainLuna;
    float progressScareTime = -100f;
    string gainText = "";
    float gainTime = -100f;

    bool candleShown;

    // Chapter 0 hides the candle display until the tutorial has explained the candle (H).
    public static bool CandleHudAllowed = true;
    int hintsLeft, hintsMax;
    float cooldown01;
    bool burnedOut;

    GUIStyle toastStyle, objectiveStyle, smallStyle;

    // ---------- call these from other scripts
    public static void Toast(string text, float seconds = 3.5f)
    {
        FadingHud h = Instance;
        h.toast = text;
        h.toastStart = Time.unscaledTime;
        h.toastLength = seconds;
    }

    // The goal line at the bottom. It fades out after ObjectiveSeconds and shows again whenever the text changes.
    // sticky = stays until replaced (the tutorial steps).
    public static void SetObjective(string text, bool sticky = false)
    {
        FadingHud h = Instance;
        text = text ?? "";
        if (text == h.objective && sticky == h.objectiveSticky) return;
        h.objective = text;
        h.objectiveTime = Time.unscaledTime;
        h.objectiveSticky = sticky;
    }

    // "SPACE  skip" in the bottom-right corner (shown during cutscenes).
    public static void ShowSkipHint(bool on) { Instance.skipHint = on; }

    // A line of dialogue near the bottom of the screen: "MOM   Did you hear that?"  (used by cutscenes)
    public static void Subtitle(string speaker, string text, float seconds)
    {
        FadingHud h = Instance;
        h.subtitleSpeaker = speaker ?? "";
        h.subtitleText = text ?? "";
        h.subtitleStart = Time.unscaledTime;
        h.subtitleLength = seconds;
    }

    public static void ClearSubtitle() { Instance.subtitleLength = 0f; }

    // The "they feel you" bar at the top of the screen (0 to 1).
    // The two bars at the top: how much Mom and Luna feel the ghost (0 to 1 each).
    public static void SetProgress(float mom01, float luna01)
    {
        FadingHud h = Instance;
        h.progressShown = true;
        h.momBar01 = Mathf.Clamp01(mom01);
        h.lunaBar01 = Mathf.Clamp01(luna01);
    }

    // "+10  Mom noticed the coffee mug" under the bar for a moment, and the bar glows.
    // "+6  Mom noticed the coffee mug" under the bar(s) that grew, and those bars glow for a moment.
    public static void ProgressGain(string text, bool mom, bool luna)
    {
        FadingHud h = Instance;
        h.gainText = text;
        h.gainTime = Time.unscaledTime;
        h.gainMom = mom;
        h.gainLuna = luna;
    }

    // The bar flashes red for a moment (the ghost frightened the family).
    public static void ProgressScare() { Instance.progressScareTime = Time.unscaledTime; }

    public static void HideProgress() { if (instance != null) instance.progressShown = false; }

    public static void SetCandle(int hintsLeft, int hintsMax, float cooldown01, bool burnedOut)
    {
        FadingHud h = Instance;
        h.candleShown = true;
        h.hintsLeft = hintsLeft;
        h.hintsMax = hintsMax;
        h.cooldown01 = cooldown01;
        h.burnedOut = burnedOut;
    }

    // ---------- drawing
    void OnGUI()
    {
        MakeStyles();
        bool cutscene = CutsceneRunner.IsPlaying;          // cutscenes show only subtitles (no bar, candle or goal)
        if (!cutscene) DrawProgress();
        DrawToast();
        DrawSkipHint();
        DrawSubtitle();
        if (!cutscene) DrawObjective();
        if (candleShown && !cutscene && CandleHudAllowed) DrawCandle();
    }

    void MakeStyles()
    {
        int big = Mathf.Max(16, Mathf.RoundToInt(Screen.height * 0.032f));
        if (toastStyle != null && toastStyle.fontSize == big) return;
        toastStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = big, wordWrap = true };
        objectiveStyle = new GUIStyle(toastStyle) { fontSize = Mathf.RoundToInt(big * 0.8f) };
        smallStyle = new GUIStyle(toastStyle) { fontSize = Mathf.RoundToInt(big * 0.65f), alignment = TextAnchor.MiddleLeft };
    }

    void DrawProgress()
    {
        if (!progressShown) return;
        float w = Screen.width * 0.26f, h = Screen.height * 0.024f, gap = Screen.width * 0.045f;
        float y = Screen.height * 0.04f;
        float momX = Screen.width / 2f - gap / 2f - w, lunaX = Screen.width / 2f + gap / 2f;
        float scare = Mathf.Clamp01(1f - (Time.unscaledTime - progressScareTime) / 2.5f);
        float gain = Mathf.Clamp01(1f - (Time.unscaledTime - gainTime) / 2.8f);

        DrawBar(momX, y, w, h, momBar01, "MOM", gainMom ? gain : 0f, scare, new Color(1f, 0.62f, 0.3f));
        DrawBar(lunaX, y, w, h, lunaBar01, "LUNA", gainLuna ? gain : 0f, scare, new Color(0.62f, 0.78f, 1f));

        // a small candle flame between the bars
        DrawBox(new Rect(Screen.width / 2f - h * 0.25f, y - h * 0.2f, h * 0.5f, h * 1.2f), new Color(1f, 0.8f, 0.45f, 0.85f));

        smallStyle.alignment = TextAnchor.MiddleCenter;
        if (scare > 0f)
        {
            smallStyle.normal.textColor = new Color(1f, 0.35f, 0.3f, scare);
            GUI.Label(new Rect(momX, y + h * 1.4f, lunaX + w - momX, h * 1.8f), "You frightened them", smallStyle);
        }
        else if (gain > 0f)
        {
            float left = gainMom && !gainLuna ? momX : (gainLuna && !gainMom ? lunaX : momX);
            float width = gainMom && gainLuna ? lunaX + w - momX : w;
            smallStyle.normal.textColor = new Color(1f, 0.9f, 0.65f, Mathf.Clamp01(gain * 1.6f));
            GUI.Label(new Rect(left - w * 0.3f, y + h * 1.4f + (1f - gain) * 6f, width + w * 0.6f, h * 1.8f), gainText, smallStyle);
        }
        smallStyle.alignment = TextAnchor.MiddleLeft;
    }

    void DrawBar(float x, float y, float w, float h, float value, string label, float glow, float scare, Color tint)
    {
        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.normal.textColor = new Color(tint.r, tint.g, tint.b, 0.9f);
        GUI.Label(new Rect(x, y - h * 1.6f, w, h * 1.5f), label, smallStyle);
        if (glow > 0f) DrawBox(new Rect(x - 3f - 4f * glow, y - 3f - 4f * glow, w + 6f + 8f * glow, h + 6f + 8f * glow), new Color(tint.r, tint.g, tint.b, 0.35f * glow));
        DrawBox(new Rect(x - 3f, y - 3f, w + 6f, h + 6f), new Color(0f, 0f, 0f, 0.55f));
        DrawBox(new Rect(x, y, w, h), new Color(0.16f, 0.13f, 0.1f, 0.9f));
        Color fill = Color.Lerp(tint * 0.85f, Color.Lerp(tint, Color.white, 0.5f), value);
        fill.a = 1f;
        DrawBox(new Rect(x, y, w * value, h), Color.Lerp(fill, new Color(0.85f, 0.15f, 0.12f), scare));
        DrawBox(new Rect(x + w * FamilyProgress.FeltAt - 1f, y - 2f, 2f, h + 4f), new Color(1f, 1f, 1f, 0.35f));   // "feels you" mark
    }

    void DrawToast()
    {
        float age = Time.unscaledTime - toastStart;
        if (age > toastLength) return;
        float alpha = Mathf.Clamp01(Mathf.Min(age / 0.25f, (toastLength - age) / 0.5f));
        float w = Screen.width * 0.6f, h = Screen.height * 0.11f;
        Rect box = new Rect((Screen.width - w) / 2f, Screen.height * 0.14f - (1f - alpha) * 10f, w, h);
        DrawBox(box, new Color(0.05f, 0.04f, 0.03f, 0.75f * alpha));
        toastStyle.normal.textColor = new Color(1f, 0.93f, 0.8f, alpha);
        GUI.Label(box, toast, toastStyle);
    }

    void DrawSubtitle()
    {
        float age = Time.unscaledTime - subtitleStart;
        if (age > subtitleLength || subtitleText.Length == 0) return;
        float alpha = Mathf.Clamp01(Mathf.Min(age / 0.3f, (subtitleLength - age) / 0.4f));
        float w = Screen.width * 0.7f, h = Screen.height * 0.14f;
        Rect box = new Rect((Screen.width - w) / 2f, Screen.height * 0.72f, w, h);
        toastStyle.richText = true;
        string speaker = subtitleSpeaker.Length > 0 ? "<color=#ffcc88><size=70%>" + subtitleSpeaker.ToUpper() + "</size></color>\n" : "";
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        toastStyle.normal.textColor = new Color(1f, 0.97f, 0.9f, 1f);
        GUI.Label(box, speaker + subtitleText, toastStyle);
        GUI.color = old;
    }

    void DrawSkipHint()
    {
        if (!skipHint) return;
        smallStyle.alignment = TextAnchor.MiddleRight;
        smallStyle.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
        GUI.Label(new Rect(Screen.width - 340f, Screen.height * 0.9f, 320f, 40f),(MobileControls.Active ? "" : "SPACE / ENTER   skip"), smallStyle);
        smallStyle.alignment = TextAnchor.MiddleLeft;
    }

    void DrawObjective()
    {
        if (objective.Length == 0) return;
        float age = Time.unscaledTime - objectiveTime;
        float alpha = objectiveSticky ? 1f : Mathf.Clamp01(Mathf.Min(age / 0.4f, (ObjectiveSeconds - age) / 1.5f));
        if (alpha <= 0f) return;
        float w = Screen.width * 0.7f, h = Screen.height * 0.09f;
        Rect box = new Rect((Screen.width - w) / 2f, Screen.height * 0.84f, w, h);
        DrawBox(box, new Color(0f, 0f, 0f, 0.5f * alpha));
        objectiveStyle.normal.textColor = new Color(0.9f, 0.95f, 1f, alpha);
        GUI.Label(box, objective, objectiveStyle);
    }

    void DrawCandle()
    {
        float x = Screen.width * 0.02f, y = Screen.height * 0.9f;
        float pip = Screen.height * 0.025f;
        smallStyle.normal.textColor = new Color(1f, 0.85f, 0.6f, 0.9f);
        GUI.Label(new Rect(x, y - pip * 1.6f, 300, pip * 1.5f),burnedOut ? "Candle  (burned out)" : "Candle  " + MobileControls.Label("H") + " hint", smallStyle);

        for (int i = 0; i < hintsMax; i++)
        {
            bool full = i < hintsLeft;
            DrawBox(new Rect(x + i * pip * 1.4f, y, pip, pip), full ? new Color(1f, 0.7f, 0.3f, 0.95f) : new Color(0.25f, 0.2f, 0.15f, 0.7f));
        }
        if (cooldown01 > 0f)
        {
            float barW = hintsMax * pip * 1.4f - pip * 0.4f;
            DrawBox(new Rect(x, y + pip * 1.2f, barW, pip * 0.3f), new Color(0f, 0f, 0f, 0.5f));
            DrawBox(new Rect(x, y + pip * 1.2f, barW * (1f - cooldown01), pip * 0.3f), new Color(1f, 0.6f, 0.2f, 0.9f));
        }
    }

    static void DrawBox(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }
}
