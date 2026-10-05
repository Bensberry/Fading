using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Goes on: one empty object in the MainMenu scene (the scene already has it).
// The menu: a pitch black screen, the title lit by a candle burning on the right, PLAY and QUIT on the left.
// PLAY: the text fades away, the candle flares, then the screen fades to black while the game scene
//       loads in the background, and the game starts.
// PLAY first asks HOW to play (Story / Easy / Medium / Hard, see Difficulty.cs), then the candle flares into the game.
// CREDITS: shows who made what (GameCredits.cs). BACK, Esc or the right mouse button goes back.
// QUIT: closes the game (in the Unity Editor it just stops Play mode).
// Everything is built from code at start, so the scene file stays tiny. The look comes from 'style'
// (see MenuStyle in MenuKit.cs); the candle settings are on the CandleTransition component.
public class MainMenuController : MonoBehaviour
{
    [Header("Scene")]
    public string sceneName = "Chapter0";

    [Header("Candle")]
    [Tooltip("ON: the menu opens in darkness, a match is struck and the candle is lit. OFF: the candle is already burning.")]
    public bool lightCandleOnOpen = true;
    [Tooltip("The match-strike sound. Drag your audio here, or put it at Assets/Resources/Audio/MatchStrike.")]
    public AudioClip matchSound;

    [Header("Look")]
    public MenuStyle style = new MenuStyle();

    [Header("Timing (seconds)")]
    [Tooltip("The menu text slowly appears from the dark when the menu opens.")]
    public float menuFadeInSeconds = 2.5f;
    public float textFadeOutSeconds = 0.8f;
    [Tooltip("How long the screen takes to flood with light after PLAY.")]
    public float fadeDuration = 2f;
    [Tooltip("Seconds after PLAY (and after the text is gone) before the white-out starts, while the flame is swelling.")]
    public float whiteOutDelay = 0.8f;
    [Tooltip("The black text shown while the screen is white after PLAY.")]
    public string flashQuote = LightFadeIn.Quote;
    [Tooltip("How long the quote stays readable on the white screen before the game scene appears.")]
    public float quoteHoldSeconds = 2.5f;
    [Tooltip("The blinding colour the screen fades to. The game scene starts in the same colour.")]
    public Color blindingColor = new Color(1f, 0.95f, 0.85f);
    [Tooltip("The light starts as a tiny glow at the wick of the candle (size in pixels at 1080p)...")]
    public float glowStartSize = 60f;
    [Tooltip("...and grows to this size, which is big enough to cover the whole screen.")]
    public float glowEndSize = 9000f;

    CandleTransition candle;
    Image fadeImage, glowImage;
    RectTransform glowRect;
    TextMeshProUGUI quoteLabel;
    TextMeshProUGUI title;
    MenuButton playButton, creditsButton, quitButton, backButton;
    TextMeshProUGUI creditsText, memoriesText;
    bool showingCredits;
    bool starting;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MakeEverythingBlack();
        MenuKit.EnsureEventSystem();
        MusicPlayer.Create().Play("music_menu", 3f);
        Canvas canvas = MenuKit.MakeCanvas("MenuCanvas", 0);
        title = MenuKit.MakeTitle(canvas.transform, style);
        playButton = MenuKit.MakeOption(canvas.transform, "PLAY", 0, style, OpenLevels);
        creditsButton = MenuKit.MakeOption(canvas.transform, "CREDITS", 1, style, ShowCredits);
        quitButton = MenuKit.MakeOption(canvas.transform, "QUIT", 2, style, QuitGame);
        MakeCredits(canvas.transform);
        MakeLevelPage(canvas.transform);
        memoriesText = MenuKit.MakeLabel(canvas.transform, "Memories", SecretMemories.Summary, new Vector2(0.5f, 0.06f), new Vector2(0.5f, 0.5f),
                                         new Vector2(1600f, 60f), 26, 2f, style, TextAlignmentOptions.Center);
        MakeGlow(canvas.transform);                       // under the full-screen overlay
        fadeImage = MakeFadeOverlay(canvas.transform);
        quoteLabel = MenuKit.MakeLabel(canvas.transform, "FlashQuote", flashQuote, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                                       new Vector2(1500f, 300f), 54, 4f, style, TextAlignmentOptions.Center);
        quoteLabel.color = Color.black;                    // on top of the white
        quoteLabel.alpha = 0f;
        SetTextVisibility(0f);

        // The CandleTransition component sits on this same object in the scene, so its settings can be tuned in the Inspector.
        candle = GetComponent<CandleTransition>();
        if (candle == null) candle = gameObject.AddComponent<CandleTransition>();
        if (matchSound != null) candle.matchSound = matchSound;
        else if (candle.matchSound == null) candle.matchSound = Resources.Load<AudioClip>("Audio/MatchStrike");
        candle.Build(Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>());

        StartCoroutine(OpenMenu());
    }

    // The candle gets lit (or is already burning), then the title and options slowly appear.
    IEnumerator OpenMenu()
    {
        if (lightCandleOnOpen) yield return candle.LightUp();
        else
        {
            candle.ShowLit();
            yield return new WaitForSeconds(0.5f);
        }
        yield return FadeText(0f, 1f, menuFadeInSeconds);
    }

    // ---------- setting up
    static void MakeEverythingBlack()
    {
        RenderSettings.skybox = null;
        RenderSettings.fog = false;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
    }

    // The blinding light: a soft round glow image. It starts at the candle's wick and grows until it fills the screen.
    void MakeGlow(Transform parent)
    {
        GameObject g = new GameObject("CandleGlow", typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent, false);
        glowRect = (RectTransform)g.transform;
        glowImage = g.GetComponent<Image>();
        glowImage.sprite = MakeGlowSprite();
        glowImage.color = new Color(blindingColor.r, blindingColor.g, blindingColor.b, 0f);
        glowImage.raycastTarget = false;
    }

    // A white circle that is solid in the middle and fades softly to nothing at the edge.
    static Sprite MakeGlowSprite()
    {
        const int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                float a = Mathf.Pow(Mathf.Clamp01(1f - d), 1.6f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = false;
        return image;
    }

    // ---------- difficulty (after PLAY)
    MenuButton[] levelButtons;
    TextMeshProUGUI[] levelNotes;
    TextMeshProUGUI levelHeader;
    MenuButton levelBack;
    bool choosing;

    void MakeLevelPage(Transform canvas)
    {
        int small = Mathf.RoundToInt(style.optionFontSize * 0.55f);
        levelHeader = MenuKit.MakeLabel(canvas, "LevelHeader", "HOW DO YOU WANT TO REMEMBER?", new Vector2(style.leftMargin, style.firstOptionHeight + 0.11f),
                                        new Vector2(0f, 0.5f), new Vector2(1400f, 80f), Mathf.RoundToInt(style.optionFontSize * 0.7f), style.optionSpacing, style,
                                        TextAlignmentOptions.MidlineLeft);
        levelButtons = new MenuButton[4];
        levelNotes = new TextMeshProUGUI[4];
        for (int i = 0; i < 4; i++)
        {
            Difficulty.Level level = (Difficulty.Level)i;
            levelButtons[i] = MenuKit.MakeOption(canvas, Difficulty.Name(level), i, style, () => ChooseLevel(level));
            float height = style.firstOptionHeight - i * style.optionGap;
            levelNotes[i] = MenuKit.MakeLabel(canvas, "LevelNote" + i, Difficulty.Describe(level), new Vector2(style.leftMargin + 0.2f, height),
                                              new Vector2(0f, 0.5f), new Vector2(1200f, 80f), small, 1f, style, TextAlignmentOptions.MidlineLeft);
        }
        levelBack = MenuKit.MakeOption(canvas, "BACK", 4, style, () => { if (choosing) StartCoroutine(SwitchToLevels(false)); });
        SetLevelPage(0f);
        SetLevelButtons(false);
    }

    void SetLevelPage(float v)
    {
        // A hidden page is switched OFF completely: its invisible buttons sit on top of PLAY / CREDITS / QUIT
        // and would otherwise catch every click.
        bool on = v > 0.001f;
        levelHeader.gameObject.SetActive(on);
        for (int i = 0; i < 4; i++) { levelButtons[i].gameObject.SetActive(on); levelNotes[i].gameObject.SetActive(on); }
        levelBack.gameObject.SetActive(on);
        levelHeader.alpha = v * 0.8f;
        for (int i = 0; i < 4; i++) { levelButtons[i].Visibility = v; levelNotes[i].alpha = v * 0.7f; }
        levelBack.Visibility = v;
    }

    void SetLevelButtons(bool on)
    {
        foreach (MenuButton b in levelButtons) b.Interactable = on;
        levelBack.Interactable = on;
    }

    void OpenLevels()
    {
        if (starting || choosing || showingCredits) return;
        choosing = true;
        StartCoroutine(SwitchToLevels(true));
    }

    IEnumerator SwitchToLevels(bool toLevels)
    {
        if (!toLevels) choosing = false;
        SetMainOptionsActive(true);                                   // (they are switched off while the levels are shown)
        playButton.Interactable = creditsButton.Interactable = quitButton.Interactable = false;
        SetLevelButtons(false);
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 0.5f);
            SetTextVisibility(toLevels ? 1f - k : k);
            SetLevelPage(toLevels ? k : 1f - k);
            yield return null;
        }
        SetTextVisibility(toLevels ? 0f : 1f);
        SetLevelPage(toLevels ? 1f : 0f);
        SetLevelButtons(toLevels);
        playButton.Interactable = creditsButton.Interactable = quitButton.Interactable = !toLevels;
        SetMainOptionsActive(!toLevels);                              // PLAY / CREDITS / QUIT are gone while you choose
    }

    void SetMainOptionsActive(bool on)
    {
        playButton.gameObject.SetActive(on);
        creditsButton.gameObject.SetActive(on);
        quitButton.gameObject.SetActive(on);
    }

    void ChooseLevel(Difficulty.Level level)
    {
        if (!choosing || starting) return;
        Difficulty.Current = level;                         // saved: GameSettings, FamilyFear, FamilyProgress ... read it
        StartCoroutine(BeginAtLevel());
    }

    IEnumerator BeginAtLevel()
    {
        SetLevelButtons(false);
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            SetLevelPage(1f - Mathf.SmoothStep(0f, 1f, t / 0.6f));
            yield return null;
        }
        SetLevelPage(0f);
        choosing = false;
        StartGame();                                        // the candle flares and floods the screen with light, as before
    }

    // ---------- credits
    void MakeCredits(Transform canvas)
    {
        creditsText = MenuKit.MakeLabel(canvas, "Credits", GameCredits.Text, new Vector2(0.5f, 0.54f), new Vector2(0.5f, 0.5f),
                                        new Vector2(1700f, 860f), 30, 1f, style, TextAlignmentOptions.Center);
        creditsText.richText = true;
        creditsText.alpha = 0f;
        backButton = MenuKit.MakeOption(canvas, "BACK", 0, style, HideCredits);
        RectTransform rt = (RectTransform)backButton.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.07f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        backButton.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        backButton.Visibility = 0f;
        backButton.Interactable = false;
        backButton.gameObject.SetActive(false);                  // switched on only while the credits are shown
        creditsText.gameObject.SetActive(false);
    }

    void ShowCredits()
    {
        if (starting || showingCredits) return;
        showingCredits = true;
        StartCoroutine(SwitchPage(true));
    }

    void HideCredits()
    {
        if (!showingCredits) return;
        showingCredits = false;
        StartCoroutine(SwitchPage(false));
    }

    IEnumerator SwitchPage(bool toCredits)
    {
        backButton.gameObject.SetActive(true);
        creditsText.gameObject.SetActive(true);
        playButton.Interactable = creditsButton.Interactable = quitButton.Interactable = false;
        backButton.Interactable = false;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 0.5f);
            SetTextVisibility(toCredits ? 1f - k : k);
            creditsText.alpha = toCredits ? k : 1f - k;
            backButton.Visibility = creditsText.alpha;
            yield return null;
        }
        SetTextVisibility(toCredits ? 0f : 1f);
        creditsText.alpha = backButton.Visibility = toCredits ? 1f : 0f;
        backButton.Interactable = toCredits;
        backButton.gameObject.SetActive(toCredits);              // hidden page = switched off, so it cannot block clicks
        creditsText.gameObject.SetActive(toCredits);
        playButton.Interactable = creditsButton.Interactable = quitButton.Interactable = !toCredits;
    }

    void Update()
    {
        if (!showingCredits && !choosing) return;
        bool back =  (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame) ||
                    (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame);
        if (!back) return;
        if (showingCredits) HideCredits();
        else if (choosing && levelBack.Interactable) StartCoroutine(SwitchToLevels(false));
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
        GameAudio.Play("flash_swell", 1f);                 // the sound of the candle's light swelling
        playButton.Interactable = false;
        creditsButton.Interactable = false;
        quitButton.Interactable = false;
        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // The game scene loads in the background while the candle flares. It waits at 90% until we say go.
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;

        if (title.alpha > 0.01f) yield return FadeText(1f, 0f, textFadeOutSeconds);          // the text disappears, leaving the candle

        // The flame swells until it is blinding; the screen floods with light while it does.
        whiteOutDone = false;
        StartCoroutine(WhiteOut());
        yield return candle.Play();
        while (!whiteOutDone) yield return null;
        yield return new WaitForSeconds(quoteHoldSeconds);        // the quote stays readable on the white screen

        // The game scene starts in the same blinding light and slowly clears (see LightFadeIn).
        LightFadeIn.lightColor = blindingColor;
        LightFadeIn.pending = true;
        while (load.progress < 0.9f) yield return null;
        load.allowSceneActivation = true;
    }

    bool whiteOutDone;

    // The light is born at the wick and spreads outward until the whole screen is white.
    IEnumerator WhiteOut()
    {
        yield return new WaitForSeconds(whiteOutDelay);

        Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        Vector3 wick = cam.WorldToViewportPoint(candle.WickPosition);
        glowRect.anchorMin = glowRect.anchorMax = new Vector2(wick.x, wick.y);     // sit exactly on the wick
        glowRect.anchoredPosition = Vector2.zero;

        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            float k = t / fadeDuration;
            float size = Mathf.Lerp(glowStartSize, glowEndSize, k * k);            // slow at first, then it rushes outward
            glowRect.sizeDelta = new Vector2(size, size);
            SetOverlay(glowImage, Mathf.Clamp01(k * 4f));
            SetOverlay(fadeImage, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 1f, k)));   // makes sure it ends fully white
            quoteLabel.alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.9f, k));      // the quote appears as the white takes over
            yield return null;
        }
        glowRect.sizeDelta = new Vector2(glowEndSize, glowEndSize);
        SetOverlay(glowImage, 1f);
        SetOverlay(fadeImage, 1f);
        quoteLabel.alpha = 1f;
        whiteOutDone = true;
    }

    void SetOverlay(Image image, float alpha)
    {
        image.color = new Color(blindingColor.r, blindingColor.g, blindingColor.b, alpha);
    }

    // ---------- fades
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
        title.alpha = v;
        if (memoriesText != null) memoriesText.alpha = v * 0.75f;
        playButton.Visibility = v;
        creditsButton.Visibility = v;
        quitButton.Visibility = v;
    }
}
