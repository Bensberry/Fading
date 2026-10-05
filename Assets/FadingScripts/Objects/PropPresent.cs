using UnityEngine;

// INT_Prop_Present: a wrapped present on Luna's rug (her birthday is soon). (PropDressing places it in the house when a chapter starts.)
// Touch: the present jumps.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropPresent : SlideAndReturn
{
    public override void ApplyDefaults()
    {
        prompt = "Shake the present";
        slideDirection = Vector3.forward;
        distance = 0.05f;
        hopHeight = 0.12f;
    }
}
