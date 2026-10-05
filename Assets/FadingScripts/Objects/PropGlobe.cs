using UnityEngine;

// INT_Prop_Globe: the globe on Mom's dresser. (PropDressing places it in the house when a chapter starts.)
// Touch: the globe spins.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropGlobe : SpinSign
{
    public override void ApplyDefaults()
    {
        prompt = "Spin the globe";
        partSuffix = "_Part";
        degreesPerSecond = 220f;
    }
}
