using System.Collections;
using UnityEngine;

// Rotates a part around one local axis, holds for 5 seconds, then swings back.
// Used by: doors (open/slam), photo album cover, photo frames (tip forward), calendar (swings crooked).
public class HingeSwing : Interactable
{
    [Tooltip("Child to rotate, found by the end of its name (e.g. \"_Cover\"). Empty = this object.")]
    public string partSuffix = "";
    public Vector3 localAxis = Vector3.up;
    public float angle = -90f;
    public float openTime = 0.8f;
    public float closeTime = 0.6f;

    Transform part;
    Quaternion rest;

    protected override IEnumerator Apply()
    {
        part = FindPart(partSuffix);
        rest = part.localRotation;
        yield return Turn(rest, rest * Quaternion.AngleAxis(angle, localAxis), openTime);
    }

    protected override IEnumerator Revert() { yield return Turn(part.localRotation, rest, closeTime); }

    protected override void RestoreInstant() { if (part != null) part.localRotation = rest; }

    IEnumerator Turn(Quaternion from, Quaternion to, float time)
    {
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            part.localRotation = Quaternion.Slerp(from, to, Ease(t / time));
            yield return null;
        }
        part.localRotation = to;
    }
}
