using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (BabyLife and the ending cutscene add it to the baby's model).
// The baby's face: the model has face shapes (Mouth_Smile, Mouth_Frown, Eye_Blink ...), this moves them.
//   Smile(true/false)      a slow, happy smile
//   Cry(seconds)           a crying face (and a little head shake)
//   LookAt(target)         she turns her head to look at something (a camera, the player); LookAt(null) stops
// She also blinks by herself every few seconds.
public class BabyFace : MonoBehaviour
{
    public float maxLookAngle = 70f;
    public bool eyesClosed;                    // asleep

    struct Shape { public SkinnedMeshRenderer renderer; public int index; }

    readonly Dictionary<string, List<Shape>> shapes = new Dictionary<string, List<Shape>>();
    readonly Dictionary<string, float> wanted = new Dictionary<string, float>();
    Animator animator;
    Transform head, neck, eyeL, eyeR, lookTarget;
    float smile, smileTarget, cry, cryUntil, look, blinkAt, blink;

    public Vector3 HeadPosition { get { return head != null ? head.position : transform.position; } }

    public static BabyFace On(GameObject baby)
    {
        Animator a = baby.GetComponentInChildren<Animator>();
        GameObject host = a != null ? a.gameObject : baby;
        BabyFace f = host.GetComponent<BabyFace>();
        if (f == null) f = host.AddComponent<BabyFace>();
        return f;
    }

    public void Smile(bool on) { smileTarget = on ? 1f : 0f; }
    public void Cry(float seconds) { cryUntil = Time.time + seconds; smileTarget = 0f; }
    public void LookAt(Transform target) { lookTarget = target; }

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            head = animator.GetBoneTransform(HumanBodyBones.Head);
            neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            eyeL = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            eyeR = animator.GetBoneTransform(HumanBodyBones.RightEye);
        }
        if (head == null) head = FindBone("CC_Base_Head");
        if (eyeL == null) eyeL = FindBone("CC_Base_L_Eye");
        if (eyeR == null) eyeR = FindBone("CC_Base_R_Eye");

        foreach (SkinnedMeshRenderer r in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Mesh mesh = r.sharedMesh;
            if (mesh == null) continue;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string n = mesh.GetBlendShapeName(i);
                int dot = n.LastIndexOf('.');
                if (dot >= 0) n = n.Substring(dot + 1);                  // "Body.Mouth_Smile" -> "Mouth_Smile"
                List<Shape> list;
                if (!shapes.TryGetValue(n, out list)) shapes[n] = list = new List<Shape>();
                list.Add(new Shape { renderer = r, index = i });
            }
        }
        blinkAt = Time.time + Random.Range(1.5f, 4f);
    }

    Transform FindBone(string boneName)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == boneName) return t;
        return null;
    }

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        smile = Mathf.MoveTowards(smile, smileTarget, dt * 1.2f);
        cry = Mathf.MoveTowards(cry, Time.time < cryUntil ? 1f : 0f, dt * 2f);
        look = Mathf.MoveTowards(look, lookTarget != null ? 1f : 0f, dt * 1.2f);
        UpdateBlink();

        wanted.Clear();
        Want("Mouth_Smile", 100f * smile);
        Want("Cheek_Raise_L", 60f * smile); Want("Cheek_Raise_R", 60f * smile);
        Want("Eye_Squint_L", Mathf.Max(25f * smile, 60f * cry)); Want("Eye_Squint_R", Mathf.Max(25f * smile, 60f * cry));
        Want("Mouth_Frown", 100f * cry);
        Want("Mouth_Open", 45f * cry);
        Want("Brow_Raise_Inner_L", 100f * cry); Want("Brow_Raise_Inner_R", 100f * cry);
        Want("Eye_Blink", eyesClosed ? 100f : 100f * blink);
        foreach (KeyValuePair<string, float> w in wanted) SetShape(w.Key, w.Value);

        TurnHead();
    }

    void UpdateBlink()
    {
        if (Time.time >= blinkAt)
        {
            blink = 1f;
            blinkAt = Time.time + Random.Range(2.5f, 5f);
        }
        else blink = Mathf.MoveTowards(blink, 0f, Time.deltaTime * 8f);
    }

    void Want(string shape, float value) { wanted[shape] = value; }

    void SetShape(string shape, float value)
    {
        List<Shape> list;
        if (!shapes.TryGetValue(shape, out list)) return;
        foreach (Shape s in list) if (s.renderer != null) s.renderer.SetBlendShapeWeight(s.index, value);
    }

    // Turn the head toward the target (after the animation has posed her).
    void TurnHead()
    {
        if (head == null) return;
        if (cry > 0.01f) head.rotation = Quaternion.AngleAxis(Mathf.Sin(Time.time * 11f) * 6f * cry, head.up) * head.rotation;
        if (look <= 0.001f || lookTarget == null) return;

        Vector3 facing = FaceDirection();
        Vector3 toTarget = (lookTarget.position - head.position).normalized;
        Quaternion turn = Quaternion.FromToRotation(facing, toTarget);
        float angle;
        Vector3 axis;
        turn.ToAngleAxis(out angle, out axis);
        if (angle > 180f) angle -= 360f;
        angle = Mathf.Clamp(angle, -maxLookAngle, maxLookAngle) * Mathf.SmoothStep(0f, 1f, look);
        head.rotation = Quaternion.AngleAxis(angle, axis) * head.rotation;
    }

    // Which way her face points: across the line between her eyes, away from the neck.
    Vector3 FaceDirection()
    {
        if (eyeL == null || eyeR == null) return transform.forward;
        Vector3 across = eyeR.position - eyeL.position;
        Vector3 up = neck != null ? head.position - neck.position : head.up;
        Vector3 forward = Vector3.Cross(across, up).normalized;
        Vector3 eyesMiddle = (eyeL.position + eyeR.position) * 0.5f;
        if (Vector3.Dot(forward, eyesMiddle - head.position) < 0f) forward = -forward;
        return forward;
    }
}
