using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Goes in: nowhere (it is a toolbox). The main menu and the pause menu both use it,
// so they always look exactly the same. It has two parts:
//   MenuStyle = every look setting (title, font, sizes, colours, positions) as Inspector fields
//   MenuKit   = small helpers that build the canvas, title, options and sliders from code

[Serializable]
public class MenuStyle
{
    [Header("Font")]
    [Tooltip("Optional. Empty = uses the font file in Assets/Resources/Fonts, or the project's default font.")]
    public TMP_FontAsset font;

    [Header("Title")]
    public string title = "FADED";
    public int titleFontSize = 150;
    public float titleSpacing = 45f;
    [Tooltip("Where the title sits (0,0 = bottom-left of the screen, 1,1 = top-right).")]
    public Vector2 titlePosition = new Vector2(0.5f, 0.565f);
    [Tooltip("The letters fade from this colour on the left...")]
    public Color titleLeftColor = new Color(0.42f, 0.40f, 0.38f);
    [Tooltip("...to this warm colour on the right, as if lit by the candle.")]
    public Color titleRightColor = new Color(1f, 0.8f, 0.55f);

    [Header("Options (PLAY, QUIT ...)")]
    public int optionFontSize = 42;
    public float optionSpacing = 24f;
    public Color textColor = Color.white;
    [Range(0f, 0.5f)] public float leftMargin = 0.05f;
    [Tooltip("Height of the first option on the screen (0 = bottom, 1 = top).")]
    [Range(0f, 1f)] public float firstOptionHeight = 0.594f;
    [Tooltip("Vertical distance between options (fraction of the screen height).")]
    [Range(0.02f, 0.3f)] public float optionGap = 0.104f;

    [Header("Hover effect")]
    [Range(0f, 1f)] public float normalAlpha = 0.75f;
    [Range(0f, 1f)] public float hoverAlpha = 1f;
    public float hoverScale = 1.05f;
}

public static class MenuKit
{
    static TMP_FontAsset loadedFont;

    // Font priority: 1) the one assigned in the style  2) a font file in Assets/Resources/Fonts  3) TextMeshPro's default
    public static TMP_FontAsset ResolveFont(MenuStyle style)
    {
        if (style.font != null) return style.font;
        if (loadedFont != null) return loadedFont;

        Font[] fonts = Resources.LoadAll<Font>("Fonts");
        if (fonts.Length > 0) loadedFont = TMP_FontAsset.CreateFontAsset(fonts[0]);
        return loadedFont;      // null = TextMeshPro uses its default font
    }

    // Clicks only work if the scene has an EventSystem. The project uses the new Input System.
    public static void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
        GameObject g = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
        g.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
    }

    // A canvas that scales with the screen, so everything looks the same on every resolution and aspect ratio.
    public static Canvas MakeCanvas(string name, int sortingOrder)
    {
        GameObject g = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = g.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = g.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static TextMeshProUGUI MakeLabel(Transform parent, string name, string text, Vector2 anchor, Vector2 pivot,
                                            Vector2 size, int fontSize, float spacing, MenuStyle style, TextAlignmentOptions align)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        TextMeshProUGUI label = g.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = ResolveFont(style);
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.characterSpacing = spacing;
        label.color = style.textColor;
        label.alignment = align;
        label.raycastTarget = false;
        return label;
    }

    // The big title, lit from the right (the candle side) with a colour gradient.
    public static TextMeshProUGUI MakeTitle(Transform parent, MenuStyle s)
    {
        TextMeshProUGUI t = MakeLabel(parent, "Title", s.title, s.titlePosition, new Vector2(0.5f, 0.5f),
                                      new Vector2(1500f, 260f), s.titleFontSize, s.titleSpacing, s, TextAlignmentOptions.Center);
        t.color = Color.white;
        t.enableVertexGradient = true;
        t.colorGradient = new VertexGradient(s.titleLeftColor, s.titleRightColor, s.titleLeftColor, s.titleRightColor);
        return t;
    }

    // One text option on the left side (index 0 = the top one).
    public static MenuButton MakeOption(Transform parent, string text, int index, MenuStyle s, UnityAction onClick)
    {
        float height = s.firstOptionHeight - index * s.optionGap;
        TextMeshProUGUI label = MakeLabel(parent, text, text, new Vector2(s.leftMargin, height), new Vector2(0f, 0.5f),
                                          new Vector2(620f, 120f), s.optionFontSize, s.optionSpacing, s, TextAlignmentOptions.MidlineLeft);
        label.raycastTarget = true;

        MenuButton button = label.gameObject.AddComponent<MenuButton>();
        button.normalAlpha = s.normalAlpha;
        button.hoverAlpha = s.hoverAlpha;
        button.hoverScale = s.hoverScale;
        button.onClick.AddListener(onClick);
        return button;
    }

    // A thin-line slider with a small round handle. The label above it shows the name and the current value.
    public static Slider MakeSlider(Transform parent, string name, float height, MenuStyle s, float min, float max,
                                    float value, Func<float, string> format, UnityAction<float> onChange)
    {
        TextMeshProUGUI label = MakeLabel(parent, name + "Label", "", new Vector2(s.leftMargin, height + 0.035f), new Vector2(0f, 0.5f),
                                          new Vector2(700f, 60f), Mathf.RoundToInt(s.optionFontSize * 0.7f), s.optionSpacing * 0.7f, s,
                                          TextAlignmentOptions.MidlineLeft);

        GameObject root = new GameObject(name + "Slider", typeof(RectTransform), typeof(Image), typeof(Slider));
        root.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)root.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(s.leftMargin, height);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(560f, 36f);
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);      // invisible, but lets you click anywhere on the line

        // The slider stretches its fill and handle over the full height of their container,
        // so each one lives in a thin container of its own (a 2 px line, and a 16 px tall handle strip).
        RectTransform lineArea = MakeStrip(root.transform, "LineArea", 2f);
        MakeBarImage(lineArea, "Line", new Color(1f, 1f, 1f, 0.25f));
        RectTransform fillArea = MakeStrip(root.transform, "FillArea", 2f);
        RectTransform fill = MakeBarImage(fillArea, "Fill", new Color(1f, 0.8f, 0.55f, 0.9f));

        RectTransform handleArea = MakeStrip(root.transform, "HandleArea", 16f);
        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea, false);
        RectTransform hr = (RectTransform)handle.transform;
        hr.sizeDelta = new Vector2(16f, 0f);
        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = Color.white;
        handleImage.raycastTarget = false;

        Slider slider = root.GetComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = hr;
        slider.targetGraphic = handleImage;
        slider.transition = Selectable.Transition.None;
        slider.navigation = new Navigation { mode = Navigation.Mode.None };
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));

        string title = name.ToUpper();
        label.text = title + "     " + format(slider.value);
        slider.onValueChanged.AddListener(v =>
        {
            label.text = title + "     " + format(v);
            onChange(v);
        });
        return slider;
    }

    // A full-width horizontal strip, centred vertically, with the given height.
    static RectTransform MakeStrip(Transform parent, string name, float height)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(0f, -height / 2f);
        rt.offsetMax = new Vector2(0f, height / 2f);
        return rt;
    }

    // An image that fills its parent strip completely.
    static RectTransform MakeBarImage(Transform parent, string name, Color color)
    {
        GameObject g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image image = g.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }
}
