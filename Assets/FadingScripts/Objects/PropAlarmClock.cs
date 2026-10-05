using UnityEngine;

// INT_Prop_AlarmClock: the alarm clock on Mom's nightstand. (PropDressing places it in the house when a chapter starts.)
// Touch: the clock rattles as if it were ringing.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropAlarmClock : SwingOscillate
{
    public override void ApplyDefaults()
    {
        prompt = "Rattle the alarm clock";
        partSuffix = "_Part";
        localAxis = Vector3.forward;
        amplitude = 7f;
        swingsPerSecond = 6f;
    }
}
