using System.Collections;
using UnityEngine;

// Goes in: nowhere (play it with CutsceneRunner.Play(new MorningCutscene())).
// PLACEHOLDER story: the morning of Day 1 in the kitchen. Rewrite the lines and camera moves to match your story.
// Characters that are not in the scene yet are skipped, so it already plays as camera + subtitles.
// ChapterRules has a switch (PlayMorningCutscene) to play this at the start of Chapter1.
public class MorningCutscene : Cutscene
{
    public override IEnumerator Play(CutsceneContext c)
    {
        // Start on black, with cinema bars.
        yield return c.FadeNow(1f, Color.black);
        yield return c.Letterbox(true, 0.5f);
        yield return c.CutCamera(c.House(-11.8f, 1.5f, 4.6f), c.House(-12.1f, 0.9f, 7.9f));     // from the living-room doorway, looking at the table

        yield return c.Fade(0f, 2.5f);                                                            // morning light fades in
        yield return c.Say("", "Morning. The house is quieter without her.", 3.5f);

        yield return c.Say("MOM", "Eat something, sweetheart. We have so much left to pack.", 4f);
        yield return c.Say("CHILD", "I heard the doors last night. Was it Dad?", 4f);
        yield return c.Wait(1f);
        yield return c.Say("MOM", "...Eat your breakfast.", 3f);

        // The camera slowly drifts closer to the table, to the empty chair.
        yield return c.MoveCamera(c.House(-12.0f, 1.3f, 6.6f), c.House(-12.15f, 0.8f, 7.9f), 5f);
        yield return c.Say("", "Three days. Everything here is temporary, even me.", 4f);

        yield return c.Fade(1f, 1.5f);                                                            // out to black; the runner fades back into the game
    }
}
