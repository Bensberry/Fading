using System.Collections;
using UnityEngine;

// Spins a part for 5 seconds, then slows and turns back to where it started.
// Used by: INT_Child_Mobile (the spinner under the hook).
public class SpinSign : Interactable
{
    public string partSuffix = "_Spinner";
    public float degreesPerSecond = 70f;
    public float spinUpTime = 0.8f;
    public float returnTime = 2f;

    Transform part;
    Quaternion rest;

    protected override IEnumerator Apply()
    {
        part = FindPart(partSuffix);
        rest = part.localRotation;
        for (float t = 0f; t < spinUpTime; t += Time.deltaTime)
        {
            part.Rotate(0f, degreesPerSecond * (t / spinUpTime) * Time.deltaTime, 0f, Space.Self);
            yield return null;
        }
    }

    protected override void WhileHeld(float t) { part.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.Self); }

    protected override IEnumerator Revert()
    {
        Quaternion from = part.localRotation;
        for (float t = 0f; t < returnTime; t += Time.deltaTime)
        {
            part.localRotation = Quaternion.Slerp(from, rest, Ease(t / returnTime));
            yield return null;
        }
        part.localRotation = rest;
    }

    protected override void RestoreInstant() { if (part != null) part.localRotation = rest; }
}
