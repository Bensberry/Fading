using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Goes in: nowhere by hand. ChapterRules adds it to the player's held candle.
// Press H: the candle shines a beam on the thing you should touch next and a small popup explains it.
// You cannot spam it:
//   - only 'maxHints' hints per night/day (the dots at the bottom-left)
//   - after each hint the candle burns low for 'cooldownSeconds' and H does nothing
//   - after the last hint the candle burns out completely for 'burnOutSeconds', then it relights with full hints
//   - a new day/night (DayNightCycle) also relights it
public class CandleHint : MonoBehaviour
{
    public static event System.Action HintUsed;     // the tutorial listens to this

    public int maxHints = 3;
    public float cooldownSeconds = 20f;
    public float shineSeconds = 3.5f;
    public float burnOutSeconds = 45f;

    CandleLight flame;
    Light beam;
    int hintsLeft;
    float cooldownLeft, burnLeft;
    bool shining, burnedOut;
    readonly HashSet<Interactable> touched = new HashSet<Interactable>();

    void Start()
    {
        flame = GetComponent<CandleLight>();
        if (flame == null) flame = gameObject.AddComponent<CandleLight>();
        hintsLeft = maxHints;

        foreach (Interactable i in FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Interactable captured = i;
            i.onInteract.AddListener(() => touched.Add(captured));
        }
        DayNightCycle cycle = FindFirstObjectByType<DayNightCycle>();
        if (cycle != null) cycle.onPhaseChanged.AddListener(delegate { Relight(); });
    }

    void Update()
    {
        TickTimers();
        if (HintPressed()) TryUseHint();
        UpdateHud();
    }

    bool HintPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) || MobileControls.HintPressed;
#else
        return Input.GetKeyDown(KeyCode.H);
#endif
    }

    // ---------- candle state
    void TickTimers()
    {
        if (cooldownLeft > 0f) cooldownLeft = Mathf.Max(0f, cooldownLeft - Time.deltaTime);
        if (burnedOut)
        {
            burnLeft -= Time.deltaTime;
            if (burnLeft <= 0f) Relight();
        }
        if (flame == null || shining) return;
        flame.Brightness = burnedOut ? 0f : (cooldownLeft > 0f ? 0.4f : 1f);     // ember while recovering
    }

    void Relight()
    {
        burnedOut = false;
        hintsLeft = maxHints;
        cooldownLeft = 0f;
        FadingHud.Toast("The candle flickers back to life.", 2.5f);
        GameAudio.Play("candle_relight", 0.9f);
    }

    void UpdateHud()
    {
        FadingHud.SetCandle(hintsLeft, maxHints, cooldownSeconds > 0f ? cooldownLeft / cooldownSeconds : 0f, burnedOut);
    }

    // ---------- using a hint
    void TryUseHint()
    {
        if (shining) return;
        if (burnedOut) { FadingHud.Toast("The candle has burned out... it relights in " + Mathf.CeilToInt(burnLeft) + "s."); return; }
        if (cooldownLeft > 0f) { FadingHud.Toast("The flame is still recovering... (" + Mathf.CeilToInt(cooldownLeft) + "s)"); return; }

        Transform target; string text;
        if (!FindTarget(out target, out text)) { FadingHud.Toast("Nothing left to find right now."); return; }

        hintsLeft--;
        cooldownLeft = cooldownSeconds;
        GameAudio.Play("hint_whoosh", 0.9f);
        if (HintUsed != null) HintUsed();
        StartCoroutine(Shine(target, text));
    }

    IEnumerator Shine(Transform target, string text)
    {
        shining = true;
        FadingHud.Toast("Hint: " + text, shineSeconds + 1f);
        flame.Brightness = 1.7f;                           // the flame flares

        EnsureBeam();
        Vector3 point = PointOn(target);
        Quaternion from = transform.rotation;
        beam.transform.position = flame.FlameTransform.position;
        beam.enabled = true;

        for (float t = 0f; t < shineSeconds; t += Time.deltaTime)
        {
            beam.transform.position = flame.FlameTransform.position;
            Quaternion look = Quaternion.LookRotation(point - beam.transform.position);
            beam.transform.rotation = Quaternion.Slerp(from, look, Mathf.SmoothStep(0f, 1f, t / 0.5f));
            beam.intensity = 8f * Mathf.Clamp01(Mathf.Min(t / 0.4f, (shineSeconds - t) / 0.6f));
            yield return null;
        }
        beam.enabled = false;
        shining = false;

        if (hintsLeft <= 0)
        {
            burnedOut = true;
            burnLeft = burnOutSeconds;
            FadingHud.Toast("The candle burns out...", 3f);
            GameAudio.Play("candle_out", 0.9f);
        }
    }

    void EnsureBeam()
    {
        if (beam != null) return;
        GameObject g = new GameObject("CandleBeam");
        beam = g.AddComponent<Light>();
        beam.type = LightType.Spot;
        beam.color = new Color(1f, 0.8f, 0.5f);
        beam.spotAngle = 32f;
        beam.range = 14f;
        beam.shadows = LightShadows.Hard;
        beam.enabled = false;
    }

    void OnDestroy() { if (beam != null) Destroy(beam.gameObject); }

    // The middle of the target's collider or mesh.
    static Vector3 PointOn(Transform t)
    {
        Collider c = t.GetComponentInChildren<Collider>();
        if (c != null) return c.bounds.center;
        Renderer r = t.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds.center : t.position;
    }

    // ---------- what to point at
    bool FindTarget(out Transform target, out string text)
    {
        if (FindPuzzleTarget(out target, out text)) return true;
        if (NightQuest.TryHint(transform.position, out target, out text)) return true;      // at night: the memory lights
        return FindNearestUntouched(out target, out text);
    }

    // Chapter 0: lamp -> clock -> photo album (in this order), read from Granny's animator.
    bool FindPuzzleTarget(out Transform target, out string text)
    {
        target = null; text = "";
        LampInteraction lamp = FindFirstObjectByType<LampInteraction>();
        if (lamp == null || !lamp.isActiveAndEnabled || lamp.characterAnimator == null) return false;
        Animator a = lamp.characterAnimator;

        if (a.GetBool("hasLookedAtPhotoAlbum")) return false;                 // puzzle finished
        if (a.GetBool("hasLookedAtClock"))
        {
            PhotoAlbumInteraction album = FindFirstObjectByType<PhotoAlbumInteraction>();
            if (album == null) return false;
            target = album.transform;
            text = "Quickly - the photo album, before she looks back!";
        }
        else if (a.GetBool("hasFlickered"))
        {
            ClockInteraction clock = FindFirstObjectByType<ClockInteraction>();
            if (clock == null) return false;
            target = clock.transform;
            text = "Now the clock - touch it while she looks at the lamp.";
        }
        else
        {
            target = lamp.transform;
            text = "Grandma's lamp - flicker it to draw her eye.";
        }
        return true;
    }

    // Everywhere else: the closest touchable object you have not touched yet.
    bool FindNearestUntouched(out Transform target, out string text)
    {
        target = null; text = "";
        float best = float.MaxValue;
        foreach (Interactable i in FindObjectsByType<Interactable>(FindObjectsSortMode.None))
        {
            if (!i.isActiveAndEnabled || i is DoorToggle || i is RestSpot || touched.Contains(i)) continue;
            float d = (i.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; target = i.transform; }
        }
        if (target == null) return false;

        Interactable found = target.GetComponent<Interactable>();
        string room = RoomOf(target);
        text = (room.Length > 0 ? room + ": " : "") + found.prompt;
        return true;
    }

    // "Kitchen_Interactables" -> "Kitchen"
    static string RoomOf(Transform t)
    {
        for (Transform p = t.parent; p != null; p = p.parent)
            if (p.name.EndsWith("_Interactables")) return p.name.Replace("_Interactables", "");
        return "";
    }
}
