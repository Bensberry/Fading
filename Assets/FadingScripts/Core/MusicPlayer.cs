using System.Collections;
using UnityEngine;

// Goes in: nowhere by hand (MusicPlayer.Create() makes it; it survives scene changes so the music can flow from one scene to the next).
// Plays one looping music track at a time and crossfades when the track changes:
//   MusicPlayer.Create().Play("music_night");     // a file in Assets/Resources/Audio/ with that name
// A missing file just fades the music out. Tracks used: music_menu, music_night, music_day, music_ending.
public class MusicPlayer : MonoBehaviour
{
    static MusicPlayer instance;

    AudioSource[] sources;
    int active;
    string current = "";
    Coroutine fading;

    public static MusicPlayer Create()
    {
        if (instance != null) return instance;
        GameObject g = new GameObject("MusicPlayer");
        DontDestroyOnLoad(g);
        instance = g.AddComponent<MusicPlayer>();
        return instance;
    }

    void Awake()
    {
        sources = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            sources[i] = gameObject.AddComponent<AudioSource>();
            sources[i].loop = true;
            sources[i].playOnAwake = false;
            sources[i].spatialBlend = 0f;
            sources[i].volume = 0f;
        }
    }

    // Start (or keep) a track. Does nothing if that track is already playing.
    public void Play(string track, float fadeSeconds = 3f, float volume = 0.5f)
    {
        if (track == current) return;
        current = track;
        if (fading != null) StopCoroutine(fading);
        fading = StartCoroutine(Crossfade(GameAudio.Get(track), fadeSeconds, volume));
    }

    public void Stop(float fadeSeconds = 2f) { Play("", fadeSeconds); }

    IEnumerator Crossfade(AudioClip next, float seconds, float volume)
    {
        AudioSource from = sources[active];
        active = 1 - active;
        AudioSource to = sources[active];

        if (next != null)
        {
            to.clip = next;
            to.volume = 0f;
            to.Play();
        }

        float fromStart = from.volume;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            float k = t / seconds;
            from.volume = Mathf.Lerp(fromStart, 0f, k);
            if (next != null) to.volume = Mathf.Lerp(0f, volume, k);
            yield return null;
        }
        from.volume = 0f;
        from.Stop();
        if (next != null) to.volume = volume;
        fading = null;
    }
}
