using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// Goes in: nowhere (a static helper used by many scripts).
// Finds your sound files BY NAME, so nothing has to be dragged into slots:
//   put a file in  Assets/Resources/Audio/  and name it exactly what the audio list says, e.g. door_locked.wav
// If a file does not exist the game simply plays no sound there (no error). Names are not case-sensitive for the extension.
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
        cache[name] = clip;
        return clip;
    }

    public static bool Has(string name) { return Get(name) != null; }

    // Play a sound once, at the player's ears (so it is not 3D-positioned). Does nothing if the file is missing.
    public static void Play(string name, float volume = 1f)
    {
        AudioClip clip = Get(name);
        if (clip == null) return;
        Vector3 at = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, at, volume);
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
