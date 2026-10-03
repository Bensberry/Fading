using UnityEngine;

// INT_Mom_CoffeeMug: your old coffee mug on the kitchen table.
// Touch: the mug slides toward Mom's chair.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Mom_CoffeeMug object. Tweak the numbers in the Inspector if needed.
public class MomCoffeeMug : SlideAndReturn
{
    public override void ApplyDefaults()
    {
        prompt = "Push the mug";
        slideDirection = Vector3.left;   // Mom's chair is to the west
        distance = 0.18f;
        hopHeight = 0f;
    }
}
