using UnityEngine;

// Goes in: nowhere (Playground adds it to each swing of the back-yard swing set).
// Makes an empty swing sway a little by itself (wind... or someone). While the ghost rides it, RestSpot sets 'held'
// and moves the swing itself, so the seat really swings with him.
public class SwingSway : MonoBehaviour
{
    [HideInInspector] public bool held;
    float seed;

    void Start() { seed = Random.value * 10f; }

    void Update()
    {
        if (held) return;
        float t = Time.time + seed;
        float angle = Mathf.Sin(t * 1.1f) * (5f + 4f * Mathf.Sin(t * 0.13f));
        transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    // Swing to this angle (degrees, forward / back) and return where the seat is now.
    public Vector3 SwingTo(float angle)
    {
        transform.localRotation = Quaternion.Euler(angle, 0f, 0f);
        return SeatPoint;
    }

    public Vector3 SeatPoint { get { return transform.TransformPoint(0f, -1.62f, 0f); } }
}
