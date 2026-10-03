using System.Collections;
using UnityEngine;

// Goes in: nowhere (play it with CutsceneRunner.Play(new GrandmaLeavesCutscene())).
// PLACEHOLDER story: Grandma says goodbye and walks out the front door. Not hooked into the game yet:
// play it where you like, e.g. after Granny's cutscene at the end of Chapter 0.
// The positions are points of the house MODEL (c.House(x, height, z)); adjust them after you see it.
public class GrandmaLeavesCutscene : Cutscene
{
    public override IEnumerator Play(CutsceneContext c)
    {
        yield return c.Letterbox(true, 0.5f);
        yield return c.CutCamera(c.House(-6.5f, 1.5f, 2.0f), c.House(-3.0f, 1.3f, 3.0f));        // in the hallway, looking toward Grandma's room

        yield return c.Say("GRANDMA", "I held this house together for forty years.", 4f);
        yield return c.Say("GRANDMA", "Look after them... wherever you are.", 4f);

        // She walks along the hallway to the front door while the camera follows.
        yield return c.TurnActor("Grandma", c.House(7.5f, 0f, 0.5f), 0.8f);
        yield return c.MoveActor("Grandma", c.House(7.5f, 0f, 0.5f), 8f);
        yield return c.MoveCamera(c.House(2.0f, 1.5f, 2.5f), c.House(7.5f, 1.2f, 0.5f), 5f);

        yield return c.Fade(1f, 1.5f);
    }
}
