using UnityEngine;

// INT_Door_Child: the child's bedroom door.
// Press F: the door swings open into the room and stays open.
// Press F again: it shuts almost instantly. (Doors do not follow the 5-second rule.)
// Put this script on the INT_Door_Child object. Tweak the numbers in the Inspector if needed.
public class ChildRoomDoor : DoorToggle
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
