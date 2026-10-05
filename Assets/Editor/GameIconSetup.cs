#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// Goes in: Assets/Editor (Editor only). Makes Assets/Art/GameIcon.png the game's icon, automatically, whenever Unity compiles:
//   - PC: the .exe, the taskbar, the window (the "Default Icon")
//   - Android: every icon slot of the phone (Android does NOT use the Default Icon, it showed Unity's logo):
//       adaptive icon = GameIcon_AndroidBack.png (the art a bit smaller, so round / squircle masks keep "FADED")
//                       + GameIcon_AndroidFront.png (empty), round and legacy icons = GameIcon.png
// To change the icon: replace the PNGs (same names), then Tools > Fading > Set game icon.
[InitializeOnLoad]
public static class GameIconSetup
{
    const string IconPath = "Assets/Art/GameIcon.png";
    const string AndroidBackPath = "Assets/Art/GameIcon_AndroidBack.png";
    const string AndroidFrontPath = "Assets/Art/GameIcon_AndroidFront.png";

    static GameIconSetup()
    {
        EditorApplication.delayCall += () => Apply(false);
    }

    [MenuItem("Tools/Fading/Set game icon")]
    static void ApplyFromMenu() { Apply(true); }

    static void Apply(bool always)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Texture2D icon = LoadIcon(IconPath);
        Texture2D back = LoadIcon(AndroidBackPath);
        Texture2D front = LoadIcon(AndroidFrontPath);
        if (icon == null) return;

        bool changed = false;
        Texture2D[] current = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
        if (always || current == null || current.Length == 0 || current[0] != icon)
        {
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            changed = true;
        }
        if (back != null && front != null) changed |= SetAndroidIcons(icon, back, front, always);

        if (!changed) return;
        AssetDatabase.SaveAssets();
        Debug.Log("GameIconSetup: the game icon (PC and Android) is now " + IconPath);
    }

    // Fills every Android icon kind (adaptive, round, legacy) at every size.
    static bool SetAndroidIcons(Texture2D icon, Texture2D back, Texture2D front, bool always)
    {
        bool changed = false;
        foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            bool kindChanged = false;
            foreach (PlatformIcon slot in icons)
            {
                bool adaptive = slot.maxLayerCount >= 2;
                Texture2D first = adaptive ? back : icon;
                if (!always && slot.GetTexture(0) == first) continue;
                if (adaptive) slot.SetTextures(back, front);
                else slot.SetTexture(icon);
                kindChanged = true;
            }
            if (!kindChanged) continue;
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
            changed = true;
        }
        return changed;
    }

    // A clean, full-quality icon image.
    static Texture2D LoadIcon(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;
        if (importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.maxTextureSize < 1024)
        {
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
#endif
