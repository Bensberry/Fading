using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Goes in: nowhere (CutsceneRunner creates one for each cutscene and hands it to Cutscene.Play).
// The toolbox of steps a cutscene can use. Every step is used like:   yield return c.Say("MOM", "Hello", 3f);
//
//   Time:       Wait
//   Screen:     Fade, FadeNow, Letterbox
//   Words:      Say (a subtitle), Title (big centred text)
//   Camera:     CutCamera (instant), MoveCamera (smooth)
//   Characters: MoveActor, TurnActor, Anim (Animator trigger), AnimBool, Show
//   Sound:      Sound
//   Positions:  House(x, y, z) turns house-model coordinates into world coordinates
//
// Characters are found by name: "Grandma", "Mom", "Child", "Father" (or the exact object name).
// A character that is not in the scene is simply skipped (with one warning in the Console), so a cutscene
// still plays before all the models exist.
public class CutsceneContext
{
    // Names the cutscenes use -> a piece of the real object's name in the scene.
    static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
    {
        { "Grandma", "Grandmother" }, { "Granny", "Grandmother" }, { "Mom", "Mom" }, { "Mother", "Mom" },
        { "Child", "Child" }, { "Father", "Father" }, { "Ghost", "Father" },
    };

    readonly Dictionary<string, GameObject> actors = new Dictionary<string, GameObject>();
    readonly HashSet<string> warned = new HashSet<string>();
    readonly List<AudioListener> mutedListeners = new List<AudioListener>();

    Canvas canvas;
    Image fade, barTop, barBottom;
    Camera cam;
    Transform house;

    public CutsceneContext()
    {
        BuildScreenLayers();
        BuildCamera();
        FadingInteractablesSetup setup = Object.FindFirstObjectByType<FadingInteractablesSetup>();
        house = setup != null ? setup.transform : null;
    }

    // ---------- setting up / cleaning up (the runner calls these)
    void BuildScreenLayers()
    {
        canvas = MenuKit.MakeCanvas("CutsceneCanvas", 900);
        fade = MakeLayer("Fade", Vector2.zero, Vector2.one, new Color(0f, 0f, 0f, 0f));
        barTop = MakeLayer("BarTop", new Vector2(0f, 0.9f), Vector2.one, new Color(0f, 0f, 0f, 0f));
        barBottom = MakeLayer("BarBottom", Vector2.zero, new Vector2(1f, 0.1f), new Color(0f, 0f, 0f, 0f));
    }

    Image MakeLayer(string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject g = new GameObject(name, typeof(RectTransform), typeof(Image));
        g.transform.SetParent(canvas.transform, false);
        RectTransform rt = (RectTransform)g.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image image = g.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // A separate camera that is drawn on top of the game camera. It starts exactly where the player's view is.
    void BuildCamera()
    {
        Camera main = Camera.main;
        GameObject g = new GameObject("CutsceneCamera");
        cam = g.AddComponent<Camera>();
        cam.depth = 50f;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 60f;
        if (main != null)
        {
            g.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            cam.fieldOfView = main.fieldOfView;
            foreach (AudioListener l in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (!l.enabled) continue;
                l.enabled = false;                       // only one listener may be active
                mutedListeners.Add(l);
            }
        }
        g.AddComponent<AudioListener>();
    }

    // The end: if the cutscene was skipped, quickly fade to black first; then give the view back to the player
    // and fade into the game.
    public IEnumerator Finish(bool skipped)
    {
        FadingHud.ClearSubtitle();
        if (skipped || fade.color.a < 0.99f) yield return Fade(1f, 0.4f);

        foreach (AudioListener l in mutedListeners) if (l != null) l.enabled = true;
        if (cam != null) Object.Destroy(cam.gameObject);
        barTop.color = barBottom.color = new Color(0f, 0f, 0f, 0f);

        yield return Fade(0f, 1f);
        if (canvas != null) Object.Destroy(canvas.gameObject);
    }

    // ---------- time
    public IEnumerator Wait(float seconds) { yield return new WaitForSeconds(seconds); }

    // ---------- screen
    // Fade the whole screen to a colour (alpha 1 = fully covered) or back to the game (alpha 0).
    public IEnumerator Fade(float toAlpha, float seconds, Color? colour = null)
    {
        Color start = fade.color;
        Color end = colour ?? new Color(start.r, start.g, start.b, 1f);
        end.a = toAlpha;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            fade.color = Color.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        fade.color = end;
    }

    // The same, instantly.
    public IEnumerator FadeNow(float alpha, Color? colour = null)
    {
        Color c = colour ?? new Color(fade.color.r, fade.color.g, fade.color.b, 1f);
        c.a = alpha;
        fade.color = c;
        yield break;
    }

    // Black cinema bars at the top and bottom of the screen.
    public IEnumerator Letterbox(bool on, float seconds = 0.8f)
    {
        float from = barTop.color.a, to = on ? 1f : 0f;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float a = Mathf.Lerp(from, to, t / seconds);
            barTop.color = barBottom.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        barTop.color = barBottom.color = new Color(0f, 0f, 0f, to);
    }

    // ---------- words
    // A subtitle: speaker name (can be "") and the line. Waits until the line is done.
    public IEnumerator Say(string speaker, string line, float seconds)
    {
        FadingHud.Subtitle(speaker, line, seconds);
        yield return new WaitForSeconds(seconds);
    }

    // Big centred text (e.g. an ending's name). Waits until it is gone.
    public IEnumerator Title(string text, float seconds)
    {
        FadingHud.Toast(text, seconds);
        yield return new WaitForSeconds(seconds);
    }

    // ---------- camera
    public IEnumerator CutCamera(Vector3 position, Vector3 lookAt)
    {
        cam.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
        yield break;
    }

    // Glide the camera to a new position while turning to look at a point.
    public IEnumerator MoveCamera(Vector3 position, Vector3 lookAt, float seconds)
    {
        Vector3 fromPos = cam.transform.position;
        Quaternion fromRot = cam.transform.rotation;
        Quaternion toRot = Quaternion.LookRotation(lookAt - position);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, position, k), Quaternion.Slerp(fromRot, toRot, k));
            yield return null;
        }
        cam.transform.SetPositionAndRotation(position, toRot);
    }

    // ---------- characters
    // Walk a character to a world position (keeps its own height) and turn it to face the way it walks.
    public IEnumerator MoveActor(string name, Vector3 worldPosition, float seconds)
    {
        GameObject a = Actor(name);
        if (a == null) { yield return new WaitForSeconds(seconds); yield break; }

        Vector3 from = a.transform.position;
        Vector3 to = new Vector3(worldPosition.x, from.y, worldPosition.z);
        Vector3 dir = to - from;
        if (dir.sqrMagnitude > 0.0001f) a.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));

        SetBoolIfExists(a, "Walking", true);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            a.transform.position = Vector3.Lerp(from, to, t / seconds);
            yield return null;
        }
        a.transform.position = to;
        SetBoolIfExists(a, "Walking", false);
    }

    // Turn a character to look at a point.
    public IEnumerator TurnActor(string name, Vector3 lookAt, float seconds)
    {
        GameObject a = Actor(name);
        if (a == null) { yield return new WaitForSeconds(seconds); yield break; }

        Quaternion from = a.transform.rotation;
        Vector3 dir = lookAt - a.transform.position;
        Quaternion to = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            a.transform.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        a.transform.rotation = to;
    }

    // Fire an Animator trigger on a character (e.g. "Sit", "Wave"). Does not wait.
    public IEnumerator Anim(string name, string trigger)
    {
        GameObject a = Actor(name);
        Animator animator = a != null ? a.GetComponentInChildren<Animator>() : null;
        if (animator != null && HasParameter(animator, trigger)) animator.SetTrigger(trigger);
        yield break;
    }

    // Set an Animator true/false value on a character (e.g. "Walking").
    public IEnumerator AnimBool(string name, string parameter, bool value)
    {
        GameObject a = Actor(name);
        if (a != null) SetBoolIfExists(a, parameter, value);
        yield break;
    }

    public IEnumerator Show(string name, bool visible)
    {
        GameObject a = Actor(name);
        if (a != null) a.SetActive(visible);
        yield break;
    }

    // ---------- sound
    public IEnumerator Sound(AudioClip clip, float volume = 1f)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, cam.transform.position, volume);
        yield break;
    }

    // ---------- positions
    // Turn a point of the house MODEL (x, height, z as in the model: front door about x 8.1, z 0) into a world position.
    public Vector3 House(float x, float y, float z)
    {
        Vector3 local = new Vector3(-x, y, z);            // the model is mirrored in x when it is imported
        return house != null ? house.TransformPoint(local) : local;
    }

    // The position of a character (so the camera can look at them). Falls back to the origin if they are missing.
    public Vector3 PositionOf(string name, float height = 1.5f)
    {
        GameObject a = Actor(name);
        return a != null ? a.transform.position + Vector3.up * height : Vector3.zero;
    }

    // ---------- finding characters
    GameObject Actor(string name)
    {
        GameObject found;
        if (actors.TryGetValue(name, out found) && found != null) return found;

        string part;
        if (!Aliases.TryGetValue(name, out part)) part = name;
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (t.name.StartsWith("INT_") || t.GetComponentInParent<Interactable>() != null) continue;     // not a touchable object
            if (t.GetComponent<Animator>() == null && t.GetComponentInChildren<Animator>() == null) continue;
            actors[name] = t.gameObject;
            return t.gameObject;
        }

        if (warned.Add(name)) Debug.LogWarning("Cutscene: character '" + name + "' was not found in the scene, skipping their steps.");
        return null;
    }

    static void SetBoolIfExists(GameObject a, string parameter, bool value)
    {
        Animator animator = a.GetComponentInChildren<Animator>();
        if (animator != null && HasParameter(animator, parameter)) animator.SetBool(parameter, value);
    }

    static bool HasParameter(Animator animator, string parameter)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == parameter) return true;
        return false;
    }
}
