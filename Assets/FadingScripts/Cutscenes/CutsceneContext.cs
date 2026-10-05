using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CastStance { Standing, Sitting }

// Goes in: nowhere (CutsceneRunner creates one for each cutscene and hands it to Cutscene.Play).
// The toolbox of steps a cutscene can use. Every step is used like:   yield return c.Say("MOM", "Hello", 3f);
//
//   Time:       Wait
//   Screen:     Fade, FadeNow, Letterbox
//   Words:      Say (a subtitle), Title (big centred text)
//   Camera:     CutCamera (instant), MoveCamera (smooth), CameraLight (soft light so dark nights can be seen)
//   Characters: Spawn, MoveActor, TurnActor, Stance, Anim, AnimBool, Show
//   The cradle: Cradle (a cradle, with Luna lying in it or empty), BabyLooksUpAndSmiles (close-up: she looks at us and smiles)
//   Dreams:     DreamHaze (soft white haze over the picture), Glow (a warm light), Glimmers (floating memory sparks),
//               BabyLooksAt (Luna turns her head to a character or the camera, and smiles)
//   "Father" is the ghost himself: a pale, softly glowing figure (or Resources/Cast/Father if a model is added).
//   World:      Touch (play an object's own sign), Extinguish (a candle goes out), Dawn (morning light)
//   Sound:      Sound
//   Positions:  House(x, y, z) turns house-model coordinates into world coordinates
//
// CHARACTERS: Grandma is the real model that is already in the scene. Mom and Baby are created here as simple stand-in
// figures (coloured like their models: Mom = dark grey top, jeans, red sneakers; Baby = white beanie, light blue diaper).
// When a real model prefab named Cast/Mom or Cast/Baby exists in an Assets/Resources folder, it is used instead, automatically.
public class CutsceneContext
{
    // Names the cutscenes use -> a piece of the real object's name in the scene.
    static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>
    {
        { "Grandma", "Grandmother" }, { "Granny", "Grandmother" },
    };

    class Mannequin
    {
        public GameObject root;
        public Transform legL, legR, shoeL, shoeR, torso, armL, armR, head, hair, nose;
        public float scale;
    }

    readonly Dictionary<string, GameObject> actors = new Dictionary<string, GameObject>();
    readonly Dictionary<string, Mannequin> standIns = new Dictionary<string, Mannequin>();
    readonly HashSet<string> warned = new HashSet<string>();
    readonly List<AudioListener> mutedListeners = new List<AudioListener>();
    readonly List<Renderer> hidden = new List<Renderer>();
    readonly List<GameObject> spawned = new List<GameObject>();

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

    // ---------------------------------------------------------------- start and end (the runner calls these)
    // Hides what must not be seen (the player's body, the held candle, the placeholder NPCs of the AI test) and waits
    // a few frames, because the GPU resident drawer is a frame late to react.
    public IEnumerator Begin()
    {
        FirstPersonController player = Object.FindFirstObjectByType<FirstPersonController>();
        if (player != null) Hide(player.transform.root.GetComponentsInChildren<Renderer>());
        GameObject flame = GameObject.Find("CandleFlame");
        if (flame != null) Hide(flame.GetComponentsInChildren<Renderer>());
        foreach (MonoBehaviour b in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b == null) continue;
            string type = b.GetType().Name;
            if (type == "GrandmaAI" || type == "BabyAI") Hide(b.GetComponentsInChildren<Renderer>());
        }
        savedFog = RenderSettings.fog;
        RenderSettings.fog = false;                          // cutscenes are shown clearly, without the game's fog
        for (int i = 0; i < 4; i++) yield return null;
    }

    bool savedFog;

    void Hide(Renderer[] renderers)
    {
        foreach (Renderer r in renderers)
        {
            if (r == null || !r.enabled) continue;
            r.enabled = false;
            hidden.Add(r);
        }
    }

    // The end: if the cutscene was skipped, quickly fade to black first; then give the view back to the player and fade into the game.
    public IEnumerator Finish(bool skipped)
    {
        FadingHud.ClearSubtitle();
        if (skipped || fade.color.a < 0.99f) yield return Fade(1f, 0.4f);

        foreach (AudioListener l in mutedListeners) if (l != null) l.enabled = true;
        if (cam != null) Object.Destroy(cam.gameObject);
        foreach (GameObject g in spawned) if (g != null) Object.Destroy(g);
        foreach (Mannequin m in standIns.Values) if (m.root != null) Object.Destroy(m.root);
        foreach (Renderer r in hidden) if (r != null) r.enabled = true;
        RenderSettings.fog = savedFog;
        barTop.color = barBottom.color = new Color(0f, 0f, 0f, 0f);

        yield return Fade(0f, 1f);
        if (canvas != null) Object.Destroy(canvas.gameObject);
    }

    // ---------------------------------------------------------------- screen layers and camera
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
        cam.nearClipPlane = 0.05f;
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

    // ---------------------------------------------------------------- time
    public IEnumerator Wait(float seconds) { yield return new WaitForSeconds(seconds); }

    // ---------------------------------------------------------------- screen
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

    // ---------------------------------------------------------------- words
    // A subtitle: speaker name (can be "") and the line. Waits until the line is done.
    // 'voice' is optional: the name of a voice file in Assets/Resources/Audio/ (e.g. "voice_mom_eyes"). Played if it exists.
    public IEnumerator Say(string speaker, string line, float seconds, string voice = null)
    {
        FadingHud.Subtitle(speaker, line, seconds);
        if (voice != null) GameAudio.Play(voice, 1f);
        yield return new WaitForSeconds(seconds);
    }

    // A sound effect by file name (Assets/Resources/Audio/<name>). Does nothing if the file does not exist. Does not wait.
    public IEnumerator Sfx(string name, float volume = 1f)
    {
        GameAudio.Play(name, volume);
        yield break;
    }

    // Big centred text (e.g. an ending's name). Waits until it is gone.
    public IEnumerator Title(string text, float seconds)
    {
        FadingHud.Toast(text, seconds);
        yield return new WaitForSeconds(seconds);
    }

    // ---------------------------------------------------------------- camera
    public IEnumerator CutCamera(Vector3 position, Vector3 lookAt, float fov = 60f)
    {
        cam.transform.SetPositionAndRotation(position, Quaternion.LookRotation(lookAt - position));
        cam.fieldOfView = fov;
        yield break;
    }

    // Glide the camera to a new position while turning to look at a point (and optionally changing the zoom).
    public IEnumerator MoveCamera(Vector3 position, Vector3 lookAt, float seconds, float fov = -1f)
    {
        Vector3 fromPos = cam.transform.position;
        Quaternion fromRot = cam.transform.rotation;
        float fromFov = cam.fieldOfView;
        Quaternion toRot = Quaternion.LookRotation(lookAt - position);
        float toFov = fov > 0f ? fov : fromFov;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / seconds);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, position, k), Quaternion.Slerp(fromRot, toRot, k));
            cam.fieldOfView = Mathf.Lerp(fromFov, toFov, k);
            yield return null;
        }
        cam.transform.SetPositionAndRotation(position, toRot);
        cam.fieldOfView = toFov;
    }

    // Two soft cold lights that ride on the camera, so dark night scenes can be seen. They go away with the camera.
    public IEnumerator CameraLight(bool on, float intensity = 0.9f)
    {
        foreach (Light l in cam.GetComponentsInChildren<Light>()) Object.Destroy(l.gameObject);
        if (on)
        {
            AddLight(Quaternion.Euler(20f, 0f, 0f), intensity);
            AddLight(Quaternion.Euler(-10f, 160f, 0f), intensity * 0.45f);
        }
        yield break;
    }

    void AddLight(Quaternion localRotation, float intensity)
    {
        GameObject g = new GameObject("CutsceneLight");
        g.transform.SetParent(cam.transform, false);
        g.transform.localRotation = localRotation;
        Light l = g.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = intensity;
        l.color = new Color(0.72f, 0.80f, 1f);
        l.shadows = LightShadows.None;
    }

    // ---------------------------------------------------------------- characters
    // Put a character in the world. 'position' and 'lookAt' are house-model coordinates (x, height above the floor, z).
    public IEnumerator Spawn(string who, Vector3 position, Vector3 lookAt, CastStance stance = CastStance.Standing)
    {
        Vector3 world = House(position.x, position.y, position.z);
        Vector3 toward = House(lookAt.x, lookAt.y, lookAt.z) - world;
        toward.y = 0f;

        GameObject prefab = Resources.Load<GameObject>("Cast/" + who);
        GameObject go;
        if (prefab != null)
        {
            go = Object.Instantiate(prefab);
            spawned.Add(go);
            if (who == "Father") MakeGhostly(go);
            // The baby's only animation is crawling: when she should sit still, freeze it.
            Animator animator = go.GetComponentInChildren<Animator>();
            if (animator != null && stance == CastStance.Sitting && who == "Baby") animator.speed = 0f;
        }
        else
        {
            Mannequin m = BuildStandIn(who);
            standIns[who] = m;
            SetStance(m, stance);
            go = m.root;
        }
        actors[who] = go;
        go.transform.position = world;
        if (toward.sqrMagnitude > 0.0001f) go.transform.rotation = Quaternion.LookRotation(toward);

        // A real model: wait a moment for its animation to take its pose, then put its lowest point exactly on the surface
        // (so a baby on the bed rests on the mattress and does not float).
        if (prefab != null)
        {
            yield return null;
            yield return null;
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
                go.transform.position += Vector3.up * (world.y - b.min.y);
            }
        }
    }

    // Walk a character to a house position. Real models slide (and get their "Walking" value set); stand-ins also bob a little.
    public IEnumerator MoveActor(string name, Vector3 position, float seconds)
    {
        GameObject a = Actor(name);
        if (a == null) { yield return new WaitForSeconds(seconds); yield break; }

        Vector3 from = a.transform.position;
        Vector3 to = House(position.x, position.y, position.z);
        Vector3 dir = to - from;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) a.transform.rotation = Quaternion.LookRotation(dir);

        bool standIn = standIns.ContainsKey(name);
        SetBoolIfExists(a, "Walking", true);
        SetFloatIfExists(a, "Speed", 1f);                 // Mom's animator uses a "Speed" value
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            Vector3 p = Vector3.Lerp(from, to, t / seconds);
            if (standIn) p.y += Mathf.Abs(Mathf.Sin(t * 8f)) * 0.04f;
            a.transform.position = p;
            yield return null;
        }
        a.transform.position = to;
        SetBoolIfExists(a, "Walking", false);
        SetFloatIfExists(a, "Speed", 0f);
    }

    // Turn a character to look at a house position.
    public IEnumerator TurnActor(string name, Vector3 lookAt, float seconds)
    {
        GameObject a = Actor(name);
        if (a == null) { yield return new WaitForSeconds(seconds); yield break; }

        Vector3 dir = House(lookAt.x, lookAt.y, lookAt.z) - a.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) yield break;
        Quaternion from = a.transform.rotation, to = Quaternion.LookRotation(dir);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            a.transform.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        a.transform.rotation = to;
    }

    // Standing or sitting (on the floor / in bed). Only changes stand-in figures; real models use Anim.
    public IEnumerator Stance(string name, CastStance stance)
    {
        Mannequin m;
        if (standIns.TryGetValue(name, out m)) SetStance(m, stance);
        yield break;
    }

    // Fire an Animator trigger on a real model (e.g. "Sit", "Wave"). Does not wait.
    public IEnumerator Anim(string name, string trigger)
    {
        GameObject a = Actor(name);
        Animator animator = a != null ? a.GetComponentInChildren<Animator>() : null;
        if (animator != null && HasParameter(animator, trigger)) animator.SetTrigger(trigger);
        yield break;
    }

    // Set an Animator true/false value on a real model (e.g. "Walking").
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

    // ---------------------------------------------------------------- the world
    // Play a touchable object's own sign (e.g. "INT_Child_Mobile" spins, "INT_Grandma_RockingChair" rocks).
    public IEnumerator Touch(string objectName)
    {
        Interactable i = FindInteractable(objectName);
        if (i != null) i.TryInteract();
        yield break;
    }

    // A candle goes out: its flame light fades away.
    public IEnumerator Extinguish(string candleName, float seconds)
    {
        Interactable candle = FindInteractable(candleName);
        if (candle == null) yield break;

        Light[] lights = candle.GetComponentsInChildren<Light>(true);
        float[] start = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) start[i] = lights[i].intensity;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            for (int i = 0; i < lights.Length; i++) lights[i].intensity = Mathf.Lerp(start[i], 0f, t / seconds);
            yield return null;
        }
        foreach (Light l in lights) { l.intensity = 0f; l.gameObject.SetActive(false); }
        foreach (Renderer r in candle.GetComponentsInChildren<Renderer>())
            if (r.gameObject.name == "Flame") r.gameObject.SetActive(false);
    }

    // Switch the world to the morning light of Day 3 (used by the ending). Stops the day timer.
    public IEnumerator Dawn()
    {
        DayNightCycle cycle = Object.FindFirstObjectByType<DayNightCycle>();
        if (cycle != null)
        {
            cycle.autoAdvanceDays = false;
            cycle.showPhaseTitle = false;                       // no "Day 3" title over the ending
            cycle.SetPhase(DayNightCycle.Phase.Day3, true);
        }
        yield break;
    }

    // ---------------------------------------------------------------- sound
    public IEnumerator Sound(AudioClip clip, float volume = 1f)
    {
        if (clip != null) AudioSource.PlayClipAtPoint(clip, cam.transform.position, volume);
        yield break;
    }

    // ---------------------------------------------------------------- positions
    // Turn a point of the house MODEL (x, height above the floor, z) into a world position.
    // The ghost's own model: paler, softly glowing, no shadow.
    static void MakeGhostly(GameObject g)
    {
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (Material m in r.materials)
            {
                Color pale = Color.Lerp(m.color, new Color(0.8f, 0.88f, 1f), 0.45f);
                m.color = pale;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", pale * 0.35f);
            }
        }
    }

    // ---------------------------------------------------------------- dreams
    // A soft haze over the whole picture (amount 0 = none, 0.15 = dreamy). Uses the fade layer, so call it after fading in.
    public IEnumerator DreamHaze(float amount, float seconds)
    {
        yield return Fade(amount, seconds, new Color(0.88f, 0.92f, 1f));
    }

    // A light at a house position (it goes away when the cutscene ends).
    public IEnumerator Glow(Vector3 position, Color color, float intensity = 2f, float range = 5f)
    {
        GameObject g = new GameObject("CutsceneGlow");
        g.transform.position = House(position.x, position.y, position.z);
        Light l = g.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        spawned.Add(g);
        yield break;
    }

    // Small glowing sparks drifting around a house position.
    public IEnumerator Glimmers(Vector3 center, int count = 12, float radius = 1.5f)
    {
        Vector3 c = House(center.x, center.y, center.z);
        for (int i = 0; i < count; i++)
        {
            GameObject g = FadingMaterials.Primitive(PrimitiveType.Sphere);
            Object.Destroy(g.GetComponent<Collider>());
            g.name = "Glimmer";
            g.transform.position = c + new Vector3(Random.Range(-radius, radius), Random.Range(-0.5f, 0.8f), Random.Range(-radius, radius));
            g.transform.localScale = Vector3.one * Random.Range(0.025f, 0.05f);
            Renderer r = g.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Color col = Color.Lerp(new Color(1f, 0.9f, 0.7f), new Color(0.75f, 0.85f, 1f), Random.value);
            r.material.color = col;
            r.material.EnableKeyword("_EMISSION");
            r.material.SetColor("_EmissionColor", col * 4f);
            g.AddComponent<Glimmer>();
            spawned.Add(g);
        }
        yield break;
    }

    // Luna (the spawned "Baby") turns her head toward another character ("Father", "Mom") or "camera", and smiles.
    public IEnumerator BabyLooksAt(string target, bool smile = true)
    {
        GameObject baby = Actor("Baby");
        if (baby == null) yield break;
        foreach (SkinnedMeshRenderer r in baby.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;
        BabyFace face = BabyFace.On(baby);
        Transform look;
        if (target == "camera") look = cam.transform;
        else if (standIns.ContainsKey(target)) look = standIns[target].head;              // a stand-in figure: its head
        else look = HeadOf(Actor(target));
        face.LookAt(look);
        face.Smile(smile);
        yield break;
    }

    static Transform HeadOf(GameObject a)
    {
        if (a == null) return null;
        Animator animator = a.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman && animator.GetBoneTransform(HumanBodyBones.Head) != null) return animator.GetBoneTransform(HumanBodyBones.Head);
        return a.transform;
    }

    // ---------------------------------------------------------------- the cradle (ending)
    GameObject cradle;
    BabyFace cradleFace;

    // A cradle at a house position; its long side faces 'lookAt'. withBaby = Luna lies in it (the baby model, sleeping pose).
    public IEnumerator Cradle(Vector3 position, Vector3 lookAt, bool withBaby)
    {
        Vector3 world = House(position.x, position.y, position.z);
        Vector3 toward = House(lookAt.x, lookAt.y, lookAt.z) - world;
        toward.y = 0f;

        float mattress;
        cradle = CradleModel.Make(out mattress);
        spawned.Add(cradle);
        cradle.transform.position = world;
        if (withBaby) yield return LayBabyIn(world + Vector3.up * mattress);
        if (toward.sqrMagnitude > 0.0001f) cradle.transform.rotation = Quaternion.LookRotation(toward);
    }

    IEnumerator LayBabyIn(Vector3 mattressTop)
    {
        GameObject prefab = Resources.Load<GameObject>("Cast/Baby");
        if (prefab == null) yield break;                                     // no baby model yet: the cradle stays empty

        GameObject baby = Object.Instantiate(prefab, mattressTop, Quaternion.identity);
        spawned.Add(baby);
        actors["Baby"] = baby;
        AnimationClip lying = GameClips.First(baby, "baby_lie", "sleeping");
        Animator animator = baby.GetComponentInChildren<Animator>();
        foreach (SkinnedMeshRenderer r in baby.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;   // measure the real pose
        if (!PosePlayer.On(baby).Play(lying, 0.05f) && animator != null) animator.speed = 0f;
        for (int i = 0; i < 3; i++) yield return null;                     // let the pose settle before measuring

        // Lie along the cradle, small enough to fit, centred, resting on the mattress.
        Bounds b = BoundsOf(baby);
        if (b.size.z > b.size.x) { baby.transform.Rotate(0f, 90f, 0f, Space.World); b = BoundsOf(baby); }
        float fit = Mathf.Min(1f, 0.78f / Mathf.Max(0.01f, b.size.x), 0.40f / Mathf.Max(0.01f, b.size.z));
        baby.transform.localScale *= fit;
        b = BoundsOf(baby);
        baby.transform.position += new Vector3(mattressTop.x - b.center.x, mattressTop.y - b.min.y, mattressTop.z - b.center.z);
        baby.transform.SetParent(cradle.transform, true);
        cradleFace = BabyFace.On(baby);
    }

    // Close-up from above the cradle: Luna turns her head, looks up at the camera (at us) and smiles.
    // Fade to black before this step; it fades in by itself.
    public IEnumerator BabyLooksUpAndSmiles(float seconds = 6f)
    {
        if (cradleFace == null) yield break;
        Vector3 head = cradleFace.HeadPosition;
        Vector3 side = cradle != null ? cradle.transform.forward : Vector3.forward;
        Vector3 from = head + Vector3.up * 0.55f + side * 0.22f;
        yield return CutCamera(from, head, 42f);
        yield return Fade(0f, 1.2f);
        yield return Wait(0.5f);
        cradleFace.LookAt(cam.transform);
        yield return Wait(1.1f);
        cradleFace.Smile(true);
        GameAudio.Play("baby_giggle", 0.9f);
        yield return MoveCamera(from + (head - from) * 0.18f, head, Mathf.Max(1f, seconds - 1.6f), 38f);     // a slow push in
    }

    static Bounds BoundsOf(GameObject g)
    {
        Renderer[] renderers = g.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }

    public Vector3 House(float x, float y, float z)
    {
        Vector3 local = new Vector3(-x, y, z);            // the model is mirrored in x when it is imported
        return house != null ? house.TransformPoint(local) : local;
    }

    // ---------------------------------------------------------------- finding things
    GameObject Actor(string name)
    {
        GameObject found;
        if (actors.TryGetValue(name, out found) && found != null) return found;

        string part;
        if (!Aliases.TryGetValue(name, out part)) part = name;
        foreach (Animator a in Object.FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (a.gameObject.name.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
            actors[name] = a.gameObject;
            return a.gameObject;
        }

        if (warned.Add(name)) Debug.LogWarning("Cutscene: character '" + name + "' was not found in the scene, skipping their steps.");
        return null;
    }

    static Interactable FindInteractable(string objectName)
    {
        foreach (Interactable i in Object.FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (i.gameObject.name == objectName) return i;
        return null;
    }

    static void SetBoolIfExists(GameObject a, string parameter, bool value)
    {
        Animator animator = a.GetComponentInChildren<Animator>();
        if (animator != null && HasParameter(animator, parameter)) animator.SetBool(parameter, value);
    }

    static void SetFloatIfExists(GameObject a, string parameter, float value)
    {
        Animator animator = a.GetComponentInChildren<Animator>();
        if (animator != null && HasParameter(animator, parameter)) animator.SetFloat(parameter, value);
    }

    static bool HasParameter(Animator animator, string parameter)
    {
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == parameter) return true;
        return false;
    }

    // ---------------------------------------------------------------- stand-in figures
    static readonly Color Skin = new Color(0.86f, 0.70f, 0.60f);

    static Mannequin BuildStandIn(string who)
    {
        if (who == "Father")
        {
            Mannequin ghost = BuildMannequin("Father", new Color(0.78f, 0.85f, 0.97f), new Color(0.62f, 0.68f, 0.82f),
                                             new Color(0.55f, 0.6f, 0.75f), new Color(0.7f, 0.76f, 0.9f), 1.05f, 1f);
            foreach (Renderer r in ghost.root.GetComponentsInChildren<Renderer>())
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", r.material.color * 0.55f);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return ghost;
        }
        if (who == "Baby" || who == "Luna")
            return BuildMannequin("Baby", new Color(0.62f, 0.80f, 0.90f), Skin, Skin, Color.white, 0.45f, 1.35f);          // white beanie, light blue diaper
        return BuildMannequin(who, new Color(0.20f, 0.20f, 0.22f), new Color(0.22f, 0.32f, 0.47f),                       // dark grey top, blue jeans
                              new Color(0.65f, 0.22f, 0.15f), new Color(0.16f, 0.11f, 0.09f), 1f, 1f);                     // red sneakers, dark hair
    }

    static Mannequin BuildMannequin(string name, Color top, Color legs, Color shoes, Color hair, float scale, float headScale)
    {
        Mannequin m = new Mannequin { scale = scale };
        m.root = new GameObject("Cast_" + name);
        m.root.transform.localScale = Vector3.one * scale;
        Transform r = m.root.transform;
        m.legL = Part(r, PrimitiveType.Capsule, legs);
        m.legR = Part(r, PrimitiveType.Capsule, legs);
        m.shoeL = Part(r, PrimitiveType.Cube, shoes);
        m.shoeR = Part(r, PrimitiveType.Cube, shoes);
        m.torso = Part(r, PrimitiveType.Capsule, top);
        m.armL = Part(r, PrimitiveType.Capsule, Skin);
        m.armR = Part(r, PrimitiveType.Capsule, Skin);
        m.head = Part(r, PrimitiveType.Sphere, Skin);
        m.hair = Part(r, PrimitiveType.Sphere, hair);
        m.nose = Part(r, PrimitiveType.Sphere, new Color(0.78f, 0.60f, 0.50f));      // shows which way the figure looks
        m.head.localScale = Vector3.one * 0.22f * headScale;
        m.hair.localScale = Vector3.one * 0.25f * headScale;
        m.nose.localScale = Vector3.one * 0.05f * headScale;
        return m;
    }

    static Transform Part(Transform parent, PrimitiveType type, Color color)
    {
        GameObject g = FadingMaterials.Primitive(type);
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.GetComponent<Renderer>().material.color = color;
        return g.transform;
    }

    static void Put(Transform t, Vector3 position, Vector3 euler, Vector3 scale)
    {
        t.localPosition = position;
        t.localRotation = Quaternion.Euler(euler);
        t.localScale = scale;
    }

    // Standing (feet on the floor) or sitting on the floor / in bed (legs stretched forward).
    static void SetStance(Mannequin m, CastStance stance)
    {
        float hs = m.head.localScale.x / 0.22f;               // 1 for adults, bigger for the baby
        if (stance == CastStance.Standing)
        {
            Put(m.shoeL, new Vector3(-0.09f, 0.035f, 0.04f), Vector3.zero, new Vector3(0.12f, 0.07f, 0.26f));
            Put(m.shoeR, new Vector3(0.09f, 0.035f, 0.04f), Vector3.zero, new Vector3(0.12f, 0.07f, 0.26f));
            Put(m.legL, new Vector3(-0.09f, 0.50f, 0f), Vector3.zero, new Vector3(0.15f, 0.43f, 0.15f));
            Put(m.legR, new Vector3(0.09f, 0.50f, 0f), Vector3.zero, new Vector3(0.15f, 0.43f, 0.15f));
            Put(m.torso, new Vector3(0f, 1.22f, 0f), Vector3.zero, new Vector3(0.34f, 0.30f, 0.20f));
            Put(m.armL, new Vector3(-0.24f, 1.20f, 0f), Vector3.zero, new Vector3(0.09f, 0.30f, 0.09f));
            Put(m.armR, new Vector3(0.24f, 1.20f, 0f), Vector3.zero, new Vector3(0.09f, 0.30f, 0.09f));
            m.head.localPosition = new Vector3(0f, 1.64f, 0f);
            m.hair.localPosition = new Vector3(0f, 1.67f, -0.02f * hs);
            m.nose.localPosition = new Vector3(0f, 1.63f, 0.11f * hs);
        }
        else
        {
            Put(m.shoeL, new Vector3(-0.09f, 0.07f, 0.92f), Vector3.zero, new Vector3(0.12f, 0.07f, 0.26f));
            Put(m.shoeR, new Vector3(0.09f, 0.07f, 0.92f), Vector3.zero, new Vector3(0.12f, 0.07f, 0.26f));
            Put(m.legL, new Vector3(-0.09f, 0.12f, 0.45f), new Vector3(90f, 0f, 0f), new Vector3(0.15f, 0.43f, 0.15f));
            Put(m.legR, new Vector3(0.09f, 0.12f, 0.45f), new Vector3(90f, 0f, 0f), new Vector3(0.15f, 0.43f, 0.15f));
            Put(m.torso, new Vector3(0f, 0.52f, 0f), Vector3.zero, new Vector3(0.34f, 0.30f, 0.20f));
            Put(m.armL, new Vector3(-0.24f, 0.50f, 0.05f), Vector3.zero, new Vector3(0.09f, 0.30f, 0.09f));
            Put(m.armR, new Vector3(0.24f, 0.50f, 0.05f), Vector3.zero, new Vector3(0.09f, 0.30f, 0.09f));
            m.head.localPosition = new Vector3(0f, 0.96f, 0f);
            m.hair.localPosition = new Vector3(0f, 0.99f, -0.02f * hs);
            m.nose.localPosition = new Vector3(0f, 0.95f, 0.11f * hs);
        }
    }
}
