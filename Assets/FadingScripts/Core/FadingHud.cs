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
        DrawToast();
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
