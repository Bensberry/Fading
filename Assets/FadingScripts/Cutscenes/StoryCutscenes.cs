using System.Collections;
using UnityEngine;

// Goes in: nowhere (ChapterRules plays these at the right moments of the story).
// The story cutscenes, in game order. Each one is a list of steps ("yield return c.something"):
//   1. IntroCutscene          start of Chapter 0: the candle is lit, the ghost wakes
//   2. ChapterZeroEndCutscene end of Chapter 0: Grandma speaks to him, sends him to his family, leaves
//   3. NightOneCutscene       Chapter 1, when Night 1 begins: Mom by the bed, the baby sees him
//   4. DayTwoCutscene         Chapter 2, start of Day 2: the baby laughs at nothing, Mom wonders
//   5. EndingCutscene         Chapter 3, after the last night: "You can go now", the candle goes out
// The dialogue lines are placeholders: change the text inside Say("WHO", "text", seconds) freely.
// Positions are points of the house MODEL (x, height above floor, z): c.House(x, y, z). Nudge them if a shot looks off.
// Mom and Baby are stand-in figures until real model prefabs exist (see CutsceneContext.cs).

// ------------------------------------------------------------------------------------------------- 1
public class IntroCutscene : Cutscene
{
    public override IEnumerator Play(CutsceneContext c)
    {
        yield return c.FadeNow(1f, Color.black);
        yield return c.Letterbox(true, 0.3f);

        // The memorial candle on the hallway console.
        yield return c.CutCamera(c.House(-8.4f, 1.00f, 2.45f), c.House(-7.3f, 0.90f, 2.45f), 22f);
        yield return c.Fade(0f, 2f);
        yield return c.Say("GRANDMA", "They say the light shows you the way home. Just until it burns out.", 5.5f);
        yield return c.Wait(0.5f);
        yield return c.Fade(1f, 1f);

        // The ghost wakes on the floor of Grandma's room and drifts toward the lamp.
        yield return c.CutCamera(c.House(-5.2f, 0.30f, 3.2f), c.House(-1.6f, 0.70f, 3.2f), 60f);
        yield return c.Fade(0f, 1.5f);
        yield return c.MoveCamera(c.House(-3.6f, 0.35f, 3.2f), c.House(-1.6f, 0.75f, 3.2f), 6f);
        yield return c.Say("", "A light in the dark. Someone is calling me home.", 3.5f);
        yield return c.Fade(1f, 1.2f);
        yield return c.Letterbox(false, 0.3f);
    }
}

// ------------------------------------------------------------------------------------------------- 2
public class ChapterZeroEndCutscene : Cutscene
{
    public override IEnumerator Play(CutsceneContext c)
    {
        yield return c.Letterbox(true, 0.5f);
        yield return c.Fade(1f, 0.8f, Color.black);

        // Grandma in the rocking chair by the lamp.
        yield return c.CutCamera(c.House(-4.8f, 1.25f, 3.0f), c.House(-1.2f, 0.70f, 3.0f), 45f);
        yield return c.Fade(0f, 1.5f);
        yield return c.Say("GRANDMA", "...Is that you?", 3.5f);
        yield return c.Say("GRANDMA", "You always came home late.", 4f);

        // Closer, three-quarter view.
        yield return c.MoveCamera(c.House(-3.2f, 1.20f, 2.3f), c.House(-1.2f, 1.00f, 3.5f), 5f, 40f);
        yield return c.Say("GRANDMA", "I know you're here, son.", 4f);

        // The ghost's seat: she looks straight at him.
        yield return c.CutCamera(c.House(-1.0f, 1.05f, 2.2f), c.House(-1.2f, 0.85f, 3.7f), 70f);
        yield return c.Say("GRANDMA", "Go see them. They need you more than I do.", 5.5f);
        yield return c.Fade(1f, 2f);

        // Morning: the empty hallway and the candle. She has gone.
        yield return c.CutCamera(c.House(-8.4f, 1.00f, 2.45f), c.House(-7.3f, 0.90f, 2.45f), 22f);
        yield return c.Fade(0f, 1.5f);
        yield return c.Say("GRANDMA", "Look after them, wherever you are.", 4.5f);
        yield return c.Wait(1f);
        yield return c.Fade(1f, 1.5f);
        yield return c.Letterbox(false, 0.3f);
    }
}

// ------------------------------------------------------------------------------------------------- 3
public class NightOneCutscene : Cutscene
{
    public override IEnumerator Play(CutsceneContext c)
    {
        yield return c.FadeNow(1f, Color.black);
        yield return c.Letterbox(true, 0.3f);
        yield return c.CameraLight(true, 0.9f);                                    // the night is nearly black: a soft light on the camera

        yield return c.Spawn("Mom", new Vector3(-4.3f, 0f, 12.6f), new Vector3(-2.9f, 0.5f, 13.6f), CastStance.Sitting);
        yield return c.Spawn("Baby", new Vector3(-2.9f, 0.5f, 13.7f), new Vector3(-2.9f, 1f, 8f), CastStance.Sitting);

        // The child's room: Mom on the floor beside the bed.
        yield return c.CutCamera(c.House(-3.0f, 1.60f, 8.4f), c.House(-2.9f, 0.50f, 13.6f), 60f);
        yield return c.Fade(0f, 2f);
        yield return c.Say("MOM", "She has your eyes. I keep waiting for you to walk through that door.", 6.5f);
        yield return c.Touch("INT_Child_Mobile");                                  // the mobile turns by itself
        yield return c.Say("", "The mobile turns, though nobody touched it.", 4f);
        yield return c.Fade(1f, 1f);

        // From the doorway: the baby wakes and looks straight at him.
        yield return c.CutCamera(c.House(-6.6f, 1.60f, 8.16f), c.House(-2.9f, 0.50f, 13.6f), 70f);
        yield return c.Fade(0f, 1.5f);
        yield return c.TurnActor("Baby", new Vector3(-6.6f, 1.6f, 8.16f), 1.5f);
        yield return c.MoveCamera(c.House(-5.6f, 1.60f, 9.4f), c.House(-2.9f, 0.70f, 13.6f), 6f, 65f);
        yield return c.Say("FATHER", "Luna... you can see me?", 4f);
        yield return c.Fade(1f, 1.2f);
        yield return c.Letterbox(false, 0.3f);
    }
}

// ------------------------------------------------------------------------------------------------- 4
public class DayTwoCutscene : Cutscene
{
    public override IEnumerator Play(CutsceneContext c)
    {
        yield return c.FadeNow(1f, Color.black);
        yield return c.Letterbox(true, 0.3f);

        yield return c.Spawn("Baby", new Vector3(-13.7f, 0f, 3.1f), new Vector3(-9.5f, 1f, 5.2f), CastStance.Sitting);
        yield return c.Spawn("Mom", new Vector3(-9.6f, 0f, 2.8f), new Vector3(-13.7f, 0f, 3.1f), CastStance.Standing);

        // The living room by day: the baby on the rug, boxes by the window.
        yield return c.CutCamera(c.House(-9.5f, 2.00f, 5.2f), c.House(-13.5f, 0.20f, 2.0f), 75f);
        yield return c.Fade(0f, 2f);
        yield return c.Say("", "Luna giggles and reaches for something I can't hold.", 4f);

        yield return c.MoveActor("Mom", new Vector3(-12.2f, 0f, 3.7f), 5f);
        yield return c.Say("MOM", "Who are you smiling at, baby?", 4f);
        yield return c.TurnActor("Mom", new Vector3(-9.5f, 1.6f, 5.2f), 1.5f);      // she looks toward where he is
        yield return c.MoveCamera(c.House(-10.6f, 1.50f, 4.6f), c.House(-12.2f, 1.40f, 3.7f), 4f, 50f);
        yield return c.Say("MOM", "...Is it you?", 4.5f);
        yield return c.Fade(1f, 1.5f);
        yield return c.Letterbox(false, 0.3f);
    }
}

// ------------------------------------------------------------------------------------------------- 5
public class EndingCutscene : Cutscene
{
    public override IEnumerator Play(CutsceneContext c)
    {
        yield return c.Letterbox(true, 0.5f);
        yield return c.Fade(1f, 1.5f, Color.black);
        yield return c.Dawn();                                                     // the world switches to morning light

        yield return c.Spawn("Mom", new Vector3(-12.6f, 0f, 1.8f), new Vector3(-12.6f, 1.6f, 4.4f), CastStance.Standing);
        yield return c.Spawn("Baby", new Vector3(-12.35f, 0.95f, 1.95f), new Vector3(-12.6f, 1.6f, 4.4f), CastStance.Sitting);

        // The bare living room at dawn, Mom at the window with Luna.
        yield return c.CutCamera(c.House(-12.6f, 1.50f, 4.4f), c.House(-12.6f, 1.60f, 0.0f), 55f);
        yield return c.Fade(0f, 2.5f);
        yield return c.Say("MOM", "It's okay. We're going to be okay.", 4.5f);
        yield return c.Say("MOM", "You can go now.", 4f);
        yield return c.Fade(1f, 1.5f);

        // The memorial candle goes out.
        yield return c.CutCamera(c.House(-8.4f, 1.00f, 2.45f), c.House(-7.3f, 0.90f, 2.45f), 22f);
        yield return c.Fade(0f, 1.5f);
        yield return c.Extinguish("INT_Hallway_MemorialCandle", 4f);
        yield return c.Wait(1.5f);
        yield return c.Title("Nothing lasts. Some things stay.", 5f);
        yield return c.Fade(1f, 2f);
        yield return c.Letterbox(false, 0.3f);
    }
}
