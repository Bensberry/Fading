using UnityEngine;

// INT_Prop_RubberDuck: Luna's rubber duck on her nightstand. (PropDressing places it in the house when a chapter starts.)
// Touch: the duck bounces.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropRubberDuck : SlideAndReturn
{
    public override void ApplyDefaults()
    {
        prompt = "Squeak the duck";
        slideDirection = Vector3.left;
        distance = 0.05f;
        hopHeight = 0.1f;
    }
}
