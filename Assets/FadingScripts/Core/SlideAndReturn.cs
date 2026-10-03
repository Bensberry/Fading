using System.Collections;
using UnityEngine;

// Slides (and optionally hops) a short distance, then drifts back after 5 seconds.
// Used by: INT_Mom_CoffeeMug (slides toward Mom), INT_Child_StuffedToy (hops toward the child).
public class SlideAndReturn : Interactable
{
    [Tooltip("Optional: slide toward this object.")]
    public Transform slideToward;
    [Tooltip("World direction used when Slide Toward is empty.")]
    public Vector3 slideDirection = Vector3.left;
    public float distance = 0.18f;
    public float hopHeight = 0f;
    public float slideTime = 0.6f;
    public float returnTime = 1.5f;

    Vector3 start, end;

    protected override IEnumerator Apply()
    {
        start = transform.position;
        Vector3 dir = slideToward != null ? slideToward.position - start : slideDirection;
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.left;
        end = start + dir * distance;
        yield return Move(start, end, slideTime, hopHeight);
    }

    protected override IEnumerator Revert() { yield return Move(end, start, returnTime, 0f); }

    protected override void RestoreInstant() { transform.position = start; }

    IEnumerator Move(Vector3 from, Vector3 to, float time, float hop)
    {
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            float k = t / time;
            transform.position = Vector3.Lerp(from, to, Ease(k)) + Vector3.up * hop * Mathf.Sin(k * Mathf.PI);
            yield return null;
        }
        transform.position = to;
    }
}
