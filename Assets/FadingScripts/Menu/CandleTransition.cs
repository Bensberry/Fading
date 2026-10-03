using System.Collections;
using UnityEngine;

// Goes on: the MainMenu / PauseMenu object (they add it). It builds a small candle out of simple 3D shapes
// (no downloaded assets needed) standing in the dark, lit by its own flame:
//   ShowLit()  = the candle is already burning, with a warm glow on the ground around it (the menu look)
//   Play()     = the "lighting the candle" moment when PLAY is clicked: the flame swells and glows brighter,
//                burns for a moment, then the controller fades the screen out.
//                (If the candle was not lit yet, Play() first reveals it and ignites the wick.)
// Every important setting is a public field.
public class CandleTransition : MonoBehaviour
{
    [Header("Placement (screen position of the candle)")]
    [Tooltip("0,0 = bottom-left of the screen, 1,1 = top-right. The candle sits on the right side.")]
    public Vector2 viewportPosition = new Vector2(0.76f, 0.46f);
    [Tooltip("How far in front of the camera the candle stands.")]
    public float distanceFromCamera = 4f;
    [Tooltip("1 = default size (the candle is about 0.7 m tall).")]
    public float candleScale = 1f;
    public Color waxColor = new Color(0.17f, 0.15f, 0.14f);

    [Header("Timing (seconds)")]
    [Tooltip("Only used if the candle starts unlit.")]
    public float revealSeconds = 4f;
    [Tooltip("Only used if the candle starts unlit.")]
    public float igniteSeconds = 2.5f;
    [Tooltip("How long the flame swells when PLAY is clicked.")]
    public float flareSeconds = 1.5f;
    [Tooltip("How much brighter the light gets when PLAY is clicked.")]
    public float flareMultiplier = 1.8f;
    [Tooltip("How long the lit candle burns before the fade-out starts (about 1-2 seconds).")]
    public float burnSeconds = 1.5f;

    [Header("Light")]
    public Color lightColor = new Color(1f, 0.62f, 0.28f);
    public float lightIntensity = 4f;
    public float lightRange = 7f;
    [Tooltip("The faint cold light that shows an unlit candle in the dark.")]
    public Color revealColor = new Color(0.55f, 0.65f, 0.85f);
    public float revealIntensity = 0.5f;

    Light flameLight, revealLight;
    Transform flame;
    Renderer flameRenderer;
    GameObject root;
    bool flickering;
    float flameStrength;           // 0 = unlit, 1 = fully lit
    float boost = 1f;              // 1 = normal, flareMultiplier = flared

    // Creates the candle (switched off until ShowLit / Play is called).
    public void Build(Camera cam)
    {
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.fieldOfView = 40f;

        root = new GameObject("MenuCandle");
        Vector3 centre = cam.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, distanceFromCamera));
        root.transform.position = centre;
        root.transform.localScale = Vector3.one * candleScale;

        const float height = 0.7f;
        MakePart("Wax", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.28f, height / 2f, 0.28f), waxColor, 0.15f);
        MakePart("Wick", PrimitiveType.Cylinder, new Vector3(0f, height / 2f + 0.02f, 0f), new Vector3(0.012f, 0.02f, 0.012f), new Color(0.05f, 0.04f, 0.04f), 0f);
        MakePart("Ground", PrimitiveType.Cylinder, new Vector3(0f, -height / 2f - 0.01f, 0f), new Vector3(40f, 0.01f, 40f), new Color(0.12f, 0.1f, 0.09f), 0.1f);

        MakeFlame(new Vector3(0f, height / 2f + 0.09f, 0f));
        MakeLights(new Vector3(0f, height / 2f + 0.1f, 0f));
        SetLook(0f, 0f);
        root.SetActive(false);
    }

    GameObject MakePart(string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Color color, float smoothness)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(root.transform, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = localScale;
        Material m = g.GetComponent<Renderer>().material;
        m.color = color;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        return g;
    }

    void MakeFlame(Vector3 localPos)
    {
        GameObject f = MakePart("Flame", PrimitiveType.Sphere, localPos, new Vector3(0.05f, 0.11f, 0.05f), lightColor, 0f);
        f.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        flame = f.transform;
        flameRenderer = f.GetComponent<Renderer>();
        flameRenderer.material.EnableKeyword("_EMISSION");
    }

    void MakeLights(Vector3 flameLocalPos)
    {
        GameObject fl = new GameObject("FlameLight");
        fl.transform.SetParent(root.transform, false);
        fl.transform.localPosition = flameLocalPos;
        flameLight = fl.AddComponent<Light>();
        flameLight.type = LightType.Point;
        flameLight.color = lightColor;
        flameLight.range = lightRange;
        flameLight.shadows = LightShadows.Soft;

        GameObject rv = new GameObject("RevealLight");
        rv.transform.SetParent(root.transform, false);
        rv.transform.localPosition = new Vector3(-0.8f, 0.9f, -1.2f);
        revealLight = rv.AddComponent<Light>();
        revealLight.type = LightType.Point;
        revealLight.color = revealColor;
        revealLight.range = 6f;
        revealLight.shadows = LightShadows.None;
    }

    // ---------- states
    // The candle is already burning (what the menu looks like).
    public void ShowLit()
    {
        root.SetActive(true);
        flameStrength = 1f;
        flickering = true;
    }

    // The sequence started by PLAY (the controller runs this).
    public IEnumerator Play()
    {
        root.SetActive(true);
        if (flameStrength < 1f) yield return LightFromDark();
        yield return Flare();
        yield return new WaitForSeconds(burnSeconds);
    }

    IEnumerator LightFromDark()
    {
        SetLook(0f, 0f);
        for (float t = 0f; t < revealSeconds; t += Time.deltaTime)
        {
            SetLook(0f, Mathf.SmoothStep(0f, 1f, t / revealSeconds));
            yield return null;
        }
        SetLook(0f, 1f);

        flickering = true;
        for (float t = 0f; t < igniteSeconds; t += Time.deltaTime)
        {
            flameStrength = Mathf.SmoothStep(0f, 1f, t / igniteSeconds);
            yield return null;
        }
        flameStrength = 1f;
    }

    // The flame swells and the glow spreads a little further.
    IEnumerator Flare()
    {
        for (float t = 0f; t < flareSeconds; t += Time.deltaTime)
        {
            boost = Mathf.Lerp(1f, flareMultiplier, Mathf.SmoothStep(0f, 1f, t / flareSeconds));
            yield return null;
        }
        boost = flareMultiplier;
    }

    void Update()
    {
        if (!flickering || flameLight == null) return;
        float time = Time.unscaledTime;                    // unscaled: the flame also flickers while the game is paused
        float n = Mathf.PerlinNoise(time * 5f, 0.3f) * 0.6f + Mathf.PerlinNoise(time * 17f, 4.1f) * 0.4f;
        float f = 0.85f + 0.3f * n;
        SetLook(flameStrength * f * boost, 1f - flameStrength * 0.6f);
    }

    // flame01: strength of the flame and its light (1 = normal).  reveal01: strength of the faint cold light.
    void SetLook(float flame01, float reveal01)
    {
        flameLight.intensity = lightIntensity * flame01;
        flameLight.range = lightRange * Mathf.Lerp(1f, 1.3f, Mathf.InverseLerp(1f, flareMultiplier, boost));
        flameLight.enabled = flame01 > 0.001f;
        revealLight.intensity = revealIntensity * reveal01;

        flame.gameObject.SetActive(flame01 > 0.001f);
        float size = Mathf.Clamp(flame01, 0.01f, 1.4f);
        flame.localScale = new Vector3(0.05f, 0.11f, 0.05f) * size;
        flameRenderer.material.SetColor("_EmissionColor", lightColor * 3f * flame01);
    }

    void OnDestroy() { if (root != null) Destroy(root); }
}
