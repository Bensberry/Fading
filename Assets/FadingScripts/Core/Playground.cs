using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in every chapter).
// The back yard: a small playground behind the house (the swing set, a slide, a sandbox, a bench and a dim yard lamp).
// The swings sway by themselves in the night wind.
// It is made of simple shapes until real models are added: put a model (or prefab) in Assets/Resources/Outdoor/
// named "Playground" and it is placed here instead (origin on the ground, about 8 x 6 m).
// Also knows the OUTDOOR spots where NightQuest puts memory lights (Playground.OutdoorSpots).
public class Playground : MonoBehaviour
{
    // House-model points (x, z) of the yard. The house itself is x -16..0, z 0..14.5; the ground goes much further.
    public static readonly Vector2 PlaygroundSpot = new Vector2(-9f, 23f);
    static readonly Vector2[] YardSpots =
    {
        new Vector2(-3.5f, -6.5f), new Vector2(-13f, -6f),       // front yard
        new Vector2(2.5f, 7f), new Vector2(-19f, 7f),            // the two sides
        new Vector2(-4f, 19.5f), new Vector2(-13f, 19f),         // back yard
    };

    static readonly Color Wood = new Color(0.5f, 0.36f, 0.24f);
    static readonly Color Metal = new Color(0.32f, 0.35f, 0.38f);
    static readonly Color Red = new Color(0.62f, 0.2f, 0.16f);
    static readonly Color Sand = new Color(0.78f, 0.7f, 0.52f);

    readonly List<Transform> swings = new List<Transform>();

    void Start()
    {
        Vector3 ground;
        if (!GroundAt(PlaygroundSpot.x, PlaygroundSpot.y, out ground)) return;

        GameObject custom = Resources.Load<GameObject>("Outdoor/Playground");
        GameObject root = custom != null ? Instantiate(custom) : new GameObject("Playground");
        root.transform.position = ground;
        Transform house = HouseTransform();
        if (house != null) root.transform.rotation = house.rotation;
        if (custom == null) Build(root.transform);
        AddCreak(root);
    }

    // A slow creak from the swings (sound file 'swing_creak' in Resources/Audio, looping; nothing if it is missing).
    static void AddCreak(GameObject root)
    {
        AudioClip creak = GameAudio.Get("swing_creak");
        if (creak == null) return;
        AudioSource s = root.AddComponent<AudioSource>();
        s.clip = creak;
        s.loop = true;
        s.spatialBlend = 1f;
        s.minDistance = 2f;
        s.maxDistance = 18f;
        s.volume = 0.5f;
        s.Play();
    }

    // ---------- where things are
    public static Transform HouseTransform()
    {
        FadingInteractablesSetup setup = FindAnyObjectByType<FadingInteractablesSetup>();
        return setup != null ? setup.transform : null;
    }

    // A house-model point (x, z) on the ground outside (found with a ray from above).
    public static bool GroundAt(float x, float z, out Vector3 point)
    {
        Transform house = HouseTransform();
        Vector3 above = house != null ? house.TransformPoint(-x, 6f, z) : new Vector3(-x, 6f, z);
        RaycastHit hit;
        if (Physics.Raycast(above, Vector3.down, out hit, 20f, ~0, QueryTriggerInteraction.Ignore)) { point = hit.point; return true; }
        point = above;
        return false;
    }

    // Two random outdoor spots for tonight's memory lights (one of them at the playground).
    public static List<Vector3> OutdoorSpots(int count)
    {
        List<Vector3> spots = new List<Vector3>();
        Vector3 p;
        if (GroundAt(PlaygroundSpot.x + 1.5f, PlaygroundSpot.y - 1.5f, out p)) spots.Add(p);
        List<Vector2> pool = new List<Vector2>(YardSpots);
        while (spots.Count < count && pool.Count > 0)
        {
            Vector2 s = pool[Random.Range(0, pool.Count)];
            pool.Remove(s);
            if (GroundAt(s.x, s.y, out p)) spots.Add(p);
        }
        return spots;
    }

    // ---------- the placeholder playground (simple shapes)
    void Build(Transform root)
    {
        // swing set: two A-frames, a top bar, two swings
        for (int side = -1; side <= 1; side += 2)
            for (int leg = -1; leg <= 1; leg += 2)
                Part(root, PrimitiveType.Cylinder, new Vector3(side * 1.4f, 1.1f, leg * 0.45f), new Vector3(0.08f, 1.15f, 0.08f), Metal, new Vector3(leg * 12f, 0f, 0f));
        Part(root, PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 0f), new Vector3(0.09f, 1.45f, 0.09f), Metal, new Vector3(0f, 0f, 90f));
        for (int s = -1; s <= 1; s += 2)
        {
            Transform pivot = new GameObject("SwingPivot").transform;
            pivot.SetParent(root, false);
            pivot.localPosition = new Vector3(s * 0.6f, 2.2f, 0f);
            for (int chain = -1; chain <= 1; chain += 2)
                Part(pivot, PrimitiveType.Cylinder, new Vector3(chain * 0.2f, -0.8f, 0f), new Vector3(0.02f, 0.8f, 0.02f), Metal, Vector3.zero);
            Part(pivot, PrimitiveType.Cube, new Vector3(0f, -1.6f, 0f), new Vector3(0.5f, 0.05f, 0.22f), Red, Vector3.zero);
            swings.Add(pivot);
        }

        // slide: ladder, platform, ramp
        Vector3 slide = new Vector3(3.6f, 0f, 0.5f);
        for (int x = -1; x <= 1; x += 2)
            Part(root, PrimitiveType.Cube, slide + new Vector3(x * 0.3f, 0.75f, -0.6f), new Vector3(0.06f, 1.5f, 0.06f), Metal, Vector3.zero);
        for (int r = 0; r < 4; r++)
            Part(root, PrimitiveType.Cube, slide + new Vector3(0f, 0.3f + r * 0.35f, -0.6f), new Vector3(0.6f, 0.04f, 0.04f), Metal, Vector3.zero);
        Part(root, PrimitiveType.Cube, slide + new Vector3(0f, 1.5f, -0.35f), new Vector3(0.7f, 0.06f, 0.6f), Wood, Vector3.zero);
        Part(root, PrimitiveType.Cube, slide + new Vector3(0f, 0.78f, 0.75f), new Vector3(0.55f, 0.05f, 1.9f), Red, new Vector3(-38f, 0f, 0f));

        // sandbox and bench
        Vector3 box = new Vector3(-3.4f, 0f, 1.2f);
        for (int s = -1; s <= 1; s += 2)
        {
            Part(root, PrimitiveType.Cube, box + new Vector3(s * 0.9f, 0.1f, 0f), new Vector3(0.1f, 0.2f, 1.9f), Wood, Vector3.zero);
            Part(root, PrimitiveType.Cube, box + new Vector3(0f, 0.1f, s * 0.9f), new Vector3(1.9f, 0.2f, 0.1f), Wood, Vector3.zero);
        }
        Part(root, PrimitiveType.Cube, box + new Vector3(0f, 0.08f, 0f), new Vector3(1.7f, 0.06f, 1.7f), Sand, Vector3.zero);
        Vector3 bench = new Vector3(0f, 0f, -2.6f);
        Part(root, PrimitiveType.Cube, bench + new Vector3(0f, 0.45f, 0f), new Vector3(1.6f, 0.06f, 0.4f), Wood, Vector3.zero);
        for (int s = -1; s <= 1; s += 2) Part(root, PrimitiveType.Cube, bench + new Vector3(s * 0.65f, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.35f), Metal, Vector3.zero);

        // a dim yard lamp
        Vector3 lamp = new Vector3(-1.8f, 0f, -2.8f);
        Part(root, PrimitiveType.Cylinder, lamp + new Vector3(0f, 1.4f, 0f), new Vector3(0.07f, 1.4f, 0.07f), Metal, Vector3.zero);
        GameObject bulb = Part(root, PrimitiveType.Sphere, lamp + new Vector3(0f, 2.85f, 0f), Vector3.one * 0.22f, new Color(1f, 0.85f, 0.6f), Vector3.zero);
        Renderer br = bulb.GetComponent<Renderer>();
        br.material.EnableKeyword("_EMISSION");
        br.material.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.5f) * 2f);
        Light l = bulb.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.8f, 0.55f);
        l.range = 9f;
        l.intensity = 1.2f;
        l.shadows = LightShadows.None;
    }

    static GameObject Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Vector3 euler)
    {
        GameObject g = FadingMaterials.Primitive(type);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        g.transform.localEulerAngles = euler;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().material.color = color;
        return g;
    }

    // The swings move a little by themselves (wind... or someone).
    void Update()
    {
        for (int i = 0; i < swings.Count; i++)
        {
            if (swings[i] == null) continue;
            float angle = Mathf.Sin(Time.time * 1.1f + i * 1.9f) * (6f + 4f * Mathf.Sin(Time.time * 0.13f + i));
            swings[i].localRotation = Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
