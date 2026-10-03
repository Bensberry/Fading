using UnityEngine;

// INT_Child_MusicBox: the music box on the child's dresser.
// Touch: the lid opens, the key turns and the lullaby plays (put it in the Lullaby slot).
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Child_MusicBox object. Tweak the numbers in the Inspector if needed.
public class ChildMusicBox : MusicBoxInteraction
{
    public override void ApplyDefaults()
    {
        prompt = "Wind the music box";
        lidOpenAngle = 70f;
    }
}
