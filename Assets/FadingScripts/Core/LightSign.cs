using System.Collections;
using UnityEngine;

// ============================================================
// LightSign
// ============================================================
// Base interaction for lamps / lights.
//
// Flicker:
//     Flickers, then stays on for the interaction duration,
//     then returns to its normal state.
//
// TurnOn:
//     Turns on for the interaction duration,
//     then fades back to its normal state.
//
// IMPORTANT:
// The ClueGoal on this object is the ONLY clue reference.
// ============================================================

public class LightSign : Interactable
{
    public enum Mode
    {
        Flicker,
        TurnOn
    }

    [Header("Light Settings")]
    public Mode mode = Mode.Flicker;

    public bool startsOn = true;

    public string bulbSuffix = "_Bulb";

    public Color color = new Color(1f, 0.78f, 0.5f);

    public float intensity = 1.2f;

    public float range = 4f;

    public float flickerTime = 1.2f;

    public float fadeTime = 1f;

    [Header("Clue")]
    public ClueGoal clueGoal;

    protected Light lamp;
    protected Renderer bulbRenderer;
    protected float normal;

    // --------------------------------------------------------
    // START
    // --------------------------------------------------------

    void Start()
    {
        Transform bulb = FindPart(bulbSuffix);

        if (bulb == null)
        {
            Debug.LogError(
                $"[LIGHT] {name}: Could not find bulb '{bulbSuffix}'"
            );

            return;
        }

        lamp = bulb.GetComponentInChildren<Light>();

        // If there is no Light component, create one.
        if (lamp == null)
        {
            lamp = bulb.gameObject.AddComponent<Light>();

            lamp.type = LightType.Point;
            lamp.color = color;
            lamp.range = range;
            lamp.shadows = LightShadows.None;                // no shadows: much cheaper
        }

        // Apply light settings.
        lamp.color = color;
        lamp.range = range;

        if (bulb != transform)
        {
            bulbRenderer = bulb.GetComponent<Renderer>();
        }

        normal = startsOn ? intensity : 0f;

        Set(normal);

        // Make sure clue starts inactive.
        if (clueGoal != null)
        {
            clueGoal.goalAchieved = false;
            clueGoal.reactionStarted = false;
        }
    }

    // --------------------------------------------------------
    // SET LIGHT
    // --------------------------------------------------------

    protected void Set(float value)
    {
        if (lamp == null)
            return;

        lamp.intensity = value;

        lamp.enabled = value > 0.001f;

        if (bulbRenderer != null)
        {
            bulbRenderer.enabled = value > 0.001f;
        }
    }

    // --------------------------------------------------------
    // APPLY
    // --------------------------------------------------------

    protected override IEnumerator Apply()
    {
        Debug.Log(
            $"[LIGHT] {name}: Interaction started."
        );

        // ====================================================
        // FLICKER MODE
        // ====================================================

        if (mode == Mode.Flicker)
        {
            for (
                float t = 0f;
                t < flickerTime;
                t += 0.07f
            )
            {
                Set(
                    Random.value > 0.5f
                        ? intensity * 1.5f
                        : 0f
                );

                yield return new WaitForSeconds(0.07f);
            }
        }

        // ====================================================
        // TURN LIGHT ON
        // ====================================================

        float currentIntensity =
            lamp != null && lamp.enabled
                ? lamp.intensity
                : 0f;

        float targetIntensity =
            intensity *
            (mode == Mode.Flicker ? 1.4f : 1f);

        yield return Fade(
            currentIntensity,
            targetIntensity,
            0.3f
        );

        Debug.Log(
            $"[LIGHT] {name}: Light is now ON."
        );

        // ====================================================
        // ACTIVATE CLUE
        // ====================================================

        if (clueGoal != null)
        {
            clueGoal.ActivateClue();

            Debug.Log(
                $"[LIGHT CLUE] {name}: goalAchieved = TRUE"
            );
        }
        else
        {
            Debug.LogWarning(
                $"[LIGHT CLUE] {name}: NO ClueGoal assigned!"
            );
        }
    }

    // --------------------------------------------------------
    // WHILE HELD
    // --------------------------------------------------------

    protected override void WhileHeld(float t)
    {
        if (lamp == null)
            return;

        if (mode == Mode.Flicker)
        {
            Set(
                intensity *
                (
                    1.3f +
                    0.15f *
                    Mathf.PerlinNoise(t * 6f, 0f)
                )
            );
        }
    }

    // --------------------------------------------------------
    // REVERT
    // --------------------------------------------------------

    protected override IEnumerator Revert()
    {
        Debug.Log(
            $"[LIGHT] {name}: Returning to normal."
        );

        if (lamp != null)
        {
            yield return Fade(
                lamp.intensity,
                normal,
                fadeTime
            );
        }

        // ====================================================
        // DEACTIVATE CLUE
        // ====================================================

        if (clueGoal != null)
        {
            clueGoal.DeactivateClue();

            clueGoal.reactionStarted = false;

            Debug.Log(
                $"[LIGHT CLUE] {name}: goalAchieved = FALSE"
            );
        }
    }

    // --------------------------------------------------------
    // RESTORE INSTANT
    // --------------------------------------------------------

    protected override void RestoreInstant()
    {
        if (lamp != null)
        {
            Set(normal);
        }

        if (clueGoal != null)
        {
            clueGoal.DeactivateClue();
            clueGoal.reactionStarted = false;
        }
    }

    // --------------------------------------------------------
    // FADE
    // --------------------------------------------------------

    protected IEnumerator Fade(
        float from,
        float to,
        float time
    )
    {
        if (time <= 0f)
        {
            Set(to);
            yield break;
        }

        for (
            float t = 0f;
            t < time;
            t += Time.deltaTime
        )
        {
            float progress = t / time;

            Set(
                Mathf.Lerp(
                    from,
                    to,
                    progress
                )
            );

            yield return null;
        }

        Set(to);
    }
}