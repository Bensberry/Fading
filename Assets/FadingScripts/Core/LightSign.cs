using System.Collections;
using UnityEngine;

// Lamps. Adds a Point Light to the "_Bulb" child automatically.
// Flicker: flickers, then glows steady for 5 seconds before returning to normal (Grandma's lamp).
// TurnOn:  switches on for 5 seconds, then fades off again (the child's nightlight).
public class LightSign : Interactable
{
    public enum Mode { Flicker, TurnOn }
    public Mode mode = Mode.Flicker;
    public bool startsOn = true;
    public string bulbSuffix = "_Bulb";
    public Color color = new Color(1f, 0.78f, 0.5f);
    public float intensity = 1.2f;
    public float range = 4f;
    public float flickerTime = 1.2f;
    public float fadeTime = 1f;

    Light lamp;
    Renderer bulbRenderer;
    float normal;

    void Start()
    {
        Transform bulb = FindPart(bulbSuffix);
        lamp = bulb.GetComponentInChildren<Light>();
        if (lamp == null)
        {
            lamp = bulb.gameObject.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = color;
            lamp.range = range;
            lamp.shadows = LightShadows.Soft;
        }
        if (bulb != transform) bulbRenderer = bulb.GetComponent<Renderer>();
        normal = startsOn ? intensity : 0f;
        Set(normal);
    }

    void Set(float value)
    {
        lamp.intensity = value;
        lamp.enabled = value > 0.001f;
        if (bulbRenderer != null) bulbRenderer.enabled = value > 0.001f;
    }

    protected override IEnumerator Apply()
    {
        if (mode == Mode.Flicker)
        {
            for (float t = 0f; t < flickerTime; t += 0.07f)
            {
                Set(Random.value > 0.5f ? intensity * 1.5f : 0f);
                yield return new WaitForSeconds(0.07f);
            }
        }
        yield return Fade(lamp.enabled ? lamp.intensity : 0f, intensity * (mode == Mode.Flicker ? 1.4f : 1f), 0.3f);
    }

    protected override void WhileHeld(float t)
    {
        if (mode == Mode.Flicker) Set(intensity * (1.3f + 0.15f * Mathf.PerlinNoise(t * 6f, 0f)));
    }

    protected override IEnumerator Revert() { yield return Fade(lamp.intensity, normal, fadeTime); }

    protected override void RestoreInstant() { if (lamp != null) Set(normal); }

    IEnumerator Fade(float from, float to, float time)
    {
        for (float t = 0f; t < time; t += Time.deltaTime) { Set(Mathf.Lerp(from, to, t / time)); yield return null; }
        Set(to);
    }
}
