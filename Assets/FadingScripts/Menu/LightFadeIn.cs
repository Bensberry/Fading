using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Goes in: nowhere by hand. ChapterRules starts it when a game scene loads.
// After PLAY the main menu ends in a blinding warm white. This makes the game scene begin in that same white
// and slowly clear away, so the candle's light flows straight into the game (no black gap, no sudden cut).
// It only runs when the scene was started from the main menu (the menu sets 'pending').
public class LightFadeIn : MonoBehaviour
{
    public const string Quote = "Grief is a ghost that refuses to admit it is dead.";   // shown in black on the white flash

    public static bool pending;                     // set by MainMenuController when PLAY is pressed (or by BlindThenLoad)
    public static string caption = Quote;           // the black text on the white (the quote after the menu, "Day 2" between chapters)
    public static Color lightColor = new Color(1f, 0.95f, 0.85f);

    public float holdSeconds = 0.5f;
    public float fadeSeconds = 3f;

    Image overlay;
    TextMeshProUGUI quote;
    float age;

    public static void StartIfPending()
    {
        if (!pending) return;
        pending = false;
        new GameObject("LightFadeIn").AddComponent<LightFadeIn>();
    }

    void Awake()
    {
        Canvas canvas = MenuKit.MakeCanvas("LightFadeInCanvas", 1000);     // on top of everything
        canvas.transform.SetParent(transform, false);

        GameObject g = new GameObject("Light", typeof(RectTransform), typeof(Image));
        g.transform.SetParent(canvas.transform, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        overlay = g.GetComponent<Image>();
        overlay.color = lightColor;
        overlay.raycastTarget = false;

        // The quote from the menu's flash carries on, in black, and fades away together with the white.
        quote = MenuKit.MakeLabel(canvas.transform, "FlashQuote", caption, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                  new Vector2(1500f, 300f), 54, 4f, new MenuStyle(), TextAlignmentOptions.Center);
        quote.color = Color.black;
        caption = Quote;                            // the next fade-in shows the quote again unless someone changes it
    }

    // A new day: the screen fills with a blinding light (about 2.5 s), then the next chapter loads and clears out of the
    // same light, showing 'text' (e.g. "Day 2") in black for a moment.
    public static void BlindThenLoad(MonoBehaviour host, string scene, string text)
    {
        host.StartCoroutine(Blind(scene, text));
    }

    static System.Collections.IEnumerator Blind(string scene, string text)
    {
        Canvas canvas = MenuKit.MakeCanvas("DaybreakCanvas", 1000);
        GameObject g = new GameObject("Light", typeof(RectTransform), typeof(Image));
        g.transform.SetParent(canvas.transform, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image white = g.GetComponent<Image>();
        white.raycastTarget = false;
        GameAudio.Play("flash_swell", 0.8f);

        const float seconds = 2.5f;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            Color c = lightColor;
            c.a = Mathf.SmoothStep(0f, 1f, t / seconds);
            white.color = c;
            yield return null;
        }
        white.color = lightColor;
        yield return new WaitForSecondsRealtime(0.3f);

        pending = true;
        caption = text;
        UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;                                // unscaled: also works if the game is paused
        float t = Mathf.Clamp01((age - holdSeconds) / fadeSeconds);
        Color c = lightColor;
        c.a = 1f - Mathf.SmoothStep(0f, 1f, t);
        overlay.color = c;
        quote.alpha = c.a;
        if (t >= 1f) Destroy(gameObject);
    }
}
