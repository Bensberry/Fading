using System.Collections;
using UnityEngine;

// INT_Child_MusicBox: the lid opens, the key turns and the tune plays for 5 seconds, then it fades and closes.
public class MusicBoxInteraction : Interactable
{
    public AudioClip lullaby;
    [Range(0f, 1f)] public float volume = 0.6f;
    public float lidOpenAngle = 70f;
    public float openTime = 0.8f;
    public float closeTime = 1.2f;
    public float keyTurnsPerSecond = 0.5f;

    Transform lid, key;
    Quaternion lidClosed;
    AudioSource music;

    void Start()
    {
        lid = FindPart("_Lid");
        key = FindPart("_Key");
        lidClosed = lid.localRotation;
        music = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.spatialBlend = 1f;
        music.maxDistance = 12f;
    }

    protected override IEnumerator Apply()
    {
        if (lullaby != null) { music.clip = lullaby; music.volume = volume; music.Play(); }
        yield return TurnLid(lidClosed, lidClosed * Quaternion.Euler(-lidOpenAngle, 0f, 0f), openTime);
    }

    protected override void WhileHeld(float t)
    {
        if (key != transform) key.Rotate(360f * keyTurnsPerSecond * Time.deltaTime, 0f, 0f, Space.Self);
    }

    protected override IEnumerator Revert()
    {
        Quaternion open = lid.localRotation;
        for (float t = 0f; t < closeTime; t += Time.deltaTime)
        {
            lid.localRotation = Quaternion.Slerp(open, lidClosed, Ease(t / closeTime));
            music.volume = volume * (1f - t / closeTime);
            yield return null;
        }
        lid.localRotation = lidClosed;
        music.Stop();
    }

    protected override void RestoreInstant()
    {
        if (lid != null) lid.localRotation = lidClosed;
        if (music != null) music.Stop();
    }

    IEnumerator TurnLid(Quaternion from, Quaternion to, float time)
    {
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            lid.localRotation = Quaternion.Slerp(from, to, Ease(t / time));
            yield return null;
        }
        lid.localRotation = to;
    }
}
