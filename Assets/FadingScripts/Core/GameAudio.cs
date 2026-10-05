using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Goes in: nowhere (a static helper used by many scripts).
// Finds your sound files BY NAME, so nothing has to be dragged into slots:
//   put a file in  Assets/Resources/Audio/  and name it exactly what the audio list says, e.g. door_locked.wav
// If a file does not exist the game simply plays no sound there (no error), or a stand-in from GeneratedSounds. Names are not case-sensitive for the extension.
public static class GameAudio
{
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetCache() { cache.Clear(); }

    // The clip with this name, or null if there is no such file.
    public static AudioClip Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        AudioClip clip;
        if (cache.TryGetValue(name, out clip)) return clip;
        clip = Resources.Load<AudioClip>("Audio/" + name);
        if (clip == null) clip = GeneratedSounds.Make(name);          // a few sounds have a stand-in made in code
        cache[name] = clip;
        return clip;
    }

    public static bool Has(string name) { return Get(name) != null; }

    // Play a sound once, at the player's ears (so it is not 3D-positioned). Does nothing if the file is missing.
    public static void Play(string name, float volume = 1f)
    {
        AudioClip clip = Get(name);
        if (clip == null) return;
        FlatSource().PlayOneShot(clip, volume);                 // "in your head": same volume wherever the camera is (also in cutscenes)
    }

    static AudioSource flat;

    static AudioSource FlatSource()
    {
        if (flat == null)
        {
            GameObject g = new GameObject("GameAudio2D");
            Object.DontDestroyOnLoad(g);
            flat = g.AddComponent<AudioSource>();
            flat.spatialBlend = 0f;
            flat.playOnAwake = false;
        }
        return flat;
    }

    // How long a sound file is (0 if it does not exist).
    public static float Length(string name)
    {
        AudioClip clip = Get(name);
        return clip != null ? clip.length : 0f;
    }

    // The voice file for a line someone says in the game (FamilyLife.Say): "Did you see that?" -> voice_line_did_you_see_that
    public static string VoiceFor(string line)
    {
        System.Text.StringBuilder b = new System.Text.StringBuilder();
        bool gap = false;
        foreach (char ch in line.ToLowerInvariant())
        {
            bool ok = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9');
            if (ok) { if (gap && b.Length > 0) b.Append('_'); b.Append(ch); gap = false; }
            else gap = true;
        }
        string s = b.ToString();
        if (s.Length > 40) s = s.Substring(0, 40);
        return "voice_line_" + s.TrimEnd('_');
    }

    // A spoken line from a place in the house: you hear the direction, and it stays clear up to ~20 m.
    public static void PlayVoiceAt(string name, Vector3 position, float volume = 1f)
    {
        AudioClip clip = Get(name);
        if (clip == null) return;
        GameObject g = new GameObject("Voice_" + name);
        g.transform.position = position;
        AudioSource s = g.AddComponent<AudioSource>();
        s.clip = clip;
        s.volume = volume;
        s.spatialBlend = 0.7f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = 3f;
        s.maxDistance = 22f;
        s.Play();
        Object.Destroy(g, clip.length + 0.2f);
    }

    // Play a sound once at a place in the house (3D: louder when the player is close). Does nothing if the file is missing.
    public static void PlayAt(string name, Vector3 position, float volume = 1f)
    {
        AudioClip clip = Get(name);
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    // For a component with public AudioClip slots that were left empty in the scene: every empty slot is filled from
    // Assets/Resources/Audio/<slot name>. (FadingInteractablesSetup uses this for candleWhoosh, doorCreak, lullaby ...)
    public static void FillEmptySlots(object target)
    {
        foreach (FieldInfo f in target.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (f.FieldType != typeof(AudioClip)) continue;
            AudioClip current = (AudioClip)f.GetValue(target);
            if (current != null) continue;
            AudioClip found = Get(f.Name);
            if (found != null) f.SetValue(target, found);
        }
    }
}
