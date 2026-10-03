using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// Goes on: a TextMeshPro UI text (MainMenuController adds it to PLAY and QUIT).
// Makes plain text behave like a button, without any button box:
//   - hover: the text gets a little brighter and slightly bigger (very subtle, no flashy animation)
//   - click: fires onClick
// Everything is a public field, so the hover effect can be tweaked in the Inspector.
[RequireComponent(typeof(TMP_Text))]
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Text Hover Effect")]
    [Range(0f, 1f)] public float normalAlpha = 0.7f;
    [Range(0f, 1f)] public float hoverAlpha = 1f;
    public float hoverScale = 1.05f;
    [Tooltip("How fast the text eases to its hover look. Lower = slower and calmer.")]
    public float easeSpeed = 6f;

    public UnityEvent onClick = new UnityEvent();

    // The controller uses these to fade the text in and out and to stop clicks.
    public float Visibility { get; set; } = 1f;
    public bool Interactable { get; set; } = true;

    TMP_Text label;
    Color baseColor = Color.white;
    float hover;     // 0 = normal, 1 = fully hovered

    void Awake()
    {
        label = GetComponent<TMP_Text>();
        baseColor = label.color;
        baseColor.a = 1f;
    }

    void Update()
    {
        float target = (hovered && Interactable) ? 1f : 0f;
        hover = Mathf.MoveTowards(hover, target, easeSpeed * Time.unscaledDeltaTime);

        float alpha = Mathf.Lerp(normalAlpha, hoverAlpha, Mathf.SmoothStep(0f, 1f, hover)) * Visibility;
        label.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, hoverScale, Mathf.SmoothStep(0f, 1f, hover));
    }

    bool hovered;
    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Interactable) onClick.Invoke();
    }
}
