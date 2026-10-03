using System.Collections;
using UnityEngine;

// Doors: the one exception to the 5-second rule.
// Pressing F opens the door; it stays open until you press F on it again, then it shuts almost instantly.
public class DoorToggle : Interactable
{
    public Vector3 localAxis = Vector3.up;
    public float angle = -90f;            // negative = swings into the room
    public float openTime = 0.9f;
    public float closeTime = 0.15f;       // near-instant close (a slam)
    public string openPrompt = "Open the door";
    public string closePrompt = "Close the door";

    [Header("Locking")]
    [Tooltip("A locked door will not open: it rattles and shows a message instead.")]
    public bool locked;
    public string lockedMessage = "It won't open.";
    public string lockedPrompt = "Locked door";

    public bool IsOpen { get; private set; }

    Quaternion closedRot;
    bool captured;
    Coroutine moving, rattling;

    void Start() { Capture(); prompt = ClosedPrompt(); }

    string ClosedPrompt() { return locked ? lockedPrompt : openPrompt; }

    // Lock or unlock from code (ChapterRules does this per chapter).
    public void SetLocked(bool value, string message = null)
    {
        locked = value;
        if (message != null) lockedMessage = message;
        prompt = IsOpen ? closePrompt : ClosedPrompt();
    }

    void Capture()
    {
        if (captured) return;
        closedRot = transform.localRotation;
        captured = true;
    }

    public override void TryInteract()
    {
        if (!isActiveAndEnabled) return;
        Capture();
        if (locked && !IsOpen) { Rattle(); return; }
        IsOpen = !IsOpen;
        prompt = IsOpen ? closePrompt : openPrompt;
        onInteract.Invoke();
        if (sound != null) AudioSource.PlayClipAtPoint(sound, transform.position, soundVolume);
        if (moving != null) StopCoroutine(moving);
        Quaternion target = IsOpen ? closedRot * Quaternion.AngleAxis(angle, localAxis) : closedRot;
        moving = StartCoroutine(Turn(target, IsOpen ? openTime : closeTime));
    }

    // Locked door: the handle rattles (small quick shake that fades out) and a message pops up.
    void Rattle()
    {
        FadingHud.Toast(lockedMessage);
        if (rattling == null) rattling = StartCoroutine(RattleRoutine());
    }

    IEnumerator RattleRoutine()
    {
        const float length = 0.5f;
        for (float t = 0f; t < length; t += Time.deltaTime)
        {
            float fade = 1f - t / length;
            float shake = Mathf.Sin(t * 70f) * 3.5f * fade;
            transform.localRotation = closedRot * Quaternion.AngleAxis(shake, localAxis);
            yield return null;
        }
        transform.localRotation = closedRot;
        rattling = null;
    }

    IEnumerator Turn(Quaternion to, float time)
    {
        Quaternion from = transform.localRotation;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            transform.localRotation = Quaternion.Slerp(from, to, Ease(t / time));
            yield return null;
        }
        transform.localRotation = to;
        if (!IsOpen) onReturned.Invoke();
        moving = null;
    }

    // Close instantly from code (e.g. when a new night starts).
    public void CloseInstant()
    {
        if (moving != null) StopCoroutine(moving);
        moving = null;
        if (captured) transform.localRotation = closedRot;
        IsOpen = false;
        prompt = ClosedPrompt();
    }

    // Swing the door open instantly from code (e.g. Grandma's door is already open in Chapter 1).
    public void OpenInstant()
    {
        Capture();
        if (moving != null) StopCoroutine(moving);
        moving = null;
        IsOpen = true;
        prompt = closePrompt;
        transform.localRotation = closedRot * Quaternion.AngleAxis(angle, localAxis);
    }

    // Not used by doors, but required by Interactable.
    protected override IEnumerator Apply() { yield break; }
    protected override IEnumerator Revert() { yield break; }
    protected override void RestoreInstant() { CloseInstant(); }
}
