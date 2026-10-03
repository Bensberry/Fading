using UnityEngine;

// INT_Mom_FogWindow: the fogged kitchen window above the sink.
// Touch: your nickname for her is traced letter by letter in the fog.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Mom_FogWindow object. Tweak the numbers in the Inspector if needed.
public class MomFogWindow : FogWriting
{
    public override void ApplyDefaults()
    {
        prompt = "Trace in the fog";
        nickname = "Sunny";   // change to the real nickname
    }
}
