using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom and Luna live).
// The NIGHT quest. While the family sleeps, MEMORY LIGHTS (small glowing lights) appear in the rooms of the house.
//   1. Walk into the lights to gather them (LightsToFind).
//   2. Bring them to Mom's bed (or Luna's): she DREAMS of you. +15 on the bar, and the night ends a few seconds later.
// The candle hint (H) points at the next light, then at the sleeping family.
// The lights are made in code (no model files needed).
public class NightQuest : MonoBehaviour
{
    const int LightsToFind = 3;
    const float GatherDistance = 1.4f;
    const float DeliverDistance = 2.4f;

    static readonly string[] Rooms = { "Hallway", "LivingRoom", "Kitchen", "GuestRoom" };

    public static NightQuest Instance { get; private set; }
    public static bool Complete { get; private set; }          // ChapterRules ends the night soon after this
    public static bool Running { get { return Instance != null && Instance.active; } }

    readonly List<Transform> lights = new List<Transform>();
    DayNightCycle cycle;
    GrandmaAI mom;
    BabyAI baby;
    bool active, carrying;
    int found;

    void Awake() { Instance = this; Complete = false; }

    void Start()
    {
        cycle = FindAnyObjectByType<DayNightCycle>();
        mom = FindAnyObjectByType<GrandmaAI>();
        baby = FindAnyObjectByType<BabyAI>();
        if (cycle == null || (mom == null && baby == null)) { enabled = false; return; }
        cycle.onPhaseChanged.AddListener(delegate { OnPhase(); });
        OnPhase();
    }

    void OnPhase()
    {
        if (cycle.IsNight) Begin();
        else Stop();
    }

    // ---------- the night begins: lights appear
    void Begin()
    {
        Stop();
        active = true;
        Complete = false;
        found = 0;
        carrying = false;

        List<string> rooms = new List<string>(Rooms);
        for (int i = 0; i < LightsToFind && rooms.Count > 0; i++)
        {
            string room = rooms[Random.Range(0, rooms.Count)];
            rooms.Remove(room);
            Vector3 p;
            if (HouseRooms.RandomPoint(room, out p)) lights.Add(MakeLight(p + Vector3.up * 1.1f));
        }
        if (lights.Count == 0) { active = false; return; }
        FadingHud.Toast("The family is asleep. Memories are glowing in the house...", 4f);
        ShowGoal();
    }

    void Stop()
    {
        foreach (Transform l in lights) if (l != null) Destroy(l.gameObject);
        lights.Clear();
        active = false;
    }

    static Transform MakeLight(Vector3 at)
    {
        GameObject g = FadingMaterials.Primitive(PrimitiveType.Sphere);
        g.name = "MemoryLight";
        Destroy(g.GetComponent<Collider>());
        g.transform.position = at;
        g.transform.localScale = Vector3.one * 0.16f;
        Renderer r = g.GetComponent<Renderer>();
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Color c = new Color(0.7f, 0.85f, 1f);
        r.material.color = c;
        r.material.EnableKeyword("_EMISSION");
        r.material.SetColor("_EmissionColor", c * 4f);

        Light glow = g.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = c;
        glow.range = 3.5f;
        glow.intensity = 1.8f;
        glow.shadows = LightShadows.None;
        return g.transform;
    }

    // ---------- every frame: float, gather, deliver
    void Update()
    {
        if (!active || Camera.main == null) return;
        Vector3 player = Camera.main.transform.position;

        for (int i = lights.Count - 1; i >= 0; i--)
        {
            Transform l = lights[i];
            if (l == null) { lights.RemoveAt(i); continue; }
            Float(l, i);
            if (Flat(l.position, player) < GatherDistance && Mathf.Abs(l.position.y - player.y) < 1.6f) Gather(i);
        }

        if (carrying && !Complete && !CutsceneRunner.IsPlaying)
        {
            if (mom != null && Flat(mom.transform.position, player) < DeliverDistance) Deliver(true);
            else if (baby != null && Flat(baby.transform.position, player) < DeliverDistance) Deliver(false);
        }
    }

    void Float(Transform l, int i)
    {
        float t = Time.time + i * 1.7f;
        Vector3 p = l.position;
        p.y += Mathf.Sin(t * 1.6f) * 0.12f * Time.deltaTime;
        l.position = p;
        l.localScale = Vector3.one * (0.15f + Mathf.Sin(t * 3f) * 0.02f);
        Light glow = l.GetComponent<Light>();
        if (glow != null) glow.intensity = 1.6f + Mathf.Sin(t * 2.3f) * 0.4f;
    }

    void Gather(int index)
    {
        Destroy(lights[index].gameObject);
        lights.RemoveAt(index);
        found++;
        GameAudio.Play("memory_collect", 0.8f);
        if (lights.Count == 0)
        {
            carrying = true;
            FadingHud.Toast("You hold their memories. Bring them to Mom or Luna while they sleep.", 4f);
        }
        else FadingHud.Toast("A memory... (" + found + "/" + (found + lights.Count) + ")", 2f);
        ShowGoal();
    }

    void Deliver(bool toMom)
    {
        Complete = true;
        carrying = false;
        GameAudio.Play("dream_swell", 0.9f);
        if (toMom)
        {
            FamilyProgress.Award("dream:mom", 15f, 15f, "Mom dreams of you");
            FadingHud.Subtitle("", "In her dream, I am home again.", 4f);
        }
        else
        {
            FamilyProgress.Award("dream:luna", 15f, 15f, "Luna dreams of you");
            BabyLife babyLife = FindAnyObjectByType<BabyLife>();
            if (babyLife != null && babyLife.Face != null) babyLife.Face.Smile(true);
            FadingHud.Subtitle("", "She smiles in her sleep.", 4f);
        }
        FadingHud.SetObjective("The night is quiet now...");
    }

    void ShowGoal()
    {
        if (!active) return;
        if (Complete) return;
        if (carrying) FadingHud.SetObjective("Tonight:  bring the memories to Mom's bed or Luna's bed");
        else FadingHud.SetObjective("Tonight:  gather the memory lights  " + found + "/" + (found + lights.Count) + "     [H] your candle shows the way");
    }

    // For the candle hint (H): the next light, or the sleeping family.
    public static bool TryHint(Vector3 from, out Transform target, out string text)
    {
        target = null;
        text = "";
        if (!Running || Complete) return false;
        NightQuest q = Instance;
        if (q.carrying)
        {
            target = q.mom != null ? q.mom.transform : (q.baby != null ? q.baby.transform : null);
            text = "Bring the memories to her bed.";
            return target != null;
        }
        float best = float.MaxValue;
        foreach (Transform l in q.lights)
        {
            if (l == null) continue;
            float d = (l.position - from).sqrMagnitude;
            if (d < best) { best = d; target = l; }
        }
        text = "A memory glows over there.";
        return target != null;
    }

    static float Flat(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void OnDestroy() { if (Instance == this) Instance = null; }
}
