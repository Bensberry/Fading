using UnityEngine;

// INT_Child_Mobile: the mobile hanging over the child's bed (backup sign).
// Touch: the mobile spins.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Child_Mobile object. Tweak the numbers in the Inspector if needed.
public class ChildMobile : SpinSign
{
    public override void ApplyDefaults()
    {
        prompt = "Spin the mobile";
        partSuffix = "_Spinner";
        degreesPerSecond = 70f;
    }
}
