using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Put this on the player's camera. Look at an interactable and press F.
public class PlayerInteractor : MonoBehaviour
{
    public float reach = 3f;
    public LayerMask layers = 1 << 8;

    Interactable current;
    GUIStyle promptStyle;

    void Update()
    {
        current = null;
        Ray ray = new Ray(transform.position, transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, reach, layers, QueryTriggerInteraction.Ignore))
            current = hit.collider.GetComponentInParent<Interactable>();

        if (current != null && PressedThisFrame())
            current.TryInteract();
    }

    bool PressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }

    void OnGUI()
    {
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20 };
            promptStyle.normal.textColor = Color.white;
        }
        float cx = Screen.width / 2f, cy = Screen.height / 2f;
        GUI.Label(new Rect(cx - 20, cy - 15, 40, 30), current != null ? "( + )" : "+", promptStyle);
        if (current != null && !current.IsBusy)
            GUI.Label(new Rect(cx - 250, cy + 30, 500, 30), current.prompt + "  [F]", promptStyle);
    }
}
