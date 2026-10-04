using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (a static helper). Finds extra animation clips BY NAME while the game runs:
//   1. a file in Assets/Resources/Animations/ with that name (e.g. mom_sit.fbx downloaded from Mixamo), or
//   2. a clip inside a character's own model whose name contains that word (the ClipLibrary on the cast prefabs).
// Missing clips return null and the game simply skips that animation.
public static class GameClips
{
    static readonly Dictionary<string, AnimationClip> cache = new Dictionary<string, AnimationClip>();

    public static AnimationClip Get(string name, GameObject owner = null)
    {
        AnimationClip clip;
        if (!cache.TryGetValue(name, out clip))
        {
            clip = null;
            foreach (AnimationClip c in Resources.LoadAll<AnimationClip>("Animations/" + name))
                if (!c.name.StartsWith("__preview__")) { clip = c; break; }
            cache[name] = clip;
        }
        if (clip != null || owner == null) return clip;

        ClipLibrary library = owner.GetComponentInChildren<ClipLibrary>(true);
        if (library == null) library = owner.GetComponentInParent<ClipLibrary>();
        return library != null ? library.Find(name) : null;
    }

    // The first of several names that exists.
    public static AnimationClip First(GameObject owner, params string[] names)
    {
        foreach (string n in names)
        {
            AnimationClip c = Get(n, owner);
            if (c != null) return c;
        }
        return null;
    }
}
