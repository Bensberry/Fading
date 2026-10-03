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

    public bool IsOpen { get; private set; }

    Quaternion closedRot;
    bool captured;
    Coroutine moving;

    void Start() { Capture(); prompt = openPrompt; }

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
        IsOpen = !IsOpen;
        prompt = IsOpen ? closePrompt : openPrompt;
        onInteract.Invoke();
        if (sound != null) AudioSource.PlayClipAtPoint(sound, transform.position, soundVolume);
        if (moving != null) StopCoroutine(moving);
        Quaternion target = IsOpen ? closedRot * Quaternion.AngleAxis(angle, localAxis) : closedRot;
        moving = StartCoroutine(Turn(target, IsOpen ? openTime : closeTime));
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
        prompt = openPrompt;
    }

    // Not used by doors, but required by Interactable.
    protected override IEnumerator Apply() { yield break; }
    protected override IEnumerator Revert() { yield break; }
    protected override void RestoreInstant() { CloseInstant(); }
}
