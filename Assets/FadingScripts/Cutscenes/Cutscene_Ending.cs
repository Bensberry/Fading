using System.Collections;
using UnityEngine;

// Goes in: nowhere (play it with CutsceneRunner.Play(new EndingCutscene(1)), numbers 1 to 4).
// TEMPLATE for the 4 endings. The text below is placeholder; write your real endings here, and add camera moves,
// Anim(...) steps and sounds like in Cutscene_Morning.cs. Plays after Night 3, when you decide which ending the player earned.
public class EndingCutscene : Cutscene
{
    readonly int ending;

    public EndingCutscene(int ending) { this.ending = ending; }

    public override IEnumerator Play(CutsceneContext c)
    {
        yield return c.Letterbox(true, 0.5f);
        yield return c.Fade(1f, 1.5f, Color.black);

        switch (ending)
        {
            case 1:  yield return Show(c, "THE LIGHT", "They saw every sign. They left the house in peace, and so did he."); break;
            case 2:  yield return Show(c, "THE ECHO", "They felt him, but never understood. The house forgot him slowly."); break;
            case 3:  yield return Show(c, "THE COLD", "Fear drove them out in the night. He was alone with the candle."); break;
            default: yield return Show(c, "THE FADING", "No one noticed. The candle burned down, and so did he."); break;
        }
    }

    static IEnumerator Show(CutsceneContext c, string title, string line)
    {
        yield return c.Wait(1f);
        yield return c.Title(title, 4f);
        yield return c.Say("", line, 6f);
        yield return c.Wait(1f);
    }
}
