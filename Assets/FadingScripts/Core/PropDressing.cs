using UnityEngine;

// Goes in: nowhere (ChapterRules calls PropDressing.Run() when a chapter starts, BEFORE the signs are counted).
// Puts more things in the house (models from Assets/Resources/Props/, the Household Props pack) and makes 12 of them
// new touchable CLUES (each has its own small script in FadingScripts/Objects, like every other sign):
//   Mom notices:   cup of tea, flowers, globe, alarm clock, the ceiling fans (living room, her room), the anniversary glass
//   Luna feels:    her doll, rubber duck, toy airplane, piggy bank, a wrapped present, the ceiling fan in her room
// plus a few things that are only decoration (books, a candlestick, a plant, a telescope, a trophy, a packing box).
// Positions are house-model points (x, height of the furniture top, z), measured from FadingHouse.glb.
// Each clue can have its own sound in Assets/Resources/Audio/ (the last word of each line below; see the audio list).
// Several of them get packed away in later chapters (HouseEmptying).
public static class PropDressing
{
    public static void Run()
    {
        Transform house = Playground.HouseTransform();
        if (house == null || GameObject.Find("INT_Prop_CupOfTea") != null) return;           // no house, or already done

        // ---- Mom's clues (kitchen, her room, living room)
        Clue<PropCupOfTea>("CupOfTea", "INT_Prop_CupOfTea", "Kitchen", -11.75f, 0.76f, 8.15f, 0.13f, true, 0f, "tea_clink");
        Clue<PropFlowers>("Flowers", "INT_Prop_Flowers", "Kitchen", -13.0f, 0.92f, 10.15f, 0.45f, false, 0f, "flowers_rustle");
        Clue<PropCeilingFan>("CeilingFan", "INT_Prop_CeilingFan_Mother", "MotherRoom", -12.15f, 2.7f, 12.0f, 1.1f, true, 0f, "ceiling_fan", true);
        Clue<PropGlobe>("Globe", "INT_Prop_Globe", "MotherRoom", -14.82f, 0.85f, 10.85f, 0.32f, false, 0f, "globe_spin");
        Clue<PropAlarmClock>("AlarmClock", "INT_Prop_AlarmClock", "MotherRoom", -10.68f, 0.55f, 14.2f, 0.18f, false, 180f, "clock_ring");
        Clue<PropCeilingFan>("CeilingFan", "INT_Prop_CeilingFan", "LivingRoom", -12.6f, 2.7f, 2.8f, 1.2f, true, 0f, "ceiling_fan", true);
        Clue<PropCeilingFan>("CeilingFan", "INT_Prop_CeilingFan_Child", "ChildRoom", -3.5f, 2.7f, 10.5f, 1.1f, true, 0f, "ceiling_fan", true);
        Clue<PropWineGlass>("WineGlass", "INT_Prop_WineGlass", "LivingRoom", -10.38f, 0.8f, 0.32f, 0.16f, false, 0f, "glass_ring");

        // ---- Luna's clues (her room)
        Clue<PropDoll>("Doll", "INT_Prop_Doll", "ChildRoom", -4.6f, 0.85f, 14.25f, 0.3f, true, 90f, "doll_thump");
        Clue<PropRubberDuck>("RubberDuck", "INT_Prop_RubberDuck", "ChildRoom", -1.78f, 0.55f, 14.2f, 0.12f, true, 200f, "duck_squeak");
        Clue<PropToyAirplane>("ToyAirplane", "INT_Prop_ToyAirplane", "ChildRoom", -0.3f, 0.49f, 10.72f, 0.4f, true, 30f, "airplane_whoosh");
        Clue<PropPiggyBank>("PiggyBank", "INT_Prop_PiggyBank", "ChildRoom", -1.63f, 1.2f, 6.72f, 0.2f, true, 180f, "piggy_rattle");
        Clue<PropPresent>("Present", "INT_Prop_Present", "ChildRoom", -3.6f, 0.01f, 10.0f, 0.25f, false, 15f, "present_shake");

        // ---- decoration only
        Decor("Books", "Prop_Books", -6.8f, 1.8f, 1.73f, 0.25f, true, 0f);
        Decor("Candlestick", "Prop_Candlestick", -6.85f, 0.82f, 2.68f, 0.28f, false, 0f);

        Decor("PottedPlant", "Prop_Plant", -7.45f, 0.01f, 13.9f, 0.5f, false, 0f);
        Decor("Telescope", "Prop_Telescope", -15.5f, 0.01f, 13.8f, 1.0f, false, 135f);
        Decor("Trophy", "Prop_Trophy", -4.15f, 0.85f, 14.25f, 0.25f, false, 180f);
        Decor("EmptyBox", "Prop_PackingBox", -10.6f, 0.01f, 2.9f, 0.4f, false, 20f);
    }

    // A touchable clue: root (script + collider) -> "<name>_Part" (what moves) -> the model.
    static void Clue<T>(string model, string objectName, string room, float x, float y, float z, float size, bool byLongestSide,
                        float yaw, string soundName, bool hangsFromCeiling = false) where T : Interactable
    {
        GameObject root = Build(model, objectName, room, x, y, z, size, byLongestSide, yaw, hangsFromCeiling);
        if (root == null) return;
        T sign = root.AddComponent<T>();                    // its Awake adds a collider around the model
        sign.ApplyDefaults();
        sign.sound = GameAudio.Get(soundName);
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0) foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    static void Decor(string model, string objectName, float x, float y, float z, float size, bool byLongestSide, float yaw)
    {
        GameObject g = Build(model, objectName, null, x, y, z, size, byLongestSide, yaw, false);
        if (g != null) foreach (Renderer r in g.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static GameObject Build(string model, string objectName, string room, float x, float y, float z, float size, bool byLongestSide,
                            float yaw, bool hangsFromCeiling)
    {
        GameObject prefab = Resources.Load<GameObject>("Props/" + model);
        Transform house = Playground.HouseTransform();
        if (prefab == null || house == null) return null;

        GameObject root = new GameObject(objectName);
        Transform parent = RoomParent(room);
        root.transform.SetParent(parent != null ? parent : house, true);
        root.transform.position = house.TransformPoint(-x, y, z);
        root.transform.rotation = house.rotation * Quaternion.Euler(0f, yaw, 0f);

        Transform part = new GameObject(objectName + "_Part").transform;
        part.SetParent(root.transform, false);
        GameObject m = Object.Instantiate(prefab, part);
        foreach (Collider c in m.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);   // now, so the clue gets its own box collider

        // Real-world size, then stand it on the furniture (or hang it from the ceiling), centred on the pivot.
        Bounds b = BoundsOf(m);
        float current = byLongestSide ? Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)) : b.size.y;
        if (current > 0.0001f) m.transform.localScale *= size / current;
        b = BoundsOf(m);
        Vector3 pivot = part.position;
        float targetY = hangsFromCeiling ? pivot.y - b.size.y : pivot.y;
        m.transform.position += new Vector3(pivot.x - b.center.x, targetY - b.min.y, pivot.z - b.center.z);
        return root;
    }

    // "Kitchen" -> the house's Kitchen_Interactables group (so the candle hint can say which room).
    static Transform RoomParent(string room)
    {
        if (string.IsNullOrEmpty(room)) return null;
        string group = room == "MotherRoom" ? "Mother_Interactables" : room == "ChildRoom" ? "Child_Interactables" :
                       room == "LivingRoom" ? "Living_Interactables" : room + "_Interactables";
        GameObject g = GameObject.Find(group);
        if (g == null) g = GameObject.Find(room + "_Interactables");
        return g != null ? g.transform : null;
    }

    static Bounds BoundsOf(GameObject g)
    {
        Renderer[] rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
