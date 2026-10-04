using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Goes in: nowhere (a static helper used by MomLife, BabyLife and others).
// Knows where the rooms of the house are, using the floor of each room from the house model
// (Floor_Hallway, Floor_Child, Floor_Mother ...), and finds walkable points (NavMesh) inside them.
//   HouseRooms.RandomPoint("ChildRoom", out point)   a random walkable spot in that room
//   HouseRooms.Contains("ChildRoom", position)       is this position inside that room?
//   HouseRooms.Path(from, to)                         the corner points of a walkable path (null if there is none)
//   HouseRooms.TryGetObjectBounds("Bed_Mother", out bounds)   where a named object of the house is
public static class HouseRooms
{
    static readonly Dictionary<string, string> Floors = new Dictionary<string, string>
    {
        { "Hallway", "Floor_Hallway" }, { "ChildRoom", "Floor_Child" }, { "MotherRoom", "Floor_Mother" },
        { "LivingRoom", "Floor_Living" }, { "Kitchen", "Floor_Kitchen" }, { "GuestRoom", "Floor_Guest" },
    };

    static readonly Dictionary<string, Bounds> cache = new Dictionary<string, Bounds>();
    static Scene cachedScene;

    public static bool TryGetRoomBounds(string room, out Bounds bounds)
    {
        string floor;
        if (!Floors.TryGetValue(room, out floor)) floor = room;
        if (TryGetObjectBounds(floor, out bounds)) return true;
        return TryGetObjectBounds(room, out bounds);                  // no floor found: use the room's furniture
    }

    public static bool TryGetObjectBounds(string objectName, out Bounds bounds)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene != cachedScene) { cache.Clear(); cachedScene = scene; }
        if (cache.TryGetValue(objectName, out bounds)) return true;

        GameObject g = GameObject.Find(objectName);
        if (g == null) return false;
        Renderer[] renderers = g.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return false;
        bounds = renderers[0].bounds;
        foreach (Renderer r in renderers) bounds.Encapsulate(r.bounds);
        cache[objectName] = bounds;
        return true;
    }

    public static bool Contains(string room, Vector3 position)
    {
        Bounds b;
        if (!TryGetRoomBounds(room, out b)) return false;
        return position.x >= b.min.x && position.x <= b.max.x && position.z >= b.min.z && position.z <= b.max.z;
    }

    // A random walkable point inside the room (a little away from the walls).
    public static bool RandomPoint(string room, out Vector3 point)
    {
        point = Vector3.zero;
        Bounds b;
        if (!TryGetRoomBounds(room, out b)) return false;

        for (int attempt = 0; attempt < 15; attempt++)
        {
            Vector3 p = new Vector3(Random.Range(b.min.x + 0.5f, b.max.x - 0.5f), b.max.y, Random.Range(b.min.z + 0.5f, b.max.z - 0.5f));
            NavMeshHit hit;
            if (!NavMesh.SamplePosition(p, out hit, 1.2f, NavMesh.AllAreas)) continue;
            if (!Contains(room, hit.position)) continue;
            point = hit.position;
            return true;
        }
        return false;
    }

    public static Vector3[] Path(Vector3 from, Vector3 to)
    {
        NavMeshHit start, end;
        if (!NavMesh.SamplePosition(from, out start, 2f, NavMesh.AllAreas)) return null;
        if (!NavMesh.SamplePosition(to, out end, 2f, NavMesh.AllAreas)) return null;

        NavMeshPath path = new NavMeshPath();
        if (!NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path)) return null;
        if (path.status != NavMeshPathStatus.PathComplete || path.corners.Length < 2) return null;

        Vector3[] corners = new Vector3[path.corners.Length - 1];                 // skip the first corner (where she already is)
        System.Array.Copy(path.corners, 1, corners, 0, corners.Length);
        return corners;
    }
}
