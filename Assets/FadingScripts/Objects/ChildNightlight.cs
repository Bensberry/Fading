using UnityEngine;

// ============================================================
// INT_Child_Nightlight
// ============================================================
//
// This is simply the child nightlight configuration.
//
// The actual ClueGoal is inherited from LightSign:
//
//     LightSign.clueGoal
//
// Do NOT create another ClueGoal reference here.
// ============================================================

public class ChildNightlight : LightSign
{
    public override void ApplyDefaults()
    {
        prompt = "Turn on the nightlight";

        mode = Mode.TurnOn;

        startsOn = false;

        intensity = 0.6f;

        range = 2.5f;

        color = new Color(
            1f,
            0.85f,
            0.6f
        );
    }
}