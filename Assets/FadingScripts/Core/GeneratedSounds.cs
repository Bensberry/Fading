using UnityEngine;

// Goes in: nowhere (GameAudio uses it). Simple stand-in sounds made in code, used ONLY while the real file is missing
// from Assets/Resources/Audio/ (a real file with the same name always wins):
//   scare_sting     a low hit with a cold shimmer (the family got frightened)
//   hint_whoosh     an airy whoosh (the candle hint)
//   candle_out      a soft breath (a candle goes out)
//   candle_relight  a small bright flare (a candle lights)
//   ability_lost    a slow, sinking tone (a sense fades)
//   notice_chime    two soft bell notes (Mom or Luna noticed you: points on the bar)
//   memory_collect  three rising bell notes (a memory light gathered)
//   dream_swell     a warm chord (the dream is delivered)
public static class GeneratedSounds
{
    const int Rate = 22050;

    public static AudioClip Make(string name)
    {
        switch (name)
        {
            case "scare_sting": return Clip(name, 1.6f, ScareSting);
            case "hint_whoosh": return Clip(name, 1.0f, (t, r) => Noise(r) * Envelope(t, 0.35f, 1.0f) * 0.35f);
            case "candle_out": return Clip(name, 0.8f, (t, r) => Noise(r) * Envelope(t, 0.05f, 0.8f) * 0.25f);
            case "candle_relight": return Clip(name, 0.7f, CandleRelight);
            case "ability_lost": return Clip(name, 2.5f, AbilityLost);
            case "notice_chime": return Clip(name, 1.2f, (t, r) => Bell(t, 659f) + Bell(t - 0.12f, 988f));
            case "memory_collect": return Clip(name, 1.4f, (t, r) => Bell(t, 784f) + Bell(t - 0.1f, 1175f) * 0.8f + Bell(t - 0.2f, 1568f) * 0.6f);
            case "dream_swell": return Clip(name, 3.5f, DreamSwell);
            default: return null;
        }
    }

    static float ScareSting(float t, System.Random r)
    {
        float hit = Mathf.Sin(2f * Mathf.PI * 55f * t) * Mathf.Exp(-t * 3f);
        float shimmer = (Mathf.Sin(2f * Mathf.PI * 1244f * t) + Mathf.Sin(2f * Mathf.PI * 1318f * t)) * 0.12f * Envelope(t, 0.3f, 1.6f);
        return (hit * 0.6f + shimmer + Noise(r) * 0.08f * Mathf.Exp(-t * 6f)) * 0.7f;
    }

    static float CandleRelight(float t, System.Random r)
    {
        float crackle = Noise(r) * Mathf.Exp(-t * 9f) * 0.4f;
        float warm = Mathf.Sin(2f * Mathf.PI * 180f * t) * Envelope(t, 0.08f, 0.7f) * 0.15f;
        return crackle + warm;
    }

    static float AbilityLost(float t, System.Random r)
    {
        float pitch = Mathf.Lerp(220f, 110f, t / 2.5f);
        return Mathf.Sin(2f * Mathf.PI * pitch * t) * Envelope(t, 0.4f, 2.5f) * 0.3f;
    }

    // A soft bell note that starts at t = 0.
    static float Bell(float t, float pitch)
    {
        if (t < 0f) return 0f;
        return (Mathf.Sin(2f * Mathf.PI * pitch * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * pitch * t)) * Mathf.Exp(-t * 3.5f) * Mathf.Min(1f, t / 0.005f) * 0.18f;
    }

    static float DreamSwell(float t, System.Random r)
    {
        float env = Envelope(t, 1.6f, 3.5f);
        return (Mathf.Sin(2f * Mathf.PI * 262f * t) + Mathf.Sin(2f * Mathf.PI * 330f * t) + Mathf.Sin(2f * Mathf.PI * 392f * t)) * 0.08f * env;
    }

    // Rises to 1 at 'peak' seconds, then falls back to 0 at 'length'.
    static float Envelope(float t, float peak, float length)
    {
        if (t < peak) return t / peak;
        return Mathf.Clamp01(1f - (t - peak) / Mathf.Max(0.01f, length - peak));
    }

    static float Noise(System.Random r) { return (float)r.NextDouble() * 2f - 1f; }

    static AudioClip Clip(string name, float seconds, System.Func<float, System.Random, float> wave)
    {
        int n = (int)(Rate * seconds);
        float[] data = new float[n];
        System.Random random = new System.Random(name.GetHashCode());
        for (int i = 0; i < n; i++) data[i] = wave(i / (float)Rate, random);
        AudioClip clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
