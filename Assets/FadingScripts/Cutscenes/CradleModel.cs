using UnityEngine;

// Goes in: nowhere (the ending cutscene builds it with c.Cradle(...)).
// A simple wooden baby cradle on rockers, made of boxes (about 0.95 m long, 0.8 m high, long side along X).
// Want a nicer one? Put your own model in Assets/Resources/Cast/ named "Cradle" (an .fbx or a prefab,
// real size in metres, origin on the floor) and it is used instead automatically.
public static class CradleModel
{
    static readonly Color Wood = new Color(0.58f, 0.40f, 0.26f);
    static readonly Color DarkWood = new Color(0.42f, 0.28f, 0.18f);
    static readonly Color Sheet = new Color(0.86f, 0.91f, 0.97f);

    // Builds the cradle at the world origin. mattressTop = height of the mattress surface above the floor.
    public static GameObject Make(out float mattressTop)
    {
        GameObject custom = Resources.Load<GameObject>("Cast/Cradle");
        if (custom != null)
        {
            GameObject c = Object.Instantiate(custom);
            Renderer[] rs = c.GetComponentsInChildren<Renderer>();
            mattressTop = 0.45f;
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                foreach (Renderer r in rs) b.Encapsulate(r.bounds);
                mattressTop = b.min.y + b.size.y * 0.5f;                  // a guess: halfway up
            }
            return c;
        }

        GameObject root = new GameObject("Cradle");
        Transform t = root.transform;

        // Two curved rockers (short boxes along an arc).
        for (int side = -1; side <= 1; side += 2)
            for (int i = -2; i <= 2; i++)
            {
                float a = i * 11f * Mathf.Deg2Rad;
                const float R = 1.2f;
                Box(t, new Vector3(Mathf.Sin(a) * R, R - Mathf.Cos(a) * R + 0.03f, side * 0.2f), new Vector3(0.23f, 0.04f, 0.045f), DarkWood, i * 11f);
            }

        // Posts, floor and mattress.
        for (int x = -1; x <= 1; x += 2)
            for (int z = -1; z <= 1; z += 2)
                Box(t, new Vector3(x * 0.38f, 0.22f, z * 0.2f), new Vector3(0.05f, 0.34f, 0.05f), DarkWood);
        Box(t, new Vector3(0f, 0.38f, 0f), new Vector3(0.92f, 0.04f, 0.48f), Wood);
        Box(t, new Vector3(0f, 0.41f, 0f), new Vector3(0.86f, 0.06f, 0.42f), Sheet);

        // Head and foot boards, rails and slats.
        for (int x = -1; x <= 1; x += 2) Box(t, new Vector3(x * 0.47f, 0.6f, 0f), new Vector3(0.04f, 0.46f, 0.5f), Wood);
        for (int z = -1; z <= 1; z += 2)
        {
            Box(t, new Vector3(0f, 0.79f, z * 0.25f), new Vector3(0.94f, 0.04f, 0.04f), Wood);
            Box(t, new Vector3(0f, 0.41f, z * 0.25f), new Vector3(0.94f, 0.04f, 0.04f), Wood);
            for (int i = 0; i < 10; i++)
                Box(t, new Vector3(-0.405f + i * 0.09f, 0.6f, z * 0.25f), new Vector3(0.025f, 0.36f, 0.025f), Wood);
        }

        mattressTop = 0.44f;
        return root;
    }

    static void Box(Transform parent, Vector3 position, Vector3 size, Color color, float tiltZ = 0f)
    {
        GameObject g = FadingMaterials.Primitive(PrimitiveType.Cube);
        Object.Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = position;
        g.transform.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
        g.transform.localScale = size;
        g.GetComponent<Renderer>().material.color = color;
    }
}
