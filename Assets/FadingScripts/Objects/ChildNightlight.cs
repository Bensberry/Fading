using UnityEngine;

// INT_Child_Nightlight: the nightlight on the child's nightstand.
// Touch: the nightlight switches on.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Child_Nightlight object. Tweak the numbers in the Inspector if needed.
public class ChildNightlight : LightSign
{
    public override void ApplyDefaults()
    {
        prompt = "Turn on the nightlight";
        mode = Mode.TurnOn;
        startsOn = false;
        intensity = 0.6f;
        range = 2.5f;
        color = new Color(1f, 0.85f, 0.6f);
    }
}
