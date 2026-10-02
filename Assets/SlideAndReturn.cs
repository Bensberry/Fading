using System.Collections;
using UnityEngine;

// Interaction 1 - Mom's coffee mug (INT_Mom_CoffeeMug).
// Slides a little toward Mom, waits, then drifts back to where it was ("everything is temporary").
public class SlideAndReturn : Interactable
{
    [Tooltip("Optional: slide toward this object (e.g. Chair_Kitchen_1_Mom).")]
    public Transform slideToward;
    [Tooltip("Used when Slide Toward is empty. World direction; Mom's chair is to the west (-X).")]
    public Vector3 slideDirection = Vector3.left;
    public float distance = 0.18f;
    public float slideTime = 0.6f;
    [Tooltip("Seconds the object stays moved before drifting back.")]
    public float holdTime = 5f;
    public float returnTime = 1.5f;
    public AudioClip slideSound;

    void Reset() { prompt = "Push the mug"; }

    protected override IEnumerator Run()
    {
        Vector3 start = transform.position;
        Vector3 dir = slideToward != null ? slideToward.position - start : slideDirection;
        dir.y = 0f;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.left;
        Vector3 end = start + dir * distance;

        if (slideSound != null) AudioSource.PlayClipAtPoint(slideSound, start, 0.6f);
        yield return Move(start, end, slideTime);
        yield return new WaitForSeconds(holdTime);
        yield return Move(end, start, returnTime);
    }

    IEnumerator Move(Vector3 from, Vector3 to, float time)
    {
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / time));
            yield return null;
        }
        transform.position = to;
    }
}
