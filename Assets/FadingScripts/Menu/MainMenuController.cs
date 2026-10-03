using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Goes on: one empty object in the MainMenu scene (the scene already has it).
// The menu: a pitch black screen with only PLAY and QUIT in white on the left.
// PLAY: the text fades away, a candle is revealed on the right and lit, then the screen fades to black
//       while the game scene loads in the background, and the game starts.
// QUIT: closes the game (in the Unity Editor it just stops Play mode).
// Everything is built from code at start, so the scene file stays tiny. All settings are public fields.
public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    public string sceneName = "Chapter0";

    [Header("Text")]
    [Tooltip("Optional. Drag a TextMeshPro font asset here to change the font. Empty = the project's default font.")]
    public TMP_FontAsset font;
    public int fontSize = 64;
    public float letterSpacing = 16f;
    public Color textColor = Color.white;
    [Tooltip("Distance of the text from the left edge (0-1 of the screen width).")]
    [Range(0f, 0.5f)] public float leftMargin = 0.08f;
    [Tooltip("Height of PLAY and QUIT on the screen (0 = bottom, 1 = top).")]
    [Range(0f, 1f)] public float playHeight = 0.56f;
    [Range(0f, 1f)] public float quitHeight = 0.44f;

    [Header("Text hover effect")]
    [Tooltip("How faded the text looks when the mouse is not on it. Lower = fainter.")]
    [Range(0f, 1f)] public float normalAlpha = 0.35f;
    [Range(0f, 1f)] public float hoverAlpha = 1f;
    public float hoverScale = 1.05f;

    [Header("Timing (seconds)")]
    [Tooltip("The menu text slowly appears from the dark when the menu opens.")]
    public float menuFadeInSeconds = 2f;
    public float textFadeOutSeconds = 0.8f;
    [Tooltip("Black fade after the candle is lit, before the game scene appears.")]
    public float fadeDuration = 2f;

    CandleTransition candle;
    Image fadeImage;
    MenuButton playButton, quitButton;
    bool starting;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MakeEverythingBlack();
        EnsureEventSystem();
        Canvas canvas = MakeCanvas();
        playButton = MakeButton(canvas.transform, "PLAY", playHeight, StartGame);
        quitButton = MakeButton(canvas.transform, "QUIT", quitHeight, QuitGame);
        fadeImage = MakeFadeOverlay(canvas.transform);

        candle = gameObject.AddComponent<CandleTransition>();
        candle.Build(Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>());

        StartCoroutine(FadeInText());
    }

    // ---------- setting up
    static void MakeEverythingBlack()
    {
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
    }

    // Clicks only work if the scene has an EventSystem. The project uses the new Input System.
    static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        GameObject g = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        g.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    // The canvas scales with the screen, so the menu looks the same on every resolution and aspect ratio.
    static Canvas MakeCanvas()
    {
        GameObject g = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = g.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = g.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    MenuButton MakeButton(Transform parent, string text, float height, UnityEngine.Events.UnityAction onClick)
    {
        GameObject g = new GameObject(text, typeof(RectTransform));
        g.transform.SetParent(parent, false);

        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(leftMargin, height);
        rt.pivot = new Vector2(0f, 0.5f);                 // grows from its left edge, so it stays lined up
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(520f, 120f);           // the hover/click area

        TextMeshProUGUI label = g.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.characterSpacing = letterSpacing;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = true;

        MenuButton button = g.AddComponent<MenuButton>();
        button.normalAlpha = normalAlpha;
        button.hoverAlpha = hoverAlpha;
        button.hoverScale = hoverScale;
        button.Visibility = 0f;
        button.onClick.AddListener(onClick);
        return button;
    }

    static Image MakeFadeOverlay(Transform parent)
    {
        GameObject g = new GameObject("FadeToBlack", typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        Image image = g.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0f);
        image.raycastTarget = false;
        return image;
    }

    // ---------- buttons
    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void StartGame()
    {
        if (starting) return;
        starting = true;
        playButton.Interactable = false;
        quitButton.Interactable = false;
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // The game scene loads in the background while the candle is lit. It waits at 90% until we say go.
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;

        yield return FadeText(1f, 0f, textFadeOutSeconds);          // the text disappears, leaving the dark
        yield return candle.Play();                                  // reveal, ignite, burn
        yield return FadeScreen(0f, 1f, fadeDuration);               // fade to black

        while (load.progress < 0.9f) yield return null;
        load.allowSceneActivation = true;
    }

    // ---------- fades
    IEnumerator FadeInText()
    {
        yield return new WaitForSeconds(0.5f);
        yield return FadeText(0f, 1f, menuFadeInSeconds);
    }

    IEnumerator FadeText(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            SetTextVisibility(Mathf.SmoothStep(from, to, t / seconds));
            yield return null;
        }
        SetTextVisibility(to);
    }

    void SetTextVisibility(float v)
    {
        playButton.Visibility = v;
        quitButton.Visibility = v;
    }

    IEnumerator FadeScreen(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            fadeImage.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(from, to, t / seconds));
            yield return null;
        }
        fadeImage.color = new Color(0f, 0f, 0f, to);
    }
}
