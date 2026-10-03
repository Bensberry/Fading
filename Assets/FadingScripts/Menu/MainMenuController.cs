using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Goes on: one empty object in the MainMenu scene (the scene already has it).
// The menu: a pitch black screen, the title lit by a candle burning on the right, PLAY and QUIT on the left.
// PLAY: the text fades away, the candle flares, then the screen fades to black while the game scene
//       loads in the background, and the game starts.
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
    [Tooltip("The blinding colour the screen fades to. The game scene starts in the same colour.")]
    public Color blindingColor = new Color(1f, 0.95f, 0.85f);

    CandleTransition candle;
    Image fadeImage;
    TextMeshProUGUI title;
    MenuButton playButton, quitButton;
    bool starting;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        MakeEverythingBlack();
        MenuKit.EnsureEventSystem();
        Canvas canvas = MenuKit.MakeCanvas("MenuCanvas", 0);
        title = MenuKit.MakeTitle(canvas.transform, style);
        playButton = MenuKit.MakeOption(canvas.transform, "PLAY", 0, style, StartGame);
        quitButton = MenuKit.MakeOption(canvas.transform, "QUIT", 1, style, QuitGame);
        fadeImage = MakeFadeOverlay(canvas.transform);
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
        // The game scene loads in the background while the candle flares. It waits at 90% until we say go.
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;

        yield return FadeText(1f, 0f, textFadeOutSeconds);          // the text disappears, leaving the candle

        // The flame swells until it is blinding; the screen floods with light while it does.
        whiteOutDone = false;
        StartCoroutine(WhiteOut());
        yield return candle.Play();
        while (!whiteOutDone) yield return null;

        // The game scene starts in the same blinding light and slowly clears (see LightFadeIn).
        LightFadeIn.lightColor = blindingColor;
        LightFadeIn.pending = true;
        while (load.progress < 0.9f) yield return null;
        load.allowSceneActivation = true;
    }

    bool whiteOutDone;

    IEnumerator WhiteOut()
    {
        yield return new WaitForSeconds(whiteOutDelay);
        yield return FadeScreen(0f, 1f, fadeDuration);
        whiteOutDone = true;
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
        playButton.Visibility = v;
        quitButton.Visibility = v;
    }

    IEnumerator FadeScreen(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            fadeImage.color = new Color(blindingColor.r, blindingColor.g, blindingColor.b, Mathf.SmoothStep(from, to, t / seconds));
            yield return null;
        }
        fadeImage.color = new Color(blindingColor.r, blindingColor.g, blindingColor.b, to);
    }
}
