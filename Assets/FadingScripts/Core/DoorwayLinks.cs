using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Goes in: nowhere (ChapterRules calls DoorwayLinks.Connect() when a chapter starts).
// The walkable floor (NavMesh) in the chapter scenes was baked with the doors CLOSED, so the door panels cut the rooms apart:
// Mom could get into a room but never out again (she stayed in her bedroom).
// This adds a walkable link through every doorway of the house (Structure/Doors/Doorway_*) while the game runs,
// so the rooms are connected again. No scene changes and no re-baking needed.
public static class DoorwayLinks
{
    static readonly string[] Doorways =
    {
        "Doorway_Child", "Doorway_Guest", "Doorway_Mother", "Doorway_Kitchen", "Doorway_Living", "Doorway_LivingKitchen",
    };

    static readonly List<NavMeshLinkInstance> links = new List<NavMeshLinkInstance>();

    public static void Connect()
    {
        foreach (NavMeshLinkInstance l in links) NavMesh.RemoveLink(l);     // links from the previous chapter
        links.Clear();

        HashSet<int> agentTypes = new HashSet<int>();
        foreach (NavMeshAgent a in Object.FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            agentTypes.Add(a.agentTypeID);
        if (agentTypes.Count == 0) return;

        int made = 0;
        foreach (string name in Doorways)
        {
            Vector3 a, b;
            if (!FindEnds(name, out a, out b)) continue;
            foreach (int type in agentTypes)
            {
                NavMeshLinkData data = new NavMeshLinkData
                {
                    startPosition = a, endPosition = b, width = 0.7f, bidirectional = true,
                    costModifier = -1f, area = 0, agentTypeID = type,
                };
                NavMeshLinkInstance link = NavMesh.AddLink(data);
                if (link.valid) { links.Add(link); made++; }
            }
        }
        Debug.Log("[DOORS] Connected " + made + " doorway links for the family.");
    }

    // The two walkable points on either side of a doorway.
    static bool FindEnds(string doorway, out Vector3 a, out Vector3 b)
    {
        a = b = Vector3.zero;
        GameObject g = GameObject.Find(doorway);
        if (g == null) return false;

        // The frame only (not the door panel, which may be swung open): it is thin across the wall and wide along it.
        Bounds frame = new Bounds();
        bool first = true;
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>(true))
        {
            if (r.GetComponentInParent<DoorToggle>() != null) continue;
            if (first) { frame = r.bounds; first = false; }
            else frame.Encapsulate(r.bounds);
        }
        if (first) return false;

        Vector3 center = new Vector3(frame.center.x, frame.min.y, frame.center.z);
        Vector3 across = frame.size.x < frame.size.z ? Vector3.right : Vector3.forward;
        return TrySides(center, across, out a, out b) || TrySides(center, across == Vector3.right ? Vector3.forward : Vector3.right, out a, out b);
    }

    static bool TrySides(Vector3 center, Vector3 across, out Vector3 a, out Vector3 b)
    {
        a = b = Vector3.zero;
        return TrySide(center, across, out a) && TrySide(center, -across, out b);
    }

    static bool TrySide(Vector3 center, Vector3 direction, out Vector3 point)
    {
        point = Vector3.zero;
        NavMeshHit hit;
        if (!NavMesh.SamplePosition(center + direction * 0.9f, out hit, 0.6f, NavMesh.AllAreas)) return false;
        if (Vector3.Dot(hit.position - center, direction) < 0.25f) return false;      // must really be on that side of the wall
        point = hit.position;
        return true;
    }
}
