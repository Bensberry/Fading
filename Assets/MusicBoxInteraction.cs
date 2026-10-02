using System.Collections;
using UnityEngine;

// Interaction 2 - the child's music box (INT_Child_MusicBox).
// Opens the lid, turns the wind-up key while the tune plays, then closes again.
public class MusicBoxInteraction : Interactable
{
    public float lidOpenAngle = 70f;
    public float openTime = 0.8f;
    [Tooltip("How long it plays if there is no audio clip (or the clip is shorter).")]
    public float playTime = 6f;
    public float keyTurnsPerSecond = 0.5f;
    [Tooltip("Optional: an AudioSource on the music box holding the lullaby clip.")]
    public AudioSource music;

    Transform lid, key;
    Quaternion lidClosed;

    void Reset() { prompt = "Wind the music box"; }

    protected override void Awake()
    {
        base.Awake();
        lid = FindPart("_Lid");      // INT_Child_MusicBox_Lid (hinge at the back)
        key = FindPart("_Key");      // INT_Child_MusicBox_Key (turns around its local X)
        if (lid != null) lidClosed = lid.localRotation;
    }

    Transform FindPart(string suffix)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t != transform && t.name.EndsWith(suffix)) return t;
        return null;
    }

    protected override IEnumerator Run()
    {
        Quaternion lidOpen = lidClosed * Quaternion.Euler(-lidOpenAngle, 0f, 0f);
        yield return RotateLid(lidClosed, lidOpen, openTime);

        if (music != null) music.Play();
        float duration = (music != null && music.clip != null) ? Mathf.Max(playTime, music.clip.length) : playTime;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            if (key != null) key.Rotate(360f * keyTurnsPerSecond * Time.deltaTime, 0f, 0f, Space.Self);
            yield return null;
        }
        if (music != null) music.Stop();

        yield return RotateLid(lidOpen, lidClosed, openTime * 1.5f);
    }

    IEnumerator RotateLid(Quaternion from, Quaternion to, float time)
    {
        if (lid == null) yield break;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            lid.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / time));
            yield return null;
        }
        lid.localRotation = to;
    }
}
