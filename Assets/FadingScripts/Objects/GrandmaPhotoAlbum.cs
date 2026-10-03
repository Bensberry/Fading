using UnityEngine;

// INT_Grandma_PhotoAlbum: the photo album on Grandma's side table.
// Touch: the cover swings open (to the baby photo).
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Grandma_PhotoAlbum object. Tweak the numbers in the Inspector if needed.
public class GrandmaPhotoAlbum : HingeSwing
{
    public override void ApplyDefaults()
    {
        prompt = "Open the album";
        partSuffix = "_Cover";
        localAxis = Vector3.forward;
        angle = 170f;
        openTime = 1.0f;
        closeTime = 1.5f;
    }
}
