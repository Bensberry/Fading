using UnityEngine;

// INT_Prop_CeilingFan: the ceiling fan in the living room. (PropDressing places it in the house when a chapter starts.)
// Touch: the fan starts to turn.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropCeilingFan : SpinSign
{
    public override void ApplyDefaults()
    {
        prompt = "Turn the ceiling fan";
        partSuffix = "_Part";
        degreesPerSecond = 240f;
        spinUpTime = 1.5f;
    }
}
