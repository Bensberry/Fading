using System.Collections;
using UnityEngine;

// Goes on: the MainMenu object (MainMenuController adds it). It builds a small candle out of simple 3D shapes
// (no downloaded assets needed) and plays the "lighting the candle" moment when PLAY is clicked:
//   1. the candle is unlit; it is slowly revealed in the dark by a very faint cold light
//   2. the wick ignites: a flame grows and a warm glow slowly spreads over the surrounding darkness
//   3. the flame burns for a moment, then the controller fades the screen out
// Every important setting is a public field.
public class CandleTransition : MonoBehaviour
{
    [Header("Placement (screen position of the candle)")]
    [Tooltip("0,0 = bottom-left of the screen, 1,1 = top-right. The candle sits on the right side.")]
    public Vector2 viewportPosition = new Vector2(0.74f, 0.5f);
    [Tooltip("How far in front of the camera the candle stands.")]
    public float distanceFromCamera = 4f;
    [Tooltip("1 = default size (the candle is about 0.7 m tall).")]
    public float candleScale = 1f;

    [Header("Timing (seconds)")]
    public float revealSeconds = 4f;
    public float igniteSeconds = 2.5f;
    [Tooltip("How long the lit candle burns before the fade-out starts (about 1-2 seconds).")]
    public float burnSeconds = 1.5f;

    [Header("Light")]
    public Color lightColor = new Color(1f, 0.62f, 0.28f);
    public float lightIntensity = 4f;
    public float lightRange = 7f;
    [Tooltip("The faint cold light that shows the unlit candle in the dark.")]
    public Color revealColor = new Color(0.55f, 0.65f, 0.85f);
    public float revealIntensity = 0.5f;

    Light flameLight, revealLight;
    Transform flame;
    Renderer flameRenderer;
    GameObject root;
    bool flickering;
    float flameStrength;           // 0 = unlit, 1 = fully lit

    // Creates the candle (switched off until Play is called).
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
        MakePart("Wax", PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.28f, height / 2f, 0.28f), new Color(0.82f, 0.77f, 0.66f), 0.15f);
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

    // ---------- the sequence (the controller runs this)
    public IEnumerator Play()
    {
        root.SetActive(true);
        SetLook(0f, 0f);

        // 1. slowly reveal the unlit candle
        for (float t = 0f; t < revealSeconds; t += Time.deltaTime)
        {
            SetLook(0f, Mathf.SmoothStep(0f, 1f, t / revealSeconds));
            yield return null;
        }
        SetLook(0f, 1f);

        // 2. ignite the wick: flame grows, warm glow spreads
        flickering = true;
        for (float t = 0f; t < igniteSeconds; t += Time.deltaTime)
        {
            flameStrength = Mathf.SmoothStep(0f, 1f, t / igniteSeconds);
            yield return null;
        }
        flameStrength = 1f;

        // 3. burn for a moment
        yield return new WaitForSeconds(burnSeconds);
    }

    void Update()
    {
        if (!flickering || flameLight == null) return;
        float n = Mathf.PerlinNoise(Time.time * 5f, 0.3f) * 0.6f + Mathf.PerlinNoise(Time.time * 17f, 4.1f) * 0.4f;
        float f = 0.85f + 0.3f * n;
        SetLook(flameStrength * f, 1f - flameStrength * 0.6f);
    }

    // flame: 0..1+ strength of the flame and its light.  reveal: 0..1 strength of the faint cold light.
    void SetLook(float flame01, float reveal01)
    {
        flameLight.intensity = lightIntensity * flame01;
        flameLight.enabled = flame01 > 0.001f;
        revealLight.intensity = revealIntensity * reveal01;

        flame.gameObject.SetActive(flame01 > 0.001f);
        float size = Mathf.Clamp01(flame01);
        flame.localScale = new Vector3(0.05f, 0.11f, 0.05f) * Mathf.Max(0.01f, size);
        flameRenderer.material.SetColor("_EmissionColor", lightColor * 3f * flame01);
    }

    void OnDestroy() { if (root != null) Destroy(root); }
}
