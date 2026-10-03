using System.Collections;
using UnityEngine;

// INT_Grandma_RockingChair: rocks on its curved runners for 5 seconds, then slowly settles.
public class RockingChairInteraction : Interactable
{
    public float maxAngle = 8f;
    public float rocksPerSecond = 0.8f;
    public float rampTime = 0.6f;
    public float settleTime = 2f;
    [Tooltip("Radius of the runners; the chair rolls around this point so it stays on the floor.")]
    public float runnerRadius = 1.0f;

    Vector3 restPos, pivot, axis;
    Quaternion restRot;
    float phase;
    bool captured;

    protected override IEnumerator Apply()
    {
        restPos = transform.position;
        restRot = transform.rotation;
        captured = true;
        pivot = restPos + Vector3.up * runnerRadius;
        axis = restRot * Vector3.right;
        phase = 0f;
        for (float t = 0f; t < rampTime; t += Time.deltaTime) { Rock(maxAngle * t / rampTime); yield return null; }
    }

    protected override void WhileHeld(float t) { Rock(maxAngle); }

    protected override IEnumerator Revert()
    {
        for (float t = 0f; t < settleTime; t += Time.deltaTime) { Rock(maxAngle * (1f - t / settleTime)); yield return null; }
        RestoreInstant();
    }

    protected override void RestoreInstant()
    {
        if (!captured) return;
        transform.position = restPos;
        transform.rotation = restRot;
    }

    void Rock(float amp)
    {
        phase += Time.deltaTime;
        Quaternion q = Quaternion.AngleAxis(amp * Mathf.Sin(phase * rocksPerSecond * 2f * Mathf.PI), axis);
        transform.position = pivot + q * (restPos - pivot);
        transform.rotation = q * restRot;
    }
}
