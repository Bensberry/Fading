using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in every chapter).
// The night sky: a dome of twinkling STARS and a glowing MOON, placed where the moonlight comes from.
// Both fade in when night falls and fade out at dawn. They follow the player (so they always look infinitely far away),
// and they are made in code (no files needed).
public class NightSky : MonoBehaviour
{
    const float Distance = 400f;

    DayNightCycle cycle;
    ParticleSystem stars;
    ParticleSystemRenderer starRenderer;
    Transform moon, halo;
    Material moonMat, haloMat, starMat;
    float shown;

    void Start()
    {
        cycle = FindAnyObjectByType<DayNightCycle>();
        Shader shader = Shader.Find("Sprites/Default");                 // always included in builds, never fogged
        if (cycle == null || shader == null) { enabled = false; return; }

        Texture2D dot = MakeDot(64);
        starMat = new Material(shader) { mainTexture = dot };
        moonMat = new Material(shader) { mainTexture = dot };
        haloMat = new Material(shader) { mainTexture = MakeGlow(128) };
        MakeStars();
        moon = MakeQuad("Moon", moonMat, 26f);
        halo = MakeQuad("MoonGlow", haloMat, 110f);
        shown = cycle.IsNight ? 1f : 0f;
        Apply();
    }

    void MakeStars()
    {
        GameObject g = new GameObject("Stars");
        g.transform.SetParent(transform, false);
        stars = g.AddComponent<ParticleSystem>();
        stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = stars.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 100000f;
        main.startSpeed = 0f;
        main.maxParticles = Application.isMobilePlatform ? 700 : 1600;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.85f, 1f), new Color(1f, 0.95f, 0.85f));
        ParticleSystem.EmissionModule emission = stars.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = stars.shape;
        shape.enabled = false;

        starRenderer = g.GetComponent<ParticleSystemRenderer>();
        starRenderer.material = starMat;
        starRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        starRenderer.receiveShadows = false;

        // Scatter them over the upper half of the sky.
        int count = main.maxParticles;
        ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 0.95f + 0.05f;
            p.position = dir.normalized * Distance;
            p.startSize = Random.value < 0.06f ? Random.Range(2.5f, 3.6f) : Random.Range(0.6f, 2f);
            stars.Emit(p, 1);
        }
        stars.Pause();                                                  // they never move: no need to simulate 700 particles every frame
    }

    Transform MakeQuad(string objectName, Material m, float size)
    {
        GameObject q = FadingMaterials.Primitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        q.name = objectName;
        q.transform.SetParent(transform, false);
        q.transform.localScale = Vector3.one * size;
        Renderer r = q.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return q.transform;
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        transform.position = cam.transform.position;                    // the sky moves with the player
        shown = Mathf.MoveTowards(shown, cycle.IsNight ? 1f : 0f, Time.deltaTime / 6f);

        // The moon sits where the moonlight comes from, and always faces the player.
        Vector3 toMoon = cycle.sun != null ? -cycle.sun.transform.forward : Vector3.up;
        moon.position = halo.position = transform.position + toMoon * (Distance * 0.9f);
        moon.rotation = halo.rotation = Quaternion.LookRotation(moon.position - cam.transform.position);
        stars.transform.Rotate(0f, 0.15f * Time.deltaTime, 0f);         // the sky turns very slowly
        Apply();
    }

    void Apply()
    {
        float twinkle = 0.85f + 0.15f * Mathf.Sin(Time.time * 1.3f);
        starMat.color = new Color(1f, 1f, 1f, shown * twinkle);
        moonMat.color = new Color(0.95f, 0.97f, 1f, shown);
        haloMat.color = new Color(0.6f, 0.7f, 1f, shown * 0.35f);
        bool visible = shown > 0.01f;
        if (starRenderer.enabled != visible) { starRenderer.enabled = visible; moon.gameObject.SetActive(visible); halo.gameObject.SetActive(visible); }
    }

    static Texture2D MakeDot(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 6f)));
            }
        t.Apply();
        return t;
    }

    static Texture2D MakeGlow(int size)
    {
        Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) / 2f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
        t.Apply();
        return t;
    }
}
