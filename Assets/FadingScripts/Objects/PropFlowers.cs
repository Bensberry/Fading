using UnityEngine;

// INT_Prop_Flowers: a vase of flowers on the kitchen counter. (PropDressing places it in the house when a chapter starts.)
// Touch: the flowers sway as if a breeze came in.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropFlowers : SwingOscillate
{
    public override void ApplyDefaults()
    {
        prompt = "Stir the flowers";
        partSuffix = "_Part";
        localAxis = Vector3.forward;
        amplitude = 10f;
        swingsPerSecond = 0.8f;
    }
}
