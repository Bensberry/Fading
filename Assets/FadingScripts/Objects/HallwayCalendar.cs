using UnityEngine;

// INT_Hallway_Calendar: the calendar on the hallway wall.
// Touch: the calendar swings crooked on its nail.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Hallway_Calendar object. Tweak the numbers in the Inspector if needed.
public class HallwayCalendar : HingeSwing
{
    public override void ApplyDefaults()
    {
        prompt = "Swing the calendar";
        partSuffix = "";
        localAxis = Vector3.forward;
        angle = 12f;
        openTime = 0.5f;
        closeTime = 1.5f;
    }
}
