using UnityEngine;

// Goes in: nowhere. It runs by itself when a scene starts (no need to attach it).
// The imported house (FadingHouse.glb) has no colliders, so the player could walk through walls or fall.
// This adds them at runtime and does not change how anything looks:
//   - big solid parts (floors, walls, ceiling, door frames, windows, ground...) get a Mesh Collider
//   - furniture and packing boxes get a simple Box Collider
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AddColliders()
    {
        FadingInteractablesSetup setup = Object.FindFirstObjectByType<FadingInteractablesSetup>();
        Transform house = setup != null ? setup.transform : FindByName("FadingHouse");
        if (house == null) return;

        foreach (Transform t in house.GetComponentsInChildren<Transform>(true))
        {
            if (IsTouchable(t)) continue;
            MeshFilter filter = t.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || t.GetComponent<Collider>() != null) continue;

            if (IsInside(t, SolidGroups, house)) AddMeshCollider(t.gameObject, filter.sharedMesh);
            else if (IsInside(t, BoxGroups, house)) AddBoxCollider(t.gameObject, filter.sharedMesh);
        }
    }

    static void AddMeshCollider(GameObject g, Mesh mesh)
    {
        g.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    static void AddBoxCollider(GameObject g, Mesh mesh)
    {
        BoxCollider box = g.AddComponent<BoxCollider>();
        box.center = mesh.bounds.center;
        box.size = mesh.bounds.size;
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
