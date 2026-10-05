using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Goes in: nowhere (Playground adds it to the bench, the swing set and the slide in the yard).
// Lets the ghost REST somewhere for a while:
//   Sit     sit down on a bench and look around (F or Space to get up)
//   Swing   sit on the swing and swing slowly (F or Space to get up)
//   Slide   climb on and slide down (you get off by yourself at the bottom)
// This is not a "sign" (nobody notices it, it does not count for the bars and it never frightens anyone),
// so it does not follow the 5-second rule. You can still look around with the mouse while resting.
public class RestSpot : Interactable
{
    public enum Kind { Sit, Swing, Slide }

    public Kind kind = Kind.Sit;
    public Vector3 seatPoint;            // world: where the ghost sits (the seat surface), or the TOP of the slide
    public Vector3 endPoint;             // world: the bottom of the slide (Slide only)
    public Vector3 facing = Vector3.forward;
    public Vector3 exitPoint;            // world: where the ghost stands up again (on the ground)
    public SwingSway swing;              // Swing: the swing that really moves with the ghost (optional)
    public bool restsTheNight;           // the bench in front of the house: at night, sitting here ends the night (StarsCutscene)

    const float EyesAboveSeat = 0.75f;

    FirstPersonController player;
    CharacterController body;
    Transform cameraRoot;
    bool resting;
    int enteredFrame;

    public static bool AnyoneResting { get; private set; }

    public override void ApplyDefaults()
    {
        prompt = kind == Kind.Swing ? "Sit on the swing" : kind == Kind.Slide ? "Go down the slide" : "Sit on the bench";
    }

    public override void TryInteract()
    {
        if (resting || AnyoneResting) return;
        player = FindAnyObjectByType<FirstPersonController>();
        if (player == null) return;
        body = player.GetComponent<CharacterController>();
        cameraRoot = player.playerCameraRoot != null ? player.playerCameraRoot : (Camera.main != null ? Camera.main.transform : player.transform);
        StartCoroutine(Rest());
    }

    IEnumerator Rest()
    {
        resting = AnyoneResting = true;
        enteredFrame = Time.frameCount;
        player.movementLocked = true;
        if (body != null) body.enabled = false;
        string oldPrompt = prompt;
        prompt = "";
        FadingHud.Toast(kind == Kind.Slide ? "Wheee..." : (MobileControls.Active ? "TOUCH to get up" : "[F] or [Space] to get up"), 2f);
        float eyeHeight = cameraRoot.position.y - player.transform.position.y;

        // Turn to face the right way, move onto the seat.
        Vector3 look = facing; look.y = 0f;
        if (look.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(look);
        yield return MoveEyesTo(seatPoint + Vector3.up * EyesAboveSeat, eyeHeight, 0.6f);

        if (restsTheNight && NightQuest.Running && !NightQuest.Complete && !NightQuest.Carrying)
        {
            yield return new WaitForSeconds(0.8f);                          // a breath, then he looks up at the stars...
            CutsceneRunner.Play(new StarsCutscene(seatPoint, facing), NightQuest.FinishNight);
            yield return null;
            while (CutsceneRunner.IsPlaying) yield return null;
        }
        else if (kind == Kind.Slide) yield return SlideDown(eyeHeight);
        else
        {
            float t = 0f;
            while (!GetUpPressed())
            {
                t += Time.deltaTime;
                if (kind == Kind.Swing && swing != null)
                {
                    swing.held = true;
                    float swingAngle = Mathf.Sin(t * 1.7f) * 24f * Mathf.Min(1f, t / 2f);
                    SetEyes(swing.SwingTo(swingAngle) + Vector3.up * EyesAboveSeat, eyeHeight);
                }
                else if (kind == Kind.Swing)
                {
                    float angle = Mathf.Sin(t * 1.7f) * 24f * Mathf.Min(1f, t / 2f);
                    Vector3 pivot = seatPoint + Vector3.up * 1.9f;
                    Vector3 side = Vector3.Cross(Vector3.up, look.normalized);
                    Vector3 seat = pivot + Quaternion.AngleAxis(angle, side) * (Vector3.down * 1.9f);
                    SetEyes(seat + Vector3.up * EyesAboveSeat, eyeHeight);
                }
                yield return null;
            }
        }

        if (swing != null) swing.held = false;

        // Stand up again next to it.
        yield return MoveEyesTo(exitPoint + Vector3.up * (eyeHeight + 0.05f), eyeHeight, 0.4f);
        if (body != null) body.enabled = true;
        player.movementLocked = false;
        prompt = oldPrompt;
        resting = AnyoneResting = false;
    }

    IEnumerator SlideDown(float eyeHeight)
    {
        yield return new WaitForSeconds(0.3f);
        Vector3 from = seatPoint + Vector3.up * EyesAboveSeat, to = endPoint + Vector3.up * EyesAboveSeat;
        for (float t = 0f; t < 1.6f; t += Time.deltaTime)
        {
            float k = t / 1.6f;
            SetEyes(Vector3.Lerp(from, to, k * k), eyeHeight);              // slow at the top, fast at the bottom
            yield return null;
        }
        SetEyes(to, eyeHeight);
        yield return new WaitForSeconds(0.4f);
    }

    IEnumerator MoveEyesTo(Vector3 eyes, float eyeHeight, float seconds)
    {
        Vector3 start = player.transform.position, end = eyes - Vector3.up * eyeHeight;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            player.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        player.transform.position = end;
    }

    void SetEyes(Vector3 eyes, float eyeHeight) { player.transform.position = eyes - Vector3.up * eyeHeight; }

    void Update()
    {
        if (!restsTheNight || resting) return;
        prompt = NightQuest.Running && !NightQuest.Complete && !NightQuest.Carrying ? "Rest under the stars  (until morning)" : "Sit on the bench";
    }

    bool GetUpPressed()
    {
        if (Time.frameCount <= enteredFrame + 1 || CutsceneRunner.IsPlaying) return false;
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)) || MobileControls.InteractPressed;
#else
        return Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Space);
#endif
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (!resting) return;
        if (body != null) body.enabled = true;
        if (player != null) player.movementLocked = false;
        resting = AnyoneResting = false;
    }

    // Not used (resting is not a sign), but required by Interactable.
    protected override IEnumerator Apply() { yield break; }
    protected override IEnumerator Revert() { yield break; }
    protected override void RestoreInstant() { }
}
