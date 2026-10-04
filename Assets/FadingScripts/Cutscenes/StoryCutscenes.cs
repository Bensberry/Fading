using System.Collections;
using UnityEngine;

// Goes in: nowhere (ChapterRules plays these at the right moments of the story).
// The story cutscenes, in game order. Each one is a list of steps ("yield return c.something"):
//   1. IntroCutscene          start of Chapter 0: the candle is lit, the ghost wakes
//   2. ChapterZeroEndCutscene end of Chapter 0: Grandma speaks to him, sends him to his family, leaves
//   3. NightOneCutscene       Chapter 1, when Night 1 begins: Mom by the bed, the baby sees him
//   4. DayTwoCutscene         Chapter 2, start of Day 2: the baby laughs at nothing, Mom wonders
//   5. EndingCutscene(1-4)    Chapter 3, after the last night: one of four endings (see StoryProgress.cs)
// The dialogue lines are placeholders: change the text inside Say("WHO", "text", seconds, "voice_file") freely.
// The last word of every Say is the name of an OPTIONAL voice file in Assets/Resources/Audio/ (see the audio list).
// Positions are points of the house MODEL (x, height above floor, z): c.House(x, y, z). Nudge them if a shot looks off.
// Mom and Baby are the real models (Resources/Cast) once those prefabs exist, otherwise simple stand-in figures.

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
        yield return c.Say("GRANDMA", "They say the light shows you the way home. Just until it burns out.", 5.5f, "voice_grandma_candle");
        yield return c.Wait(0.5f);
        yield return c.Fade(1f, 1f);

        // The ghost wakes on the floor of Grandma's room and drifts toward the lamp.
        yield return c.CutCamera(c.House(-5.2f, 0.30f, 3.2f), c.House(-1.6f, 0.70f, 3.2f), 60f);
        yield return c.Fade(0f, 1.5f);
        yield return c.MoveCamera(c.House(-3.6f, 0.35f, 3.2f), c.House(-1.6f, 0.75f, 3.2f), 6f);
        yield return c.Say("", "A light in the dark. Someone is calling me home.", 3.5f, "voice_ghost_calling");
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
        yield return c.Say("GRANDMA", "...Is that you?", 3.5f, "voice_grandma_isthatyou");
        yield return c.Say("GRANDMA", "You always came home late.", 4f, "voice_grandma_latehome");

        // Closer, three-quarter view.
        yield return c.MoveCamera(c.House(-3.2f, 1.20f, 2.3f), c.House(-1.2f, 1.00f, 3.5f), 5f, 40f);
        yield return c.Say("GRANDMA", "I know you're here, son.", 4f, "voice_grandma_son");

        // The ghost's seat: she looks straight at him.
        yield return c.CutCamera(c.House(-1.0f, 1.05f, 2.2f), c.House(-1.2f, 0.85f, 3.7f), 70f);
        yield return c.Say("GRANDMA", "Go see them. They need you more than I do.", 5.5f, "voice_grandma_gotothem");
        yield return c.Fade(1f, 2f);

        // Morning: the empty hallway and the candle. She has gone.
        yield return c.CutCamera(c.House(-8.4f, 1.00f, 2.45f), c.House(-7.3f, 0.90f, 2.45f), 22f);
        yield return c.Fade(0f, 1.5f);
        yield return c.Say("GRANDMA", "Look after them, wherever you are.", 4.5f, "voice_grandma_lookafter");
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
        yield return c.Say("MOM", "She has your eyes. I keep waiting for you to walk through that door.", 6.5f, "voice_mom_eyes");
        yield return c.Touch("INT_Child_Mobile");                                  // the mobile turns by itself
        yield return c.Say("", "The mobile turns, though nobody touched it.", 4f, "voice_ghost_mobile");
        yield return c.Fade(1f, 1f);

        // From the doorway: the baby wakes and looks straight at him.
        yield return c.CutCamera(c.House(-6.6f, 1.60f, 8.16f), c.House(-2.9f, 0.50f, 13.6f), 70f);
        yield return c.Fade(0f, 1.5f);
        yield return c.TurnActor("Baby", new Vector3(-6.6f, 1.6f, 8.16f), 1.5f);
        yield return c.Sfx("baby_giggle", 0.8f);
        yield return c.MoveCamera(c.House(-5.6f, 1.60f, 9.4f), c.House(-2.9f, 0.70f, 13.6f), 6f, 65f);
        yield return c.Say("FATHER", "Luna... you can see me?", 4f, "voice_ghost_luna");
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
        yield return c.Sfx("baby_giggle", 0.9f);
        yield return c.Say("", "Luna giggles and reaches for something I can't hold.", 4f, "voice_ghost_giggles");

        yield return c.MoveActor("Mom", new Vector3(-12.2f, 0f, 3.7f), 5f);
        yield return c.Say("MOM", "Who are you smiling at, baby?", 4f, "voice_mom_whosmiling");
        yield return c.TurnActor("Mom", new Vector3(-9.5f, 1.6f, 5.2f), 1.5f);      // she looks toward where he is
        yield return c.MoveCamera(c.House(-10.6f, 1.50f, 4.6f), c.House(-12.2f, 1.40f, 3.7f), 4f, 50f);
        yield return c.Say("MOM", "...Is it you?", 4.5f, "voice_mom_isityou");
        yield return c.Fade(1f, 1.5f);
        yield return c.Letterbox(false, 0.3f);
    }
}

// ------------------------------------------------------------------------------------------------- 5
// ending 1 = THE LIGHT, 2 = THE ECHO, 3 = THE COLD, 4 = THE FADING (picked by StoryProgress.PickEnding()).
public class EndingCutscene : Cutscene
{
    readonly int ending;

    public EndingCutscene(int ending) { this.ending = Mathf.Clamp(ending, 1, 4); }

    public override IEnumerator Play(CutsceneContext c)
    {
        MusicPlayer.Create().Play("music_ending", 4f);
        yield return c.Letterbox(true, 0.5f);
        yield return c.Fade(1f, 1.5f, Color.black);
        yield return c.Dawn();                                                     // the world switches to morning light

        // The bare living room at dawn. In the worst ending nobody is there.
        if (ending != 4)
        {
            yield return c.Spawn("Mom", new Vector3(-12.6f, 0f, 1.8f), new Vector3(-12.6f, 1.6f, 4.4f), CastStance.Standing);
            yield return c.Spawn("Baby", new Vector3(-12.35f, 0.95f, 1.95f), new Vector3(-12.6f, 1.6f, 4.4f), CastStance.Sitting);
        }
        yield return c.CutCamera(c.House(-12.6f, 1.50f, 4.4f), c.House(-12.6f, 1.60f, 0.0f), 55f);
        yield return c.Fade(0f, 2.5f);

        switch (ending)
        {
            case 1:                                                                // THE LIGHT: they feel him and let him go
                yield return c.Say("MOM", "It's okay. We're going to be okay.", 4.5f, "voice_mom_okay");
                yield return c.Say("MOM", "You can go now.", 4f, "voice_mom_yougonow");
                break;
            case 2:                                                                // THE ECHO: half felt
                yield return c.Say("MOM", "Sometimes I feel you here. I don't know why.", 5f, "voice_mom_feelyou");
                yield return c.Say("MOM", "Goodbye, anyway.", 3.5f, "voice_mom_goodbye");
                break;
            case 3:                                                                // THE COLD: they only sensed the cold and left
                yield return c.Say("MOM", "This house is so cold now. Come on, Luna. Let's go.", 5.5f, "voice_mom_letsgo");
                yield return c.TurnActor("Mom", new Vector3(-12.6f, 1.6f, -3f), 2f);   // she turns away from the room
                break;
            default:                                                               // THE FADING: nobody noticed
                yield return c.Say("", "Nobody noticed. Nobody turned around.", 5f, "voice_ghost_nobody");
                break;
        }
        yield return c.Fade(1f, 1.5f);

        // The memorial candle: it goes out (in THE ECHO it stays lit, small and faint).
        yield return c.CutCamera(c.House(-8.4f, 1.00f, 2.45f), c.House(-7.3f, 0.90f, 2.45f), 22f);
        yield return c.Fade(0f, 1.5f);
        if (ending != 2) { yield return c.Sfx("candle_out", 0.9f); yield return c.Extinguish("INT_Hallway_MemorialCandle", 4f); }
        else yield return c.Wait(4f);
        yield return c.Wait(1.5f);

        string title;
        switch (ending)
        {
            case 1: title = "Nothing lasts. Some things stay."; break;
            case 2: title = "Some things echo."; break;
            case 3: title = "He stayed behind."; break;
            default: title = "Everything is temporary."; break;
        }
        yield return c.Title(title, 5f);
        yield return c.Fade(1f, 2f);
        yield return c.Letterbox(false, 0.3f);
    }
}
