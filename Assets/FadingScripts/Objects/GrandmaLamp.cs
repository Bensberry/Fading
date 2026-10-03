using UnityEngine;

// INT_Grandma_Lamp: the table lamp next to Grandma's chair.
// Touch: the lamp flickers, then glows brighter.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Grandma_Lamp object. Tweak the numbers in the Inspector if needed.
public class GrandmaLamp : LightSign
{
    public override void ApplyDefaults()
    {
        prompt = "Flicker the lamp";
        mode = Mode.Flicker;
        startsOn = true;
        intensity = 1.2f;
        range = 4f;
    }
}
