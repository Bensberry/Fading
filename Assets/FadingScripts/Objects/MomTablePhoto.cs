using UnityEngine;

// INT_Mom_TablePhoto: the wedding photo on the kitchen table (backup sign).
// Touch: the frame tips toward Mom.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Mom_TablePhoto object. Tweak the numbers in the Inspector if needed.
public class MomTablePhoto : HingeSwing
{
    public override void ApplyDefaults()
    {
        prompt = "Tip the photo";
        partSuffix = "";
        localAxis = Vector3.right;
        angle = 14f;
        openTime = 0.4f;
        closeTime = 1.2f;
    }
}
