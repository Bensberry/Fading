using UnityEngine;

// Goes in: nowhere (a static helper). Makes the simple shapes the game builds in code (candle flames, the cradle,
// cutscene stand-ins) with a REAL material: Assets/Resources/Materials/Fading_Lit (URP Lit, emission switched on).
// Why: a shape made with GameObject.CreatePrimitive has no proper URP material in a built game, so it shows up purple/pink.
// Use FadingMaterials.Primitive(PrimitiveType.Sphere) instead of GameObject.CreatePrimitive(PrimitiveType.Sphere).
public static class FadingMaterials
{
    static Material template;

    public static GameObject Primitive(PrimitiveType type)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        Material m = Lit();
        if (m != null) g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    // A new copy of the game's lit material (white, no glow). Null only if the material file is missing.
    public static Material Lit()
    {
        if (template == null) template = Resources.Load<Material>("Materials/Fading_Lit");
        return template != null ? new Material(template) : null;
    }
}
