using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Goes in: nowhere. It runs by itself when a scene starts (no need to attach it).
// The imported house (FadingHouse.glb) has no colliders, so the player could walk through walls or fall.
// This adds them at runtime and does not change how anything looks:
//   - big solid parts (floors, walls, ceiling, door frames, windows, ground...) get a Mesh Collider
//   - furniture and packing boxes get a Mesh Collider too (their exact shape; a box would wrongly fill L-shaped
//     things like the kitchen backsplash and block the whole room)
// Anything that already has a collider (including things added by hand) is left alone,
// and so are the touchable INT_ objects (they make their own collider).
public static class HouseColliders
{
    // Names of the groups inside FadingHouse. Edit these lists if you add new groups.
    static readonly string[] SolidGroups =
        { "Floors", "Ceiling", "Walls_Exterior", "Walls_Interior", "Windows", "Doors", "Foundation", "Porch", "Ground", "Path" };
    static readonly string[] BoxGroups =
        { "Hallway_Furniture", "Guest_Furniture", "Child_Furniture", "Living_Furniture", "Kitchen_Furniture",
          "Mother_Furniture", "Grandma_Belongings", "Packing" };

    // Unity runs this start-up hook only ONCE (for the first scene), so we listen for every scene load instead.
    // That way it also works when the game is started from the main menu.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddColliders();
    }

    // Also run once for the very first scene (when you press Play directly in a chapter). Safe to run twice.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RunForFirstScene()
    {
        AddColliders();
    }

    static readonly HashSet<int> doneHouses = new HashSet<int>();

    static void AddColliders()
    {
        FadingInteractablesSetup setup = Object.FindFirstObjectByType<FadingInteractablesSetup>();
        Transform house = setup != null ? setup.transform : FindByName("FadingHouse");
        if (house == null) return;
        if (!doneHouses.Add(house.gameObject.GetInstanceID())) return;       // already done for this house

        int added = 0, repaired = 0;
        foreach (Transform t in house.GetComponentsInChildren<Transform>(true))
        {
            if (IsTouchable(t)) continue;
            MeshFilter filter = t.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null) continue;
            Collider existing = t.GetComponent<Collider>();

            if (IsInside(t, SolidGroups, house))
            {
                // Walls, floors, frames...: the collider must have exactly the visible shape (holes for doorways included).
                // A hand-added collider with no mesh, the wrong mesh or the wrong type does nothing / blocks openings, so fix it.
                if (existing == null) { AddMeshCollider(t.gameObject, filter.sharedMesh); added++; }
                else if (RepairMeshCollider(existing, filter.sharedMesh, t.gameObject)) repaired++;
            }
            else if (existing == null && IsInside(t, BoxGroups, house) && !IsFlat(t))
            {
                AddMeshCollider(t.gameObject, filter.sharedMesh);
                added++;
            }
        }
        Debug.Log("HouseColliders: added " + added + " colliders, repaired " + repaired + " existing wall/floor colliders.");
        ClearOpenDoorways(house);
    }

    // DIAGNOSTIC ONLY (it changes nothing): for each open doorway (the ones without a door, e.g. Living <-> Kitchen)
    // it prints to the Console which colliders sit inside the opening. Use it to find out what blocks the way.
    static void ClearOpenDoorways(Transform house)
    {
        foreach (Transform doorway in house.GetComponentsInChildren<Transform>(true))
        {
            if (!doorway.name.StartsWith("Doorway_") || HasDoorLeaf(doorway)) continue;
            MeshFilter frame = doorway.GetComponent<MeshFilter>();
            if (frame == null || frame.sharedMesh == null) continue;

            Bounds b = frame.sharedMesh.bounds;
            Vector3 centre = doorway.TransformPoint(new Vector3(b.center.x, 1.15f, b.center.z));
            Vector3 half = new Vector3(Mathf.Max(0.2f, b.extents.x - 0.2f), 0.95f, 0.35f);

            foreach (Collider c in Physics.OverlapBox(centre, half, doorway.rotation, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.transform == doorway || c.transform.IsChildOf(doorway)) continue;
                if (c.name.StartsWith("Floor")) continue;
                if (c is CharacterController || c.GetComponentInParent<FirstPersonController>() != null) continue;
                Debug.Log("HouseColliders (info): '" + c.name + "' (" + c.GetType().Name + ") is inside the open doorway '" + doorway.name + "'", c);
            }
        }
    }

    // True if the doorway has a door in it (an INT_Door_... child). Those are opened with F, not walked through.
    static bool HasDoorLeaf(Transform doorway)
    {
        foreach (Transform child in doorway.GetComponentsInChildren<Transform>(true))
            if (child != doorway && child.name.StartsWith("INT_Door")) return true;
        return false;
    }

    // Makes an existing collider on a wall/floor correct. Returns true if it had to change something.
    static bool RepairMeshCollider(Collider existing, Mesh mesh, GameObject g)
    {
        MeshCollider mc = existing as MeshCollider;
        if (mc == null)
        {
            existing.enabled = false;                    // e.g. a Box Collider across a doorway: replace it by the true shape
            AddMeshCollider(g, mesh);
            return true;
        }

        bool ok = mc.sharedMesh == mesh && !mc.convex && !mc.isTrigger && mc.enabled;
        mc.sharedMesh = mesh;
        mc.convex = false;                               // a convex collider would fill every doorway
        mc.isTrigger = false;
        mc.enabled = true;
        return !ok;
    }

    static void AddMeshCollider(GameObject g, Mesh mesh)
    {
        g.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    // Rugs, slippers, shawls etc. are too low to block anyone; a collider on them only makes the player climb them.
    static bool IsFlat(Transform t)
    {
        Renderer r = t.GetComponent<Renderer>();
        return r != null && r.bounds.size.y < 0.25f;
    }

    // True if this object (or any parent) is one of the touchable INT_ objects.
    static bool IsTouchable(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
            if (p.name.StartsWith("INT_")) return true;
        return false;
    }

    // True if one of this object's parents (below the house root) has one of the given names.
    static bool IsInside(Transform t, string[] groupNames, Transform root)
    {
        for (Transform p = t; p != null && p != root; p = p.parent)
            foreach (string name in groupNames)
                if (p.name == name) return true;
        return false;
    }

    static Transform FindByName(string name)
    {
        GameObject g = GameObject.Find(name);
        return g != null ? g.transform : null;
    }
}
