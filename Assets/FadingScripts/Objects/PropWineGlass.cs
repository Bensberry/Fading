using UnityEngine;

// INT_Prop_WineGlass: the anniversary glass on the sideboard. (PropDressing places it in the house when a chapter starts.)
// Touch: the glass hops and rings.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropWineGlass : SlideAndReturn
{
    public override void ApplyDefaults()
    {
        prompt = "Ring the glass";
        slideDirection = Vector3.forward;
        distance = 0.04f;
        hopHeight = 0.05f;
    }
}
