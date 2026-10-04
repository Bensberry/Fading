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
    bool skipHint;
    string subtitleSpeaker = "", subtitleText = "";
    float subtitleStart = -100f, subtitleLength;

    bool progressShown;
    float progress01;
    float progressScareTime = -100f;

    bool candleShown;
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

    public static void SetObjective(string text) { Instance.objective = text ?? ""; }

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
    public static void SetProgress(float value01)
    {
        FadingHud h = Instance;
        h.progressShown = true;
        h.progress01 = Mathf.Clamp01(value01);
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
        DrawProgress();
        DrawToast();
        DrawSkipHint();
        DrawSubtitle();
        DrawObjective();
        if (candleShown) DrawCandle();
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
        float w = Screen.width * 0.34f, h = Screen.height * 0.022f;
        float x = (Screen.width - w) / 2f, y = Screen.height * 0.035f;
        smallStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle.normal.textColor = new Color(1f, 0.93f, 0.8f, 0.9f);
        float scare = Mathf.Clamp01(1f - (Time.unscaledTime - progressScareTime) / 2.5f);
        if (scare > 0f) smallStyle.normal.textColor = Color.Lerp(smallStyle.normal.textColor, new Color(1f, 0.35f, 0.3f, 1f), scare);
        GUI.Label(new Rect(x, y - h * 1.7f, w, h * 1.6f), scare > 0f ? "You frightened them" : "They are starting to feel you", smallStyle);
        smallStyle.alignment = TextAnchor.MiddleLeft;
        DrawBox(new Rect(x - 3f, y - 3f, w + 6f, h + 6f), new Color(0f, 0f, 0f, 0.55f));
        DrawBox(new Rect(x, y, w, h), new Color(0.2f, 0.16f, 0.1f, 0.9f));
        Color fill = Color.Lerp(new Color(1f, 0.6f, 0.2f), new Color(1f, 0.95f, 0.7f), progress01);
        DrawBox(new Rect(x, y, w * progress01, h), Color.Lerp(fill, new Color(0.85f, 0.15f, 0.12f), scare));
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
        GUI.Label(new Rect(Screen.width - 340f, Screen.height * 0.9f, 320f, 40f), "SPACE / ENTER   skip", smallStyle);
        smallStyle.alignment = TextAnchor.MiddleLeft;
    }

    void DrawObjective()
    {
        if (objective.Length == 0) return;
        float w = Screen.width * 0.7f, h = Screen.height * 0.09f;
        Rect box = new Rect((Screen.width - w) / 2f, Screen.height * 0.84f, w, h);
        DrawBox(box, new Color(0f, 0f, 0f, 0.5f));
        objectiveStyle.normal.textColor = new Color(0.9f, 0.95f, 1f, 1f);
        GUI.Label(box, objective, objectiveStyle);
    }

    void DrawCandle()
    {
        float x = Screen.width * 0.02f, y = Screen.height * 0.9f;
        float pip = Screen.height * 0.025f;
        smallStyle.normal.textColor = new Color(1f, 0.85f, 0.6f, 0.9f);
        GUI.Label(new Rect(x, y - pip * 1.6f, 300, pip * 1.5f), burnedOut ? "Candle  (burned out)" : "Candle  [H] hint", smallStyle);

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
