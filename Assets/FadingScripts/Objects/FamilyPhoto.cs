using UnityEngine;

// INT_Hallway_FamilyPhoto: the family photo on the hallway table.
// Touch: the frame tips forward.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Hallway_FamilyPhoto object. Tweak the numbers in the Inspector if needed.
public class FamilyPhoto : HingeSwing
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
