using UnityEngine;

// INT_Grandma_Clock: the grandfather clock in the guest room.
// Touch: the pendulum swings (put the chime in the Sound slot).
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Grandma_Clock object. Tweak the numbers in the Inspector if needed.
public class GrandmaClock : SwingOscillate
{
    public override void ApplyDefaults()
    {
        prompt = "Make the clock chime";
        partSuffix = "_Pendulum";
        localAxis = Vector3.forward;
        amplitude = 12f;
    }
}
