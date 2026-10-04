using UnityEngine;

// Goes in: nowhere (FamilyReactions adds it to Mom and to the baby).
// Makes Mom or the baby TURN and LOOK at the thing the ghost just touched, for a few seconds.
// It only turns them while they stand still (so it never fights with walking or crawling).
public class FamilyGaze : MonoBehaviour
{
    public float turnSpeed = 5f;
    public float lookSeconds = 3f;

    Vector3 target;
    float until = -1f;
    Vector3 lastPosition;
    bool moving;

    public void LookAt(Vector3 worldPoint)
    {
        target = worldPoint;
        until = Time.time + lookSeconds;
    }

    void Start() { lastPosition = transform.position; }

    void LateUpdate()
    {
        float step = (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;
        moving = step > 0.15f;

        if (Time.time > until || moving) return;

        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.04f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
    }
}
