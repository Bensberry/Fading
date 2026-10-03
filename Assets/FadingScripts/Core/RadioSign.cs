using System.Collections;
using UnityEngine;

// INT_Mom_Radio: crackles on and plays the song for 5 seconds (the radio trembles), then fades out.
public class RadioSign : Interactable
{
    public AudioClip song;
    [Tooltip("Seconds into the song to start from (e.g. the chorus).")]
    public float songStart = 0f;
    [Range(0f, 1f)] public float volume = 0.7f;
    public float fadeIn = 0.6f;
    public float fadeOut = 1.5f;
    public float tremble = 0.0015f;

    AudioSource speaker;
    Vector3 restPos;
    bool captured;

    void Start()
    {
        speaker = gameObject.AddComponent<AudioSource>();
        speaker.playOnAwake = false;
        speaker.spatialBlend = 1f;
        speaker.maxDistance = 15f;
    }

    protected override IEnumerator Apply()
    {
        restPos = transform.localPosition;
        captured = true;
        if (song != null)
        {
            speaker.clip = song;
            speaker.time = Mathf.Clamp(songStart, 0f, Mathf.Max(0f, song.length - 0.1f));
            speaker.volume = 0f;
            speaker.Play();
        }
        for (float t = 0f; t < fadeIn; t += Time.deltaTime) { speaker.volume = volume * t / fadeIn; Shake(); yield return null; }
        speaker.volume = volume;
    }

    protected override void WhileHeld(float t) { Shake(); }

    protected override IEnumerator Revert()
    {
        for (float t = 0f; t < fadeOut; t += Time.deltaTime) { speaker.volume = volume * (1f - t / fadeOut); yield return null; }
        RestoreInstant();
    }

    protected override void RestoreInstant()
    {
        if (speaker != null) speaker.Stop();
        if (captured) transform.localPosition = restPos;
    }

    void Shake() { transform.localPosition = restPos + Random.insideUnitSphere * tremble; }
}
