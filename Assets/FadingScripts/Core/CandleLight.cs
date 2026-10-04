using UnityEngine;

// Goes in: nowhere by hand. ChapterRules adds it (together with CandleHint) to the player's held candle.
// The candle's flame light with a bit of "physics":
//   - the flame flickers (smooth noise) and sometimes a gust makes it dip
//   - when you walk, run or turn the flame leans backwards on a little spring and flickers harder
//   - Brightness is set by CandleHint: 1 = normal, 0.4 = ember, 0 = burned out, >1 = flaring
// It adds a small flame ball and a point light, so the night is lit by your candle.
public class CandleLight : MonoBehaviour
{
    public Color dimColor = new Color(1f, 0.55f, 0.22f);
    public Color brightColor = new Color(1f, 0.78f, 0.45f);
    public float baseIntensity = 2.5f;
    public float range = 8f;
    [Tooltip("How far the flame leans when you move.")]
    public float leanAmount = 0.035f;

    // Set this from other scripts. The flame fades to this value smoothly.
    public float Brightness = 1f;
    public Transform FlameTransform { get { return flame; } }

    // Set by the pause menu's LIGHTING slider (1 = normal).
    public static float GlobalScale = 1f;

    Transform flame;
    Light glow;
    Renderer flameRenderer;
    Vector3 localTop;
    Vector3 lastPos, smoothVelocity, lean, leanVelocity;
    float shown = 1f, gust;
    float seed;

    void Start()
    {
        seed = Random.value * 100f;
        localTop = FindLocalTop();
        CreateFlame();
        lastPos = transform.position;
    }

    // The top of the candle mesh, in the candle's own space (so it follows the candle).
    Vector3 FindLocalTop()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return Vector3.zero;
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return transform.InverseTransformPoint(new Vector3(b.center.x, b.max.y + 0.02f, b.center.z));
    }

    void CreateFlame()
    {
        GameObject f = FadingMaterials.Primitive(PrimitiveType.Sphere);
        f.name = "CandleFlame";
        Destroy(f.GetComponent<Collider>());
        flame = f.transform;                                  // not a child, so we can move it freely
        flameRenderer = f.GetComponent<Renderer>();
        flameRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        flameRenderer.material.EnableKeyword("_EMISSION");

        glow = f.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = range;
        glow.shadows = LightShadows.Hard;               // the held candle is the one light that keeps shadows (hard = cheaper), so walls still block its light
    }

    void OnEnable() { if (flame != null) flame.gameObject.SetActive(true); }
    void OnDisable() { if (flame != null) flame.gameObject.SetActive(false); }   // candle hidden (cutscene)
    void OnDestroy() { if (flame != null) Destroy(flame.gameObject); }

    void LateUpdate()
    {
        if (flame == null) return;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        Vector3 velocity = (transform.position - lastPos) / dt;
        lastPos = transform.position;
        smoothVelocity = Vector3.Lerp(smoothVelocity, velocity, 10f * dt);
        float speed = Mathf.Min(smoothVelocity.magnitude, 8f);

        UpdateSpring(dt);
        float flicker = Flicker(dt, speed);
        shown = Mathf.MoveTowards(shown, Brightness, dt * 1.5f);

        flame.position = transform.TransformPoint(localTop) + lean + Wobble();
        ApplyLook(flicker);
    }

    // A simple spring: the flame is pulled toward "leaning opposite to our movement" and settles with damping.
    void UpdateSpring(float dt)
    {
        Vector3 target = Vector3.ClampMagnitude(-smoothVelocity * leanAmount, 0.12f);
        Vector3 accel = 120f * (target - lean) - 14f * leanVelocity;
        leanVelocity += accel * dt;
        lean += leanVelocity * dt;
    }

    // Smooth noise flicker. Moving makes it flicker harder and can blow a gust that dips the light.
    float Flicker(float dt, float speed)
    {
        float t = Time.time + seed;
        float slow = Mathf.PerlinNoise(t * 3f, 0.5f);
        float fast = Mathf.PerlinNoise(t * 19f, 7.3f);
        float strength = 0.18f + speed * 0.03f;
        float f = 1f - strength * 0.5f + strength * (0.7f * slow + 0.3f * fast);

        gust = Mathf.MoveTowards(gust, 0f, dt * 1.2f);
        if (speed > 1.5f && Random.value < dt * speed * 0.25f) gust = Random.Range(0.15f, 0.4f);
        return Mathf.Max(0.05f, f - gust);
    }

    Vector3 Wobble()
    {
        float t = Time.time * 5f + seed;
        return new Vector3(Mathf.PerlinNoise(t, 1f) - 0.5f, 0f, Mathf.PerlinNoise(1f, t) - 0.5f) * 0.015f;
    }

    void ApplyLook(float flicker)
    {
        bool visible = shown > 0.01f;
        if (flame.gameObject.activeSelf != visible) flame.gameObject.SetActive(visible);
        if (!visible) return;

        glow.intensity = baseIntensity * GlobalScale * shown * flicker;
        glow.color = Color.Lerp(dimColor, brightColor, Mathf.Clamp01(shown * flicker));

        float size = 0.022f * (0.7f + 0.5f * shown * flicker);
        flame.localScale = new Vector3(size, size * 1.7f, size);
        flameRenderer.material.color = glow.color;
        flameRenderer.material.SetColor("_EmissionColor", glow.color * 3f * shown);
    }
}
