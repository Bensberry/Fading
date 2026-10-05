using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in every chapter).
// Dresses the whole YARD around the house when a chapter starts (no scene edits). The models are in Assets/Resources/:
//   Outdoor/  SwingSet, ParkBench, Fountain, Gazebo, Trampoline      (Poly by Google / Ray Larson, CC-BY)
//   Nature/   trees, bushes, flowers, fences, rocks, stones            (Kenney Nature Kit, CC0)
//   Props/    Mailbox, BirdHouse, WateringCan                          (Household Props pack)
// Every model is scaled to a real-world size here and put on the ground. The same layout appears in every chapter.
//   Back yard: the playground (swing set, slide, sandbox, trampoline), a gazebo, a bench, a fence around it all
//   Front yard: a fountain with a bench, flower beds, the mailbox. Trees and bushes all around, a stone path to the back.
// The ghost can SIT on the benches, SWING on the swing set and go down the SLIDE (RestSpot).
// Also knows the OUTDOOR hiding spots of the night's memory lights (Playground.OutdoorSpots).
// Positions are house-model points (x, z): the house is x -16..0, z 0..14.5, the front door is at x -8.1, z 0.
public class Playground : MonoBehaviour
{
    public static readonly Vector2 PlaygroundSpot = new Vector2(-9f, 23f);

    // Hard-to-find places for memory lights outside (behind things, under trees, in corners).
    static readonly Vector2[] HidingSpots =
    {
        new Vector2(-15.8f, 29.4f), new Vector2(-12.6f, -7.6f), new Vector2(-1.2f, 25.5f), new Vector2(3.2f, 12f),
        new Vector2(-19f, 15f), new Vector2(-6.3f, 25.8f), new Vector2(-9.6f, -9.3f), new Vector2(1.8f, -4.5f),
        new Vector2(-14.2f, 27.6f), new Vector2(-18.5f, 2f),
    };

    static readonly Color Wood = new Color(0.5f, 0.36f, 0.24f);
    static readonly Color Metal = new Color(0.32f, 0.35f, 0.38f);
    static readonly Color Red = new Color(0.62f, 0.2f, 0.16f);
    static readonly Color Sand = new Color(0.78f, 0.7f, 0.52f);

    Transform house;
    Transform yard;
    System.Random random;

    void Start()
    {
        house = HouseTransform();
        Vector3 test;
        if (!GroundAt(PlaygroundSpot.x, PlaygroundSpot.y, out test)) return;          // no ground: nothing to dress
        yard = new GameObject("Yard").transform;
        random = new System.Random(42);                                              // the same yard every chapter

        BuildPlayground();
        BuildGazebo();
        BuildFrontYard();
        BuildTrees();
        BuildForestEdge();
        BuildBushesAndFlowers();
        BuildFence();
        BuildPath();
        AddCreak();
    }

    // ---------- the back yard playground
    void BuildPlayground()
    {
        // Swing set (sit on it and swing)
        GameObject swings = Place("Outdoor/SwingSet", -11.5f, 23f, 0f, 2.3f, false, true);
        if (swings != null) AddRest(swings, RestSpot.Kind.Swing, ToHouseDir(0f, -1f), 0.45f);

        // Slide (simple shapes; ride it down)
        BuildSlide(-6.2f, 23.5f);

        // Sandbox with a forgotten bucket, and the trampoline
        BuildSandbox(-8.6f, 26.4f);
        Place("Outdoor/Trampoline/Trampoline", -3.2f, 20.6f, 0f, 2.6f, true, true, true);

        // Bench facing the playground (sit and watch)
        GameObject bench = Place("Outdoor/ParkBench", -9f, 19.3f, 0f, 1.6f, true, true, true);
        if (bench != null) AddRest(bench, RestSpot.Kind.Sit, ToHouseDir(0f, 1f), 0.5f);

        Place("Nature/stump_round", -17f, 22f, 0f, 0.5f, false, true);
        Place("Props/BirdHouse", -17f, 22f, 30f, 0.45f, false, false, false, 0.5f);
        Place("Props/WateringCan", -4.6f, 16.2f, 70f, 0.3f, false, false);
        YardLamp(-7.2f, 19f);
    }

    void BuildGazebo()
    {
        Place("Outdoor/Gazebo", -15.5f, 27.5f, 20f, 3.2f, false, false);
        GameObject bench = Place("Outdoor/ParkBench", -15.5f, 27.5f, 20f, 1.4f, true, true, true);
        if (bench != null) AddRest(bench, RestSpot.Kind.Sit, ToHouseDir(0.5f, -1f), 0.5f);
        YardLamp(-13.2f, 25.6f);
    }

    void BuildFrontYard()
    {
        GameObject fountain = Place("Outdoor/Fountain", -13.5f, -6.5f, 0f, 1.7f, false, true);
        GameObject bench = Place("Outdoor/ParkBench", -13.5f, -3.6f, 0f, 1.6f, true, true, true);
        if (bench != null) AddRest(bench, RestSpot.Kind.Sit, ToHouseDir(0f, -1f), 0.5f);
        Place("Props/Mailbox", -9.6f, -9.6f, 90f, 1.1f, false, true);
        YardLamp(-11.2f, -4.2f);
        if (fountain != null) FlowerRing(-13.5f, -6.5f, 2.1f, 10);
    }

    // ---------- nature
    static readonly string[] Trees = { "tree_oak", "tree_default", "tree_detailed", "tree_fat", "tree_tall", "tree_oak_dark", "tree_pineRoundB" };

    void BuildTrees()
    {
        for (float x = -22f; x <= 6f; x += 3.6f) Tree(x, 33f);                          // behind the back fence
        for (float z = -12f; z <= 30f; z += 4.2f) { Tree(-23f, z); Tree(7f, z); }      // both sides
        for (float x = -22f; x <= -11f; x += 4f) Tree(x, -15f);                         // front, left of the path
        Tree(4.5f, -15f);
        Tree(-19.5f, 24f); Tree(1.5f, 28f); Tree(-1f, 17.5f);                            // a few in the yard itself
        for (int i = 0; i < 10; i++) Place("Nature/rock_smallA", Rand(-22f, 6f), Rand(16f, 31f), Rand(0f, 360f), Rand(0.2f, 0.4f), false, false);
        for (int i = 0; i < 6; i++) Place("Nature/mushroom_redGroup", Rand(-22f, 6f), Rand(28f, 32f), Rand(0f, 360f), 0.2f, false, false);
        Place("Nature/log", -20.5f, 18f, 35f, 1.8f, true, true);
        Place("Nature/rock_largeB", 4f, 20f, 60f, 1.2f, true, true);
    }

    // ---------- the forest around everything
    // The ground of the house model ends at x -40..24, z -28..36. Between the play area and that edge a thick wood
    // hides the end of the world, and invisible walls keep the player inside the play area.
    static readonly Rect PlayArea = Rect.MinMaxRect(-25f, -19f, 9f, 34f);              // house-model x / z
    static readonly string[] ForestTrees = { "tree_pineTallA", "tree_pineRoundB", "tree_tall", "tree_detailed", "tree_oak_dark", "tree_fat" };

    void BuildForestEdge()
    {
        const float step = 2.8f;
        for (float x = -40f; x <= 24f; x += step)
            for (float z = -28f; z <= 36f; z += step)
            {
                if (x > PlayArea.xMin - 0.5f && x < PlayArea.xMax + 0.5f && z > PlayArea.yMin - 0.5f && z < PlayArea.yMax + 0.5f) continue;
                string kind = ForestTrees[random.Next(ForestTrees.Length)];
                GameObject t = Place("Nature/" + kind, x + Rand(-1f, 1f), z + Rand(-1f, 1f), Rand(0f, 360f), Rand(6f, 9.5f), false, false);
                if (t != null) foreach (Renderer r in t.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

        // invisible walls on the four sides of the play area
        Wall(PlayArea.xMin, PlayArea.yMin, PlayArea.xMax, PlayArea.yMin);
        Wall(PlayArea.xMin, PlayArea.yMax, PlayArea.xMax, PlayArea.yMax);
        Wall(PlayArea.xMin, PlayArea.yMin, PlayArea.xMin, PlayArea.yMax);
        Wall(PlayArea.xMax, PlayArea.yMin, PlayArea.xMax, PlayArea.yMax);
    }

    void Wall(float x1, float z1, float x2, float z2)
    {
        Vector3 a = house != null ? house.TransformPoint(-x1, 0f, z1) : new Vector3(-x1, 0f, z1);
        Vector3 b = house != null ? house.TransformPoint(-x2, 0f, z2) : new Vector3(-x2, 0f, z2);
        GameObject w = new GameObject("YardEdge");
        w.transform.SetParent(yard, false);
        w.transform.position = (a + b) / 2f + Vector3.up * 1.5f;
        Vector3 along = b - a;
        w.transform.rotation = Quaternion.LookRotation(along.normalized, Vector3.up);
        w.AddComponent<BoxCollider>().size = new Vector3(0.5f, 6f, along.magnitude + 1f);
    }

    void Tree(float x, float z)
    {
        string kind = Trees[random.Next(Trees.Length)];
        Place("Nature/" + kind, x + Rand(-0.8f, 0.8f), z + Rand(-0.8f, 0.8f), Rand(0f, 360f), Rand(4.5f, 7f), false, true, false, 0f, 0.35f);
    }

    void BuildBushesAndFlowers()
    {
        string[] bushes = { "plant_bush", "plant_bushLarge", "plant_bushDetailed", "plant_bushSmall" };
        foreach (float x in new[] { -15.2f, -13.4f, -11.6f, -4.6f, -2.8f, -1f })           // along the front wall
            Place("Nature/" + bushes[random.Next(bushes.Length)], x, -0.9f, Rand(0f, 360f), Rand(0.7f, 1.1f), false, true);
        for (float x = -15f; x <= -1f; x += 2.3f)                                        // along the back wall
            Place("Nature/" + bushes[random.Next(bushes.Length)], x, 15.4f, Rand(0f, 360f), Rand(0.7f, 1.2f), false, true);
        for (float z = 1f; z <= 13f; z += 2.6f)                                          // along the west wall
            Place("Nature/" + bushes[random.Next(bushes.Length)], -16.9f, z, Rand(0f, 360f), Rand(0.6f, 1f), false, true);
        FlowerBed(-14f, -1.8f, 3f, 0.8f, 12);
        FlowerBed(-2.8f, -1.8f, 3f, 0.8f, 12);
        FlowerBed(-5f, 16.4f, 2.5f, 0.8f, 10);
        for (int i = 0; i < 40; i++) Place("Nature/grass", Rand(-21f, 5f), Rand(-12f, 31f), Rand(0f, 360f), Rand(0.25f, 0.45f), false, false);
    }

    void FlowerBed(float x, float z, float width, float depth, int count)
    {
        string[] flowers = { "flower_redA", "flower_yellowB", "flower_purpleA", "flower_redC", "flower_yellowA" };
        for (int i = 0; i < count; i++)
            Place("Nature/" + flowers[random.Next(flowers.Length)], x + Rand(-width / 2f, width / 2f), z + Rand(-depth / 2f, depth / 2f), Rand(0f, 360f), Rand(0.3f, 0.45f), false, false);
    }

    void FlowerRing(float x, float z, float radius, int count)
    {
        string[] flowers = { "flower_redA", "flower_yellowB", "flower_purpleA" };
        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2f / count;
            Place("Nature/" + flowers[i % flowers.Length], x + Mathf.Cos(a) * radius, z + Mathf.Sin(a) * radius, Rand(0f, 360f), 0.4f, false, false);
        }
    }

    void BuildFence()
    {
        for (float x = -21f; x < 5f; x += 2f) Place("Nature/fence_simple", x + 1f, 31f, 0f, 2f, true, true);         // back
        for (float z = 15.5f; z < 31f; z += 2f)
        {
            Place("Nature/fence_simple", -21f, z + 1f, 90f, 2f, true, true);                                        // west side
            Place("Nature/fence_simple", 5f, z + 1f, 90f, 2f, true, true);                                          // east side
        }
    }

    void BuildPath()
    {
        for (float z = -1.5f; z <= 18f; z += 1.3f) Place("Nature/path_stone", 1.6f, z, Rand(-15f, 15f), 0.9f, true, false);   // round the east side
        for (float x = 1.6f; x >= -8f; x -= 1.3f) Place("Nature/path_stone", x, 18.6f, 90f + Rand(-15f, 15f), 0.9f, true, false);  // to the playground
    }

    // ---------- simple shapes (no models for these)
    void BuildSlide(float x, float z)
    {
        Vector3 g;
        if (!GroundAt(x, z, out g)) return;
        Transform root = new GameObject("Slide").transform;
        root.SetParent(yard, false);
        root.position = g;
        root.rotation = HouseRotation();
        for (int s = -1; s <= 1; s += 2) Part(root, PrimitiveType.Cube, new Vector3(s * 0.3f, 0.75f, -0.6f), new Vector3(0.06f, 1.5f, 0.06f), Metal, Vector3.zero);
        for (int r = 0; r < 4; r++) Part(root, PrimitiveType.Cube, new Vector3(0f, 0.3f + r * 0.35f, -0.6f), new Vector3(0.6f, 0.04f, 0.04f), Metal, Vector3.zero);
        Part(root, PrimitiveType.Cube, new Vector3(0f, 1.5f, -0.35f), new Vector3(0.7f, 0.06f, 0.6f), Wood, Vector3.zero);
        // The ramp: from the platform edge (z -0.05, 1.5 m high) down to the ground (z 1.75).
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.82f, 0.85f), new Vector3(0.55f, 0.05f, 2.25f), Red, new Vector3(37f, 0f, 0f));
        for (int s = -1; s <= 1; s += 2)                                                       // side rails
            Part(root, PrimitiveType.Cube, new Vector3(s * 0.29f, 0.92f, 0.85f), new Vector3(0.04f, 0.14f, 2.25f), Red, new Vector3(37f, 0f, 0f));
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.1f, 1.85f), new Vector3(0.55f, 0.05f, 0.35f), Red, Vector3.zero);   // the run-out at the bottom

        RestSpot ride = root.gameObject.AddComponent<RestSpot>();
        ride.kind = RestSpot.Kind.Slide;
        ride.seatPoint = root.TransformPoint(new Vector3(0f, 1.55f, -0.2f));
        ride.endPoint = root.TransformPoint(new Vector3(0f, 0.15f, 1.85f));
        ride.exitPoint = root.TransformPoint(new Vector3(0f, 0.05f, 2.6f));
        ride.facing = root.forward;
        ride.ApplyDefaults();
        MakeTouchable(root.gameObject);
    }

    void BuildSandbox(float x, float z)
    {
        Vector3 g;
        if (!GroundAt(x, z, out g)) return;
        Transform root = new GameObject("Sandbox").transform;
        root.SetParent(yard, false);
        root.position = g;
        root.rotation = HouseRotation();
        for (int s = -1; s <= 1; s += 2)
        {
            Part(root, PrimitiveType.Cube, new Vector3(s * 0.9f, 0.1f, 0f), new Vector3(0.1f, 0.2f, 1.9f), Wood, Vector3.zero);
            Part(root, PrimitiveType.Cube, new Vector3(0f, 0.1f, s * 0.9f), new Vector3(1.9f, 0.2f, 0.1f), Wood, Vector3.zero);
        }
        Part(root, PrimitiveType.Cube, new Vector3(0f, 0.08f, 0f), new Vector3(1.7f, 0.06f, 1.7f), Sand, Vector3.zero);
        Part(root, PrimitiveType.Cylinder, new Vector3(0.4f, 0.2f, 0.3f), new Vector3(0.18f, 0.1f, 0.18f), Red, new Vector3(0f, 0f, 20f));   // a little bucket
    }

    void YardLamp(float x, float z)
    {
        Vector3 g;
        if (!GroundAt(x, z, out g)) return;
        Transform root = new GameObject("YardLamp").transform;
        root.SetParent(yard, false);
        root.position = g;
        Part(root, PrimitiveType.Cylinder, new Vector3(0f, 1.4f, 0f), new Vector3(0.07f, 1.4f, 0.07f), Metal, Vector3.zero);
        GameObject bulb = Part(root, PrimitiveType.Sphere, new Vector3(0f, 2.85f, 0f), Vector3.one * 0.22f, new Color(1f, 0.85f, 0.6f), Vector3.zero);
        Destroy(bulb.GetComponent<Collider>());
        Renderer br = bulb.GetComponent<Renderer>();
        br.material.EnableKeyword("_EMISSION");
        br.material.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.5f) * 2f);
        Light l = bulb.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.8f, 0.55f);
        l.range = 8f;
        l.intensity = 1.1f;
        l.shadows = LightShadows.None;
    }

    // A slow creak from the swings (sound file 'swing_creak', looping; nothing if it is missing).
    void AddCreak()
    {
        AudioClip creak = GameAudio.Get("swing_creak");
        Vector3 at;
        if (creak == null || !GroundAt(-11.5f, 23f, out at)) return;
        GameObject g = new GameObject("SwingCreak");
        g.transform.SetParent(yard, false);
        g.transform.position = at + Vector3.up * 1.5f;
        AudioSource s = g.AddComponent<AudioSource>();
        s.clip = creak;
        s.loop = true;
        s.spatialBlend = 1f;
        s.minDistance = 2f;
        s.maxDistance = 18f;
        s.volume = 0.5f;
        s.Play();
    }

    // ---------- placing models
    // Put the model Resources/<path> at house point (x, z) on the ground, turned 'yaw' degrees, scaled so its height
    // (or its longest side when byLongestSide) is 'size' metres. 'solid' adds a simple box collider.
    // 'raise' lifts it (e.g. a bird house on a stump). 'trunkOnly' makes the collider only this fraction of the width (trees).
    GameObject Place(string path, float x, float z, float yaw, float size, bool byLongestSide, bool solid,
                     bool alongGround = false, float raise = 0f, float trunkOnly = 1f)
    {
        GameObject prefab = Resources.Load<GameObject>(path);
        Vector3 ground;
        if (prefab == null || !GroundAt(x, z, out ground)) return null;

        GameObject g = Instantiate(prefab, yard);
        g.name = System.IO.Path.GetFileName(path);
        g.transform.rotation = HouseRotation() * Quaternion.Euler(0f, yaw, 0f);
        Bounds b = BoundsOf(g);
        float current = byLongestSide ? Mathf.Max(b.size.x, b.size.z) : b.size.y;
        if (current > 0.0001f) g.transform.localScale *= size / current;
        b = BoundsOf(g);
        g.transform.position += new Vector3(ground.x - b.center.x, ground.y + raise - b.min.y, ground.z - b.center.z);

        foreach (Collider c in g.GetComponentsInChildren<Collider>()) Destroy(c);
        if (solid)
        {
            b = BoundsOf(g);
            BoxCollider box = g.AddComponent<BoxCollider>();
            box.center = g.transform.InverseTransformPoint(b.center);
            Vector3 s = g.transform.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(s.x) * trunkOnly, Mathf.Abs(s.y), Mathf.Abs(s.z) * trunkOnly);
        }
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
            if (size < 1f) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;     // small things: no shadows (cheaper)
        return g;
    }

    // Make a bench / swing set a place to rest. 'seatHeight' = the seat as a fraction of the model's height.
    void AddRest(GameObject g, RestSpot.Kind kind, Vector3 facing, float seatHeight)
    {
        TurnSideways(g, facing);
        Bounds b = BoundsOf(g);
        RestSpot rest = g.AddComponent<RestSpot>();
        rest.kind = kind;
        rest.facing = facing;
        rest.seatPoint = new Vector3(b.center.x, b.min.y + b.size.y * seatHeight * (kind == RestSpot.Kind.Swing ? 0.25f : 1f), b.center.z);
        rest.exitPoint = new Vector3(b.center.x, b.min.y, b.center.z) + facing.normalized * 1.1f;
        rest.ApplyDefaults();
        MakeTouchable(g);
    }

    // A bench (or swing set) is long along the seat: turn it so that long side runs across the way the ghost faces.
    static void TurnSideways(GameObject g, Vector3 facing)
    {
        Bounds b = BoundsOf(g);
        Vector3 before = b.center;
        bool facingAlongX = Mathf.Abs(facing.x) > Mathf.Abs(facing.z);
        bool longAlongX = b.size.x > b.size.z;
        if (facingAlongX != longAlongX) return;                                       // already sideways
        g.transform.Rotate(0f, 90f, 0f, Space.World);
        b = BoundsOf(g);
        g.transform.position += new Vector3(before.x - b.center.x, 0f, before.z - b.center.z);
    }

    // The player's F only looks at the "Interactable" layer.
    static void MakeTouchable(GameObject g)
    {
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer < 0) return;
        foreach (Transform t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    Vector3 ToHouseDir(float x, float z)
    {
        Vector3 d = new Vector3(-x, 0f, z);                                               // house-model directions are mirrored in x
        return house != null ? house.TransformDirection(d).normalized : d.normalized;
    }

    Quaternion HouseRotation() { return house != null ? house.rotation : Quaternion.identity; }

    float Rand(float a, float b) { return a + (float)random.NextDouble() * (b - a); }

    static Bounds BoundsOf(GameObject g)
    {
        Renderer[] rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
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

    // ---------- where things are (used by NightQuest)
    public static Transform HouseTransform()
    {
        FadingInteractablesSetup setup = FindAnyObjectByType<FadingInteractablesSetup>();
        return setup != null ? setup.transform : null;
    }

    // A house-model point (x, z) on the ground outside (found with a ray from above; the yard's own models are ignored).
    public static bool GroundAt(float x, float z, out Vector3 point)
    {
        Transform house = HouseTransform();
        Vector3 above = house != null ? house.TransformPoint(-x, 8f, z) : new Vector3(-x, 8f, z);
        foreach (RaycastHit hit in SortedHits(above))
        {
            if (hit.transform.GetComponentInParent<Playground>() != null || hit.transform.name == "Yard" || IsInYard(hit.transform)) continue;
            point = hit.point;
            return true;
        }
        point = above;
        return false;
    }

    static bool IsInYard(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent) if (p.name == "Yard") return true;
        return false;
    }

    static RaycastHit[] SortedHits(Vector3 from)
    {
        RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, 30f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        return hits;
    }

    // Tonight's outdoor memory lights: random hiding spots (some low, behind things).
    public static List<Vector3> OutdoorSpots(int count)
    {
        List<Vector3> spots = new List<Vector3>();
        List<Vector2> pool = new List<Vector2>(HidingSpots);
        while (spots.Count < count && pool.Count > 0)
        {
            Vector2 s = pool[Random.Range(0, pool.Count)];
            pool.Remove(s);
            Vector3 p;
            if (GroundAt(s.x, s.y, out p)) spots.Add(p);
        }
        return spots;
    }
}
