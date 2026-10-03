using UnityEngine;

// INT_Door_Guest: the guest room (Grandma's) door.
// Touch: the door swings open into the room.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Door_Guest object. Tweak the numbers in the Inspector if needed.
public class GuestRoomDoor : HingeSwing
{
    public override void ApplyDefaults()
    {
        prompt = "Open the door";
        partSuffix = "";
        localAxis = Vector3.up;
        angle = -90f;   // opens inward
        openTime = 1.0f;
        closeTime = 0.35f;   // fast = it slams shut
    }
}
