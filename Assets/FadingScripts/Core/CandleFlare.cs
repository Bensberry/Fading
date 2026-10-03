using System.Collections;
using UnityEngine;

// INT_Hallway_MemorialCandle: creates a small flame + light on the wick.
// Touch: the flame flares up and flickers for 5 seconds, then calms back down.
// (If startsLit is off, the touch lights it for 5 seconds and it goes out again.)
public class CandleFlare : Interactable
{
    public bool startsLit = true;
    public float wickHeight = 0.19f;
    public Color flameColor = new Color(1f, 0.7f, 0.35f);
    public float normalIntensity = 0.6f;
    public float flareIntensity = 1.8f;
    public float range = 3f;
    public float flareTime = 0.4f;
    public float calmTime = 1.2f;

    Light glow;
    Transform flame;
    Vector3 flameSize = new Vector3(0.016f, 0.035f, 0.016f);
    float normal;

    void Start()
    {
        GameObject f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        f.name = "Flame";
        Destroy(f.GetComponent<Collider>());
        flame = f.transform;
        flame.SetParent(transform, false);
        flame.localPosition = new Vector3(0f, wickHeight, 0f);
        Renderer r = f.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Material m = r.material;
        m.color = flameColor;
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", flameColor * 3f);

        glow = f.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = flameColor;
        glow.range = range;
        glow.shadows = LightShadows.Soft;

        normal = startsLit ? normalIntensity : 0f;
        Set(normal, 1f);
    }

    void Set(float intensity, float size)
    {
        glow.intensity = intensity;
        bool on = intensity > 0.001f;
        glow.enabled = on;
        flame.gameObject.SetActive(on);
        flame.localScale = flameSize * size;
    }

    protected override IEnumerator Apply()
    {
        float from = glow.enabled ? glow.intensity : 0f;
        for (float t = 0f; t < flareTime; t += Time.deltaTime)
        {
            float k = Ease(t / flareTime);
            Set(Mathf.Lerp(from, flareIntensity, k), Mathf.Lerp(1f, 1.6f, k));
            yield return null;
        }
    }

    protected override void WhileHeld(float t)
    {
        float n = Mathf.PerlinNoise(t * 8f, 0.3f);
        Set(flareIntensity * (0.8f + 0.4f * n), 1.4f + 0.4f * n);
    }

    protected override IEnumerator Revert()
    {
        float from = glow.intensity;
        for (float t = 0f; t < calmTime; t += Time.deltaTime)
        {
            float k = Ease(t / calmTime);
            Set(Mathf.Lerp(from, normal, k), Mathf.Lerp(1.5f, 1f, k));
            yield return null;
        }
        Set(normal, 1f);
    }

    protected override void RestoreInstant() { if (glow != null) Set(normal, 1f); }
}
