using UnityEngine;

// INT_Prop_ToyAirplane: Luna's toy airplane on the toy chest. (PropDressing places it in the house when a chapter starts.)
// Touch: the airplane turns as if it were flying.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropToyAirplane : SpinSign
{
    public override void ApplyDefaults()
    {
        prompt = "Fly the toy airplane";
        partSuffix = "_Part";
        degreesPerSecond = 150f;
    }
}
