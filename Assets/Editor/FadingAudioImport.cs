#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Goes in: Assets/Editor (Editor only). Long music files in Assets/Resources/Audio/ (music_*, lullaby) are imported as
// STREAMING Vorbis, so they are not unpacked into memory all at once (faster loading, much less memory).
// Short sound effects keep Unity's normal settings.
public class FadingAudioImport : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        string file = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (!file.StartsWith("music_") && file != "lullaby") return;

        AudioImporter importer = (AudioImporter)assetImporter;
        AudioImporterSampleSettings s = importer.defaultSampleSettings;
        s.loadType = AudioClipLoadType.Streaming;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.7f;
        importer.defaultSampleSettings = s;
    }
}
#endif
