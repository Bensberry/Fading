using UnityEngine;

// INT_Door_Front: the front door.
// Middle-click: the door swings open into the room and stays open.
// Middle-click again: it shuts almost instantly. (Doors do not follow the 5-second rule.)
// Put this script on the INT_Door_Front object. Tweak the numbers in the Inspector if needed.
public class FrontDoor : DoorToggle
{
    public override void ApplyDefaults()
    {
        prompt = "Open the door";
        localAxis = Vector3.up;
        angle = -90f;        // opens inward
        openTime = 0.9f;
        closeTime = 0.15f;   // near-instant close
    }
}
