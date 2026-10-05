#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// Goes in: Assets/Editor (Editor only). Makes Assets/Art/GameIcon.png the game's icon (the .exe, the taskbar, the window),
// automatically, whenever Unity compiles and the icon is not set yet. To change the icon: replace the PNG (same name).
[InitializeOnLoad]
public static class GameIconSetup
{
    const string IconPath = "Assets/Art/GameIcon.png";

    static GameIconSetup()
    {
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/Fading/Set game icon")]
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // A clean, full-quality icon image.
        TextureImporter importer = AssetImporter.GetAtPath(IconPath) as TextureImporter;
        if (importer == null) return;
        if (importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.maxTextureSize < 1024)
        {
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null) return;
        Texture2D[] current = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
        if (current != null && current.Length > 0 && current[0] == icon) return;     // already set
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
        AssetDatabase.SaveAssets();
        Debug.Log("GameIconSetup: the game icon is now " + IconPath);
    }
}
#endif
