using UnityEngine;

// INT_Prop_Doll: Luna's doll on her dresser. (PropDressing places it in the house when a chapter starts.)
// Touch: the doll wiggles and hops.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropDoll : SlideAndReturn
{
    public override void ApplyDefaults()
    {
        prompt = "Wiggle the doll";
        slideDirection = Vector3.right;
        distance = 0.06f;
        hopHeight = 0.08f;
    }
}
