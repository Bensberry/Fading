using UnityEngine;

// INT_Child_StuffedToy: the teddy bear on the child's bed.
// Touch: the teddy hops toward the pillow, into the child's arms.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Child_StuffedToy object. Tweak the numbers in the Inspector if needed.
public class ChildStuffedToy : SlideAndReturn
{
    public override void ApplyDefaults()
    {
        prompt = "Nudge the teddy";
        slideDirection = Vector3.forward;   // the pillow is to the north
        distance = 0.25f;
        hopHeight = 0.06f;
    }
}
