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

    [Header("Look")]
    public MenuStyle style = new MenuStyle();

    [Header("Timing (seconds)")]
    [Tooltip("The menu text slowly appears from the dark when the menu opens.")]
    public float menuFadeInSeconds = 2.5f;
    public float textFadeOutSeconds = 0.8f;
    [Tooltip("Black fade after the candle flares, before the game scene appears.")]
    public float fadeDuration = 2f;

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

        candle = gameObject.AddComponent<CandleTransition>();
        candle.Build(Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>());
        candle.ShowLit();

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
        // The game scene loads in the background while the candle flares. It waits at 90% until we say go.
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;

        yield return FadeText(1f, 0f, textFadeOutSeconds);          // the text disappears, leaving the candle
        yield return candle.Play();                                  // the flame swells and burns
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
        title.alpha = v;
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
