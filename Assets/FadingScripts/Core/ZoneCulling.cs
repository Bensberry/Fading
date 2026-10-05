using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in Chapter 1-3). Works only while OLD PHONE MODE is on (Settings).
// "Render distance" by zone, like in Minecraft, so phones have much less to draw:
//   inside the house  -> the yard's things and the night sky (stars, moon) are not drawn (the big trees and the lamps stay)
//   out in the yard   -> the furniture and things inside the house are not drawn (walls, roof, windows, doors stay)
//   at the door       -> everything is drawn, so nothing pops while you walk through
// Always drawn: Mom, Luna, the memory lights. Out in the yard the starry sky is drawn as usual. During cutscenes everything is drawn.
// It only switches the drawing off (Renderer.forceRenderingOff); objects, colliders and scripts keep working.
public class ZoneCulling : MonoBehaviour
{
    const float InsideMargin = 0.8f;          // this far inside the outer walls counts as "inside"
    const float OutsideMargin = 2.5f;         // this far outside the walls counts as "outside"
    const float RefreshSeconds = 4f;          // new things (boxes, props) are picked up this often

    enum Zone { Both, Inside, Outside }

    Transform house, yard;
    Bounds footprint;
    Zone zone = Zone.Both;
    float nextRefresh;
    readonly HashSet<Renderer> hidden = new HashSet<Renderer>();
    readonly Dictionary<Renderer, bool> alwaysShown = new Dictionary<Renderer, bool>();     // worked out once per object

    void Start()
    {
        house = Playground.HouseTransform();
        Transform floors = house != null ? house.Find("Structure/Floors") : null;
        if (floors == null || !BoundsOf(floors, out footprint)) { enabled = false; return; }
    }

    void Update()
    {
        if (yard == null) { GameObject y = GameObject.Find("Yard"); if (y != null) yard = y.transform; }
        Camera cam = Camera.main;
        Zone now = (cam == null || CutsceneRunner.IsPlaying || !MobilePerformance.OldPhoneMode) ? Zone.Both : ZoneOf(cam.transform.position);
        if (now == zone && Time.time < nextRefresh) return;
        zone = now;
        nextRefresh = Time.time + RefreshSeconds;
        Apply();
    }

    Zone ZoneOf(Vector3 p)
    {
        bool inX = p.x > footprint.min.x + InsideMargin && p.x < footprint.max.x - InsideMargin;
        bool inZ = p.z > footprint.min.z + InsideMargin && p.z < footprint.max.z - InsideMargin;
        if (inX && inZ && p.y < footprint.max.y + 4f) return Zone.Inside;
        bool outX = p.x < footprint.min.x - OutsideMargin || p.x > footprint.max.x + OutsideMargin;
        bool outZ = p.z < footprint.min.z - OutsideMargin || p.z > footprint.max.z + OutsideMargin;
        if (outX || outZ) return Zone.Outside;
        return Zone.Both;                                                // in the doorway / on the porch
    }

    void Apply()
    {
        HashSet<Renderer> hide = new HashSet<Renderer>();
        if (zone == Zone.Inside && yard != null)
            foreach (Renderer r in yard.GetComponentsInChildren<Renderer>(true))
                if (!Stays(r, true)) hide.Add(r);
        NightSky sky = zone == Zone.Inside ? FindAnyObjectByType<NightSky>() : null;
        if (sky != null) foreach (Renderer r in sky.GetComponentsInChildren<Renderer>(true)) hide.Add(r);
        if (zone == Zone.Outside && house != null)
            foreach (Renderer r in house.GetComponentsInChildren<Renderer>(true))
                if (!Stays(r, false)) hide.Add(r);

        foreach (Renderer r in hidden) if (r != null && !hide.Contains(r)) r.forceRenderingOff = false;
        foreach (Renderer r in hide) r.forceRenderingOff = true;
        hidden.Clear();
        hidden.UnionWith(hide);
    }

    bool Stays(Renderer r, bool inYard)
    {
        bool stays;
        if (!alwaysShown.TryGetValue(r, out stays))
        {
            stays = inYard ? StaysInYard(r.transform) : StaysInHouse(r.transform);
            alwaysShown[r] = stays;
        }
        return stays;
    }

    // Seen through the windows: the trees and the yard lamps.
    bool StaysInYard(Transform t)
    {
        for (; t != null && t != yard; t = t.parent)
            if (t.name.StartsWith("tree_") || t.name == "YardLamp") return true;
        return false;
    }

    // Seen from outside: the walls, roof, windows and doors (Structure), the porch, ground, van (Exterior), and the family.
    bool StaysInHouse(Transform t)
    {
        for (; t != null && t != house; t = t.parent)
        {
            if (t.parent == house && (t.name == "Structure" || t.name == "Exterior")) return true;
            if (t.GetComponent<GrandmaAI>() != null || t.GetComponent<BabyAI>() != null) return true;
        }
        return false;
    }

    static bool BoundsOf(Transform root, out Bounds b)
    {
        b = new Bounds();
        Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return false;
        b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return true;
    }

    void OnDestroy()
    {
        foreach (Renderer r in hidden) if (r != null) r.forceRenderingOff = false;
    }
}
