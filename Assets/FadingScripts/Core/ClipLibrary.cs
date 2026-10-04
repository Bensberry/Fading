using UnityEngine;

// Goes in: the cast prefabs in Assets/Resources/Cast (Assets/Editor/CastPrefabBuilder.cs adds it, never by hand).
// A list of the animation clips inside a character's model file (for the baby: crawling, sleeping ...),
// so the game can play them while it runs (see GameClips and PosePlayer).
public class ClipLibrary : MonoBehaviour
{
    public AnimationClip[] clips;

    // The first clip whose name contains 'part' (not case-sensitive), or null.
    public AnimationClip Find(string part)
    {
        if (clips == null) return null;
        foreach (AnimationClip c in clips)
            if (c != null && c.name.IndexOf(part, System.StringComparison.OrdinalIgnoreCase) >= 0) return c;
        return null;
    }
}
