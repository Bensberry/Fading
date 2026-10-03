using UnityEngine;
using UnityEngine.UI;

// Goes in: nowhere by hand. ChapterRules starts it when a game scene loads.
// After PLAY the main menu ends in a blinding warm white. This makes the game scene begin in that same white
// and slowly clear away, so the candle's light flows straight into the game (no black gap, no sudden cut).
// It only runs when the scene was started from the main menu (the menu sets 'pending').
public class LightFadeIn : MonoBehaviour
{
    public static bool pending;                     // set by MainMenuController when PLAY is pressed
    public static Color lightColor = new Color(1f, 0.95f, 0.85f);

    public float holdSeconds = 0.5f;
    public float fadeSeconds = 3f;

    Image overlay;
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
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;                                // unscaled: also works if the game is paused
        float t = Mathf.Clamp01((age - holdSeconds) / fadeSeconds);
        Color c = lightColor;
        c.a = 1f - Mathf.SmoothStep(0f, 1f, t);
        overlay.color = c;
        if (t >= 1f) Destroy(gameObject);
    }
}
