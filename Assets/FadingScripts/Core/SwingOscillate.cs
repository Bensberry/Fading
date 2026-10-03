using System.Collections;
using UnityEngine;

// Swings a part back and forth for 5 seconds, then lets it settle.
// Used by: INT_Grandma_Clock (the pendulum swings while the chime plays).
public class SwingOscillate : Interactable
{
    public string partSuffix = "_Pendulum";
    public Vector3 localAxis = Vector3.forward;
    public float amplitude = 12f;
    public float swingsPerSecond = 0.6f;
    public float rampTime = 0.6f;
    public float settleTime = 1.5f;

    Transform part;
    Quaternion rest;
    float phase;

    protected override IEnumerator Apply()
    {
        part = FindPart(partSuffix);
        rest = part.localRotation;
        phase = 0f;
        for (float t = 0f; t < rampTime; t += Time.deltaTime) { Swing(amplitude * (t / rampTime)); yield return null; }
    }

    protected override void WhileHeld(float t) { Swing(amplitude); }

    protected override IEnumerator Revert()
    {
        for (float t = 0f; t < settleTime; t += Time.deltaTime) { Swing(amplitude * (1f - t / settleTime)); yield return null; }
        part.localRotation = rest;
    }

    protected override void RestoreInstant() { if (part != null) part.localRotation = rest; }

    void Swing(float amp)
    {
        phase += Time.deltaTime;
        float a = amp * Mathf.Sin(phase * swingsPerSecond * 2f * Mathf.PI);
        part.localRotation = rest * Quaternion.AngleAxis(a, localAxis);
    }
}
