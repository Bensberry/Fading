using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Goes in: nowhere by hand. ChapterRules adds it to the game scenes (Chapter0 / Chapter1).
// Press Esc: the game freezes and the title page from the main menu appears (black screen, lit candle),
// with RESUME, SETTINGS, MAIN MENU and QUIT. Esc again resumes (Esc inside SETTINGS goes back).
// SETTINGS has three sliders: FOG, LIGHTING and SENSITIVITY. They are saved between runs (see GameSettings).
// Below them OLD PHONE MODE (on / off) with a GRAPHICS slider for older phones (see MobilePerformance).
//
// How the black candle page covers the game: a second camera far away from the house looks at a candle,
// drawn over everything. The game's sun/moon and ambient light are switched off while paused, so they can't light it.
public class PauseMenu : MonoBehaviour
{
    public MenuStyle style = new MenuStyle();
    public string mainMenuScene = "MainMenu";

    bool paused, inSettings;

    // True while the pause menu is open (other scripts hide their on-screen hints then).
    public static bool IsOpen { get; private set; }
    GameObject pauseCamera;
    Canvas canvas;
    GameObject optionsGroup, settingsGroup;

    readonly List<Behaviour> switchedOff = new List<Behaviour>();
    readonly List<Light> lightsOff = new List<Light>();
    Color savedAmbient;
    UnityEngine.Rendering.AmbientMode savedAmbientMode;
    bool savedFog;

    void Update()
    {
        if (CutsceneRunner.IsPlaying) return;                    // no pausing during a cutscene (Space skips it)
        bool pressed = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) || MobileControls.PausePressed;   // Esc, Android back, or II
        if (!pressed) return;
        if (!paused) Pause();
        else if (inSettings) ShowOptions();
        else Resume();
    }

    // ---------- pausing
    void Pause()
    {
        paused = true;
        IsOpen = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        SwitchOffGameplay();
        MakeDarkness();
        BuildCandlePage();
        BuildMenu();
        ShowOptions();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Resume()
    {
        paused = false;
        IsOpen = false;
        inSettings = false;
        if (canvas != null) Destroy(canvas.gameObject);
        if (pauseCamera != null) Destroy(pauseCamera);
        RestoreLights();
        SwitchOnGameplay();
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameSettings.Save();
    }

    // While paused the player must not move, touch things, use hints or skip the day with N.
    void SwitchOffGameplay()
    {
        DisableAll<FirstPersonController>();
        DisableAll<PlayerInteractor>();
        DisableAll<CandleHint>();
        DisableAll<PrologueTutorial>();
        DisableAll<LampInteraction>();
        DisableAll<ClockInteraction>();
        DisableAll<PhotoAlbumInteraction>();
        DisableAll<DayNightCycle>();
    }

    void DisableAll<T>() where T : Behaviour
    {
        foreach (T b in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            if (!b.enabled) continue;
            b.enabled = false;
            switchedOff.Add(b);
        }
    }

    void SwitchOnGameplay()
    {
        foreach (Behaviour b in switchedOff) if (b != null) b.enabled = true;
        switchedOff.Clear();
    }

    // Sun, moon and ambient light would light the candle page, so they are switched off while paused.
    void MakeDarkness()
    {
        foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type != LightType.Directional || !l.enabled) continue;
            l.enabled = false;
            lightsOff.Add(l);
        }
        savedAmbientMode = RenderSettings.ambientMode;
        savedAmbient = RenderSettings.ambientLight;
        savedFog = RenderSettings.fog;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = Color.black;
        RenderSettings.fog = false;
    }

    void RestoreLights()
    {
        foreach (Light l in lightsOff) if (l != null) l.enabled = true;
        lightsOff.Clear();
        RenderSettings.ambientMode = savedAmbientMode;
        RenderSettings.ambientLight = savedAmbient;
        RenderSettings.fog = savedFog;
    }

    // ---------- the page
    void BuildCandlePage()
    {
        pauseCamera = new GameObject("PauseCamera");
        pauseCamera.transform.position = new Vector3(5000f, 5000f, 5000f);      // far away from the house
        Camera cam = pauseCamera.AddComponent<Camera>();
        cam.depth = 100f;                                                        // drawn on top of the game camera
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        CandleTransition candle = pauseCamera.AddComponent<CandleTransition>();
        candle.Build(cam);
        candle.ShowLit();
    }

    void BuildMenu()
    {
        MenuKit.EnsureEventSystem();
        canvas = MenuKit.MakeCanvas("PauseCanvas", 500);
        MenuKit.MakeTitle(canvas.transform, style);

        optionsGroup = MakeGroup("Options");
        MenuKit.MakeOption(optionsGroup.transform, "RESUME", 0, style, Resume);
        MenuKit.MakeOption(optionsGroup.transform, "SETTINGS", 1, style, ShowSettings);
        MenuKit.MakeOption(optionsGroup.transform, "MAIN MENU", 2, style, GoToMainMenu);
        MenuKit.MakeOption(optionsGroup.transform, "QUIT", 3, style, QuitGame);

        settingsGroup = MakeGroup("Settings");
        MenuKit.MakeSlider(settingsGroup.transform, "Fog", 0.64f, style, 0f, 100f, GameSettings.Fog, v => Mathf.RoundToInt(v).ToString(),
                           v => { GameSettings.Fog = v; GameSettings.ApplyFog(); });
        MenuKit.MakeSlider(settingsGroup.transform, "Lighting", 0.53f, style, 0f, 100f, GameSettings.Lighting, v => Mathf.RoundToInt(v).ToString(),
                           v => { GameSettings.Lighting = v; GameSettings.ApplyLighting(); KeepDarkWhilePaused(); });
        MenuKit.MakeSlider(settingsGroup.transform, "Sensitivity", 0.42f, style, 0f, 100f, GameSettings.Sensitivity, v => Mathf.RoundToInt(v).ToString(),
                           v => { GameSettings.Sensitivity = v; GameSettings.ApplySensitivity(); });
        BuildOldPhoneMode();
        PlaceAt(MenuKit.MakeOption(settingsGroup.transform, "BACK", 4, style, ShowOptions), 0.08f);
    }

    // OLD PHONE MODE: ON / OFF. When it is on, a GRAPHICS slider appears (Lowest .. High). See MobilePerformance.
    MenuButton oldPhoneButton;
    GameObject graphicsSlider, graphicsLabel;

    void BuildOldPhoneMode()
    {
        oldPhoneButton = MenuKit.MakeOption(settingsGroup.transform, OldPhoneText(), 3, style, ToggleOldPhoneMode);
        PlaceAt(oldPhoneButton, 0.30f);

        Slider slider = MenuKit.MakeSlider(settingsGroup.transform, "Graphics", 0.18f, style, 0f, 3f, MobilePerformance.Level,
                                           v => MobilePerformance.LevelNames[Mathf.RoundToInt(v)],
                                           v => MobilePerformance.Level = Mathf.RoundToInt(v));
        slider.wholeNumbers = true;
        graphicsSlider = slider.gameObject;
        Transform label = settingsGroup.transform.Find("GraphicsLabel");
        graphicsLabel = label != null ? label.gameObject : null;
        ShowGraphicsSlider();
    }

    void ToggleOldPhoneMode()
    {
        MobilePerformance.OldPhoneMode = !MobilePerformance.OldPhoneMode;
        TMPro.TextMeshProUGUI text = oldPhoneButton.GetComponent<TMPro.TextMeshProUGUI>();
        if (text != null) text.text = OldPhoneText();
        ShowGraphicsSlider();
    }

    static string OldPhoneText() { return "OLD PHONE MODE   " + (MobilePerformance.OldPhoneMode ? "ON" : "OFF"); }

    void ShowGraphicsSlider()
    {
        bool on = MobilePerformance.OldPhoneMode;
        if (graphicsSlider != null) graphicsSlider.SetActive(on);
        if (graphicsLabel != null) graphicsLabel.SetActive(on);
    }

    // Put a menu option at this height of the screen (0 = bottom, 1 = top).
    void PlaceAt(MenuButton button, float height)
    {
        RectTransform rt = (RectTransform)button.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(style.leftMargin, height);
    }

    // ApplyLighting also sets the game's ambient light; remember it for later and keep the page dark.
    void KeepDarkWhilePaused()
    {
        savedAmbient = RenderSettings.ambientLight;
        RenderSettings.ambientLight = Color.black;
    }

    // A full-screen empty container, so the options/sliders inside it position themselves by screen fractions.
    GameObject MakeGroup(string name)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(canvas.transform, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return g;
    }

    void ShowOptions()
    {
        inSettings = false;
        optionsGroup.SetActive(true);
        settingsGroup.SetActive(false);
    }

    void ShowSettings()
    {
        inSettings = true;
        optionsGroup.SetActive(false);
        settingsGroup.SetActive(true);
    }

    // ---------- buttons
    void GoToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        GameSettings.Save();
        SceneManager.LoadScene(mainMenuScene);
    }

    void QuitGame()
    {
        GameSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        // If the scene is left while paused (e.g. MAIN MENU), never leave the game frozen.
        if (paused) { Time.timeScale = 1f; AudioListener.pause = false; }
        IsOpen = false;
    }
}
