using UnityEngine;

// INT_Grandma_RockingChair: Grandma's rocking chair by the window.
// Touch: the chair rocks on its runners.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Grandma_RockingChair object. Tweak the numbers in the Inspector if needed.
public class GrandmaRockingChair : RockingChairInteraction
{
    public override void ApplyDefaults()
    {
        prompt = "Rock the chair";
        maxAngle = 8f;
        rocksPerSecond = 0.8f;
    }
}
