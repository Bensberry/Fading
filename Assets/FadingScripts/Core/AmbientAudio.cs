using UnityEngine;

// Goes in: nowhere by hand. ChapterRules adds it in every chapter.
// Gives the house a voice without needing any sound files (everything is generated in code):
//   - a low, quiet room ambience that loops all the time (a soft hum and a breath of air)
//   - footsteps while the player walks (one soft step for every 0.8 m)
//   - a gentle chime when the ghost touches something that has no sound of its own
// All volumes are fields at the top. Hearing loss (AbilityLoss) automatically makes all of it quieter and duller.
// When you have real sound files, give them to the objects (Interactable.sound etc.) and these simply stay in the background.
public class AmbientAudio : MonoBehaviour
{
    public float ambienceVolume = 0.22f;
    public float footstepVolume = 0.35f;
    public float chimeVolume = 0.25f;
    public float metresPerStep = 0.8f;

    const int SampleRate = 22050;

    AudioSource ambience, effects;
    AudioClip footstep, chime;
    Transform player;
    Vector3 lastPosition;
    float travelled;

    void Start()
    {
        ambience = MakeSource();
        ambience.clip = MakeAmbience();
        ambience.loop = true;
        ambience.volume = ambienceVolume;
        ambience.Play();

        effects = MakeSource();
        footstep = MakeFootstep();
        chime = MakeChime();

        FirstPersonController p = FindFirstObjectByType<FirstPersonController>();
        if (p != null) { player = p.transform; lastPosition = player.position; }

        foreach (Interactable i in FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Interactable touched = i;
            i.onInteract.AddListener(() => PlayChime(touched));
        }
    }

    AudioSource MakeSource()
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        return s;
    }

    // ---------- playing
    void Update()
    {
        if (player == null || Time.deltaTime <= 0f) return;

        Vector3 delta = player.position - lastPosition;
        lastPosition = player.position;
        delta.y = 0f;
        if (delta.magnitude / Time.deltaTime < 0.4f || delta.magnitude > 1f) return;     // standing still (or teleported)

        travelled += delta.magnitude;
        if (travelled < metresPerStep) return;
        travelled = 0f;

        effects.pitch = Random.Range(0.85f, 1.1f);
        effects.PlayOneShot(footstep, footstepVolume);
    }

    void PlayChime(Interactable touched)
    {
        if (touched.sound != null) return;                // it has its own sound
        effects.pitch = 1f;
        effects.PlayOneShot(chime, chimeVolume);
    }

    // ---------- the generated sounds
    static AudioClip MakeAmbience()
    {
        const float seconds = 8f;
        int n = (int)(SampleRate * seconds);
        float[] data = new float[n];
        System.Random random = new System.Random(7);
        float low = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            low += ((float)random.NextDouble() * 2f - 1f - low) * 0.015f;                  // soft, rumbling noise
            float hum = 0.5f * Mathf.Sin(2f * Mathf.PI * 55f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 82.5f * t);   // whole cycles in 8 s: loops cleanly
            float swell = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * t / seconds);
            float edge = Mathf.Clamp01(Mathf.Min(i, n - i) / (0.15f * SampleRate));       // soft edges so the loop never clicks
            data[i] = (low * 2.2f + hum * 0.35f) * swell * edge * 0.5f;
        }
        return MakeClip("RoomAmbience", data);
    }

    static AudioClip MakeFootstep()
    {
        int n = (int)(SampleRate * 0.16f);
        float[] data = new float[n];
        System.Random random = new System.Random(3);
        float low = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            low += ((float)random.NextDouble() * 2f - 1f - low) * 0.12f;
            float thump = Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 38f);
            data[i] = (low * 0.8f * Mathf.Exp(-t * 28f) + thump * 0.9f) * 0.8f;
        }
        return MakeClip("Footstep", data);
    }

    static AudioClip MakeChime()
    {
        int n = (int)(SampleRate * 1.0f);
        float[] data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float envelope = Mathf.Min(1f, t / 0.01f) * Mathf.Exp(-t * 4f);                 // quick soft attack, slow fade
            data[i] = (Mathf.Sin(2f * Mathf.PI * 523.25f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 784f * t)) * 0.3f * envelope;
        }
        return MakeClip("TouchChime", data);
    }

    static AudioClip MakeClip(string name, float[] data)
    {
        AudioClip clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
