using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Put this on the player's camera. Look at an interactable and press F.
public class PlayerInteractor : MonoBehaviour
{
    public float reach = 3.5f;
    [Tooltip("Aim assist: the interaction ray is this thick (metres), so small or thin objects are easy to hit.")]
    public float aimRadius = 0.3f;
    public LayerMask layers = 1 << 8;

    Interactable current;
    GUIStyle promptStyle, cueStyle;
    bool momSees, lunaNear;
    float nextCueCheck;

    void Update()
    {
        current = null;
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.SphereCast(ray, aimRadius, out RaycastHit hit, reach, layers, QueryTriggerInteraction.Ignore))
            current = hit.collider.GetComponentInParent<Interactable>();

        if (RestSpot.AnyoneResting) current = null;                                // sitting / swinging: F means "get up"
        if (current != null && PressedThisFrame())
            current.TryInteract();

        // Cues under the prompt: will Mom SEE this? Is Luna close enough to feel it? (checked a few times a second)
        if (current == null) { momSees = lunaNear = false; }
        else if (Time.time >= nextCueCheck)
        {
            nextCueCheck = Time.time + 0.2f;
            momSees = MomLife.MomCanSee(current.transform);
            BabyAI baby = FindAnyObjectByType<BabyAI>();
            lunaNear = baby != null && !baby.asleep && baby.isActiveAndEnabled && Vector3.Distance(baby.transform.position, current.transform.position) <= 8f;
        }
    }

    bool PressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) || MobileControls.InteractPressed;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }

    void OnGUI()
    {
        if (CutsceneRunner.IsPlaying || PauseMenu.IsOpen) return;              // no crosshair or prompt over cutscenes and menus
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(20, Mathf.RoundToInt(Screen.height * 0.026f)) };
            promptStyle.normal.textColor = Color.white;
        }
        float cx = Screen.width / 2f, cy = Screen.height / 2f;
        GUI.Label(new Rect(cx - 20, cy - 15, 40, 30), current != null ? "( + )" : "+", promptStyle);
        if (current != null && !current.IsBusy && !FriendPromptShowing() && current.prompt.Length > 0)
        {
            GUI.Label(new Rect(cx - 250, cy + 30, 500, 30), current.prompt + "  " + MobileControls.Label("F"), promptStyle);
            if (cueStyle == null) cueStyle = new GUIStyle(promptStyle) { fontSize = 15 };
            if (momSees)
            {
                cueStyle.normal.textColor = new Color(1f, 0.72f, 0.42f);
                GUI.Label(new Rect(cx - 250, cy + 56, 500, 24), "Mom can see this", cueStyle);
            }
            if (lunaNear)
            {
                cueStyle.normal.textColor = new Color(0.65f, 0.8f, 1f);
                GUI.Label(new Rect(cx - 250, cy + (momSees ? 78 : 56), 500, 24), "Luna is close enough to feel it", cueStyle);
            }
        }
    }

    // Chapter 0: Granny's puzzle shows its own "[F] Interact" box; then this prompt stays away so they do not overlap.
    static bool FriendPromptShowing()
    {
        return UINotifier.Instance != null && UINotifier.Instance.interactUI != null && UINotifier.Instance.interactUI.activeInHierarchy;
    }
}
