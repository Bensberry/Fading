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
        FindPivotAndAxis();
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

    // The pivot and rocking axis come from the chair's real size, not from its object origin
    // (imported chairs are often rotated/scaled, so "origin" and "right" can point anywhere).
    // Pivot: just above the floor under the middle of the chair. Axis: across the chair's narrow side.
    void FindPivotAndAxis()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            pivot = restPos + Vector3.up * runnerRadius;
            axis = restRot * Vector3.right;
            return;
        }
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

        pivot = new Vector3(b.center.x, b.min.y + runnerRadius, b.center.z);
        axis = b.size.x < b.size.z ? Vector3.right : Vector3.forward;
    }

    void Rock(float amp)
    {
        phase += Time.deltaTime;
        Quaternion q = Quaternion.AngleAxis(amp * Mathf.Sin(phase * rocksPerSecond * 2f * Mathf.PI), axis);
        transform.position = pivot + q * (restPos - pivot);
        transform.rotation = q * restRot;
    }
}
