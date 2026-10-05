using UnityEngine;

// INT_Prop_CupOfTea: Mom's cup of tea on the kitchen table. (PropDressing places it in the house when a chapter starts.)
// Touch: the cup slides a little toward her chair.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropCupOfTea : SlideAndReturn
{
    public override void ApplyDefaults()
    {
        prompt = "Nudge the tea";
        slideDirection = Vector3.left;
        distance = 0.15f;
        hopHeight = 0f;
    }
}
