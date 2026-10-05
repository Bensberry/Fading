using UnityEngine;

// INT_Prop_PiggyBank: Luna's piggy bank on her bookshelf. (PropDressing places it in the house when a chapter starts.)
// Touch: the piggy bank rattles.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
public class PropPiggyBank : SwingOscillate
{
    public override void ApplyDefaults()
    {
        prompt = "Rattle the piggy bank";
        partSuffix = "_Part";
        localAxis = Vector3.forward;
        amplitude = 12f;
        swingsPerSecond = 4f;
    }
}
