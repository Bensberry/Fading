using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Goes in: nowhere (MomLife and the cutscenes add it to a character).
// Plays ONE extra animation clip (sitting, crying, sleeping ...) on a character, blending in and out smoothly,
// then gives the character back to its normal Animator Controller (idle / walk). No changes to the controller files needed.
// It can also make a character sob without any clip: 'sob' (0 to 1) bows the head and makes the shoulders shake.
public class PosePlayer : MonoBehaviour
{
    [Range(0f, 1f)] public float sob;           // 0 = calm, 1 = crying hard

    Animator animator;
    PlayableGraph graph;
    AnimationMixerPlayable mixer;
    float weight, targetWeight, fadeSpeed = 2f;
    float sobShown;

    public bool IsPlaying { get { return graph.IsValid() && targetWeight > 0f; } }

    public static PosePlayer On(GameObject character)
    {
        PosePlayer p = character.GetComponent<PosePlayer>();
        if (p == null) p = character.AddComponent<PosePlayer>();
        return p;
    }

    Animator FindAnimator()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        return animator;
    }

    // Returns false when there is no clip or no animator (then nothing happens).
    public bool Play(AnimationClip clip, float fadeSeconds = 0.5f)
    {
        if (clip == null || FindAnimator() == null || animator.runtimeAnimatorController == null) return false;
        DestroyGraph();

        graph = PlayableGraph.Create(name + "_Pose");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Pose", animator);
        mixer = AnimationMixerPlayable.Create(graph, 2);
        AnimatorControllerPlayable normal = AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController);
        AnimationClipPlayable extra = AnimationClipPlayable.Create(graph, clip);
        graph.Connect(normal, 0, mixer, 0);
        graph.Connect(extra, 0, mixer, 1);
        output.SetSourcePlayable(mixer);

        weight = 0f;
        targetWeight = 1f;
        fadeSpeed = 1f / Mathf.Max(0.05f, fadeSeconds);
        ApplyWeight();
        graph.Play();
        return true;
    }

    public void Stop(float fadeSeconds = 0.5f)
    {
        targetWeight = 0f;
        fadeSpeed = 1f / Mathf.Max(0.05f, fadeSeconds);
    }

    void Update()
    {
        if (!graph.IsValid()) return;
        weight = Mathf.MoveTowards(weight, targetWeight, fadeSpeed * Time.deltaTime);
        ApplyWeight();
        if (targetWeight <= 0f && weight <= 0f) DestroyGraph();
    }

    void ApplyWeight()
    {
        mixer.SetInputWeight(0, 1f - weight);
        mixer.SetInputWeight(1, weight);
    }

    // Sobbing: after the animation has posed the body, bow the head and chest and shake the shoulders a little.
    void LateUpdate()
    {
        sobShown = Mathf.MoveTowards(sobShown, sob, Time.deltaTime * 1.5f);
        if (sobShown <= 0.001f || FindAnimator() == null || !animator.isHuman) return;

        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null) chest = animator.GetBoneTransform(HumanBodyBones.Spine);
        Vector3 right = transform.right;
        float shake = Mathf.Sin(Time.time * 9f) * Mathf.Max(0f, Mathf.Sin(Time.time * 1.7f));
        if (chest != null) chest.rotation = Quaternion.AngleAxis((10f + shake * 3f) * sobShown, right) * chest.rotation;
        if (head != null) head.rotation = Quaternion.AngleAxis((22f + shake * 2f) * sobShown, right) * head.rotation;
    }

    void DestroyGraph()
    {
        if (graph.IsValid()) graph.Destroy();
    }

    void OnDisable() { DestroyGraph(); weight = targetWeight = 0f; }
    void OnDestroy() { DestroyGraph(); }
}
