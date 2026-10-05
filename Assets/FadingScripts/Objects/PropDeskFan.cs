using UnityEngine;

// INT_Prop_DeskFan: the little fan on Mom's dresser. (PropDressing places it in the house when a chapter starts.)
// Touch: the fan turns around by itself.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropDeskFan : SpinSign
{
    public override void ApplyDefaults()
    {
        prompt = "Switch on the fan";
        partSuffix = "_Part";
        degreesPerSecond = 120f;
    }
}
