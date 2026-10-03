using UnityEngine;

// INT_Mom_Radio: the radio on the kitchen table.
// Touch: it plays the wedding song (put it in the Song slot) and trembles.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Mom_Radio object. Tweak the numbers in the Inspector if needed.
public class MomRadio : RadioSign
{
    public override void ApplyDefaults()
    {
        prompt = "Turn on the radio";
        volume = 0.7f;
    }
}
