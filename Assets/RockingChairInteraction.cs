using System.Collections;
using UnityEngine;

// Interaction 3 - Grandma's rocking chair (INT_Grandma_RockingChair).
// Rocks back and forth on its runners and slowly settles.
public class RockingChairInteraction : Interactable
{
    public float maxAngle = 8f;
    public float rocksPerSecond = 0.8f;
    public float duration = 5f;
    [Tooltip("Radius of the curved runners; the chair rolls around this point so it stays on the floor.")]
    public float runnerRadius = 1.0f;
    public AudioClip creak;

    Vector3 restPos;
    Quaternion restRot;

    void Reset() { prompt = "Rock the chair"; }

    protected override IEnumerator Run()
    {
        restPos = transform.position;
        restRot = transform.rotation;
        Vector3 pivot = restPos + Vector3.up * runnerRadius;
        Vector3 axis = restRot * Vector3.right;

        if (creak != null) AudioSource.PlayClipAtPoint(creak, restPos, 0.7f);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float fade = 1f - t / duration;
            float angle = maxAngle * fade * Mathf.Sin(t * rocksPerSecond * 2f * Mathf.PI);
            Quaternion q = Quaternion.AngleAxis(angle, axis);
            transform.position = pivot + q * (restPos - pivot);
            transform.rotation = q * restRot;
            yield return null;
        }
        transform.position = restPos;
        transform.rotation = restRot;
    }
}
