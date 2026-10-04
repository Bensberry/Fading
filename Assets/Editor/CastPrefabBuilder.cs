#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

// Goes in: Assets/Editor (it only runs inside the Unity Editor, never in the built game).
// The game needs to load Mom and the baby while it runs (for the cutscenes, and to give the baby her model).
// That only works for prefabs inside a "Resources" folder. This script makes two small prefab files for that,
// pointing at the existing model files (no 47 MB copy):
//     Assets/Resources/Cast/Mom.prefab    = femeie_1 model + WifeAnimator controller, about 1.7 m tall
//     Assets/Resources/Cast/Baby.prefab   = Baby 1+motions model + KidAnimator controller, about 0.75 m long
// They are created automatically when Unity recompiles and the files do not exist yet.
// To make them again (e.g. after changing a model): Tools > Fading > Rebuild cast prefabs.
[InitializeOnLoad]
public static class CastPrefabBuilder
{
    const string MomModel = "Assets/Characters/Best-wife/femeie_1.fbx";
    const string MomController = "Assets/WifeAnimator.controller";
    const string BabyModel = "Assets/Chandran-20261003T171444Z-1-001/Chandran/source/Baby 1+motions.fbx";
    const string BabyController = "Assets/KidAnimator.controller";

    static CastPrefabBuilder()
    {
        EditorApplication.delayCall += () => Build(false);
    }

    [MenuItem("Tools/Fading/Rebuild cast prefabs")]
    static void Rebuild() { Build(true); }

    static void Build(bool force)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        bool changed = false;
        changed |= Make("Mom", MomModel, MomController, 1.7f, true, force);
        changed |= Make("Baby", BabyModel, BabyController, 0.75f, false, force);
        if (changed) AssetDatabase.SaveAssets();
    }

    // sizeIsHeight: true = scale so the model is 'size' metres tall; false = scale so its longest side is 'size' metres.
    static bool Make(string name, string modelPath, string controllerPath, float size, bool sizeIsHeight, bool force)
    {
        string prefabPath = "Assets/Resources/Cast/" + name + ".prefab";
        if (!force && File.Exists(prefabPath)) return false;

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (model == null) { Debug.LogWarning("CastPrefabBuilder: model not found: " + modelPath); return false; }

        Directory.CreateDirectory("Assets/Resources/Cast");
        GameObject root = new GameObject(name);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);

        // Scale to a sensible real-world size and put the feet on the floor (the prefab's origin).
        Bounds bounds = BoundsOf(instance);
        float current = sizeIsHeight ? bounds.size.y : Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (current > 0.0001f) instance.transform.localScale = Vector3.one * (size / current);
        bounds = BoundsOf(instance);
        instance.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);

        // The animator (with its controller).
        Animator animator = instance.GetComponent<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        if (animator.avatar == null)
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(modelPath))
                if (o is Avatar) { animator.avatar = (Avatar)o; break; }
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        animator.applyRootMotion = false;

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        Debug.Log("CastPrefabBuilder: created " + prefabPath);
        return true;
    }

    static Bounds BoundsOf(GameObject g)
    {
        Renderer[] renderers = g.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(g.transform.position, Vector3.one);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
#endif
