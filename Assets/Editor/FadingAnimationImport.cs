#if UNITY_EDITOR
using UnityEditor;

// Goes in: Assets/Editor (it only runs inside the Unity Editor, never in the built game).
// Any model file you drop into Assets/Resources/Animations/ (for example mom_sit.fbx downloaded from Mixamo)
// is imported the right way automatically:
//   - as a Humanoid animation, so it plays on Mom's (or the baby's) model
//   - looping (except files whose name contains "_once")
//   - staying in place (no drifting across the room)
// You do not need to change any import settings by hand.
public class FadingAnimationImport : AssetPostprocessor
{
    const string Folder = "Assets/Resources/Animations/";

    void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Folder)) return;
        ModelImporter importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
    }

    void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(Folder)) return;
        ModelImporter importer = (ModelImporter)assetImporter;
        bool loop = !assetPath.ToLower().Contains("_once");
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        foreach (ModelImporterClipAnimation clip in clips)
        {
            clip.loopTime = loop;
            clip.lockRootRotation = true;
            clip.lockRootHeightY = true;
            clip.lockRootPositionXZ = true;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = true;
            clip.keepOriginalPositionXZ = true;
        }
        importer.clipAnimations = clips;
    }
}
#endif
