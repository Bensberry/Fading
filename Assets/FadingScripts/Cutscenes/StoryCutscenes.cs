using System.Collections;
using UnityEngine;

// Goes in: nowhere (ChapterRules plays these at the right moments of the story).
// The story cutscenes, in game order. Each one is a list of steps ("yield return c.something"):
//   0. ArrivalCutscene        before Chapter 0: the father's ghost walks up to the house in thick fog (then the Intro follows)
//   1. IntroCutscene          start of Chapter 0: the candle is lit, the ghost wakes
//   2. ChapterZeroEndCutscene end of Chapter 0: Grandma speaks to him, sends him to his family, leaves
//   3. NightOneCutscene       Chapter 1, when Night 1 begins: Mom by the bed, the baby sees him
//   4. DayTwoCutscene         Chapter 2, start of Day 2: the baby laughs at nothing, Mom wonders
//   5. EndingCutscene(1-4)    Chapter 3 after the last night: one of four endings, picked by the Mom and Luna bars
// (the six dream cutscenes of the nights are in DreamCutscenes.cs)
// The dialogue lines are placeholders: change the text inside Say("WHO", "text", seconds, "voice_file") freely.
// The last word of every Say is the name of an OPTIONAL voice file in Assets/Resources/Audio/ (see the audio list).
// Positions are points of the house MODEL (x, height above floor, z): c.House(x, y, z). Nudge them if a shot looks off.
// Mom and Baby are the real models (Resources/Cast) once those prefabs exist, otherwise simple stand-in figures.

// ------------------------------------------------------------------------------------------------- 0
// Before Chapter 0: the father's ghost walks up the path to his house through thick fog. The door opens by itself.
public class ArrivalCutscene : Cutscene
{
    static readonly Color Mist = new Color(0.42f, 0.47f, 0.55f);

    public override IEnumerator Play(CutsceneContext c)
    {
        yield return c.FadeNow(1f, Color.black);
        yield return c.Letterbox(true, 0.3f);
        yield return c.Fog(0.11f, Mist);                                         // very foggy, just for this scene
        yield return c.CameraLight(true, 0.6f);
        yield return c.Glow(new Vector3(-8.1f, 2.2f, -0.8f), new Color(1f, 0.78f, 0.5f), 2.5f, 7f);    // the porch light
        yield return c.Spawn("Father", new Vector3(-8.1f, -0.45f, -15f), new Vector3(-8.1f, 0f, 0f));

        // The road: a figure comes out of the fog.
        yield return c.CutCamera(c.House(-4.2f, 1.3f, -17.5f), c.House(-8.1f, 1.4f, -9f), 45f);
        yield return c.Together(c.MoveActor("Father", new Vector3(-8.1f, -0.45f, -8.5f), 9f));
        yield return c.Fade(0f, 3f);
        yield return c.Say("", "I know this road. I walked it home every night.", 4.5f, "voice_arrival_a");
        yield return c.Wait(1.5f);
        yield return c.Fade(1f, 1.2f);

        // Close behind him: the house, a light in the window.
        yield return c.CutCamera(c.House(-7.2f, 1.7f, -11.5f), c.House(-8.1f, 1.5f, -1f), 50f);
        yield return c.Together(c.MoveActor("Father", new Vector3(-8.1f, 0f, -1.1f), 6.5f));
        yield return c.Fade(0f, 1.5f);
        yield return c.Say("", "The house is still here. Someone left a candle burning.", 4.5f, "voice_arrival_b");
        yield return c.Wait(1f);

        // The door opens for him.
        yield return c.CutCamera(c.House(-6.6f, 1.4f, -4.2f), c.House(-8.1f, 1.3f, -0.3f), 45f);
        yield return c.OpenDoor("INT_Door_Front");
        yield return c.Wait(1f);
        yield return c.MoveActor("Father", new Vector3(-8.1f, 0f, 1.2f), 2.5f);
        yield return c.Fade(1f, 1.5f);
        yield return c.Show("Father", false);
        yield return c.Fog(0f, Mist);                                             // inside: clear again

        // ...and straight on into the candle scene (one cutscene, so the game does not flash in between).
        yield return new IntroCutscene().Play(c);
    }
}

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
        yield return c.Spawn("Baby", new Vector3(-2.86f, 0.40f, 13.85f), new Vector3(-2.86f, 0.4f, 8f), CastStance.Sitting);     // the mattress top is 0.40 m high

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
// The four endings, picked at the end of the last night by the two bars (FamilyProgress.PickEnding()):
//   1 THE FLIGHT   nobody felt him: his desperate signs frightened them, Mom takes Luna and flees the house
//   2 THE SMILE    only Mom felt him: she smiles and speaks to him
//   3 THE CRADLE   only Luna felt him: she looks up from her cradle and smiles at him
//   4 HOME         both felt him: Mom lets him go, Luna smiles, the candle burns bright and goes out gently
public class EndingCutscene : Cutscene
{
    readonly int ending;

    public EndingCutscene(int ending) { this.ending = Mathf.Clamp(ending, 1, 4); }

    public override IEnumerator Play(CutsceneContext c)
    {
        MusicPlayer.Create().Play("music_ending", 4f);
        SecretMemories.SawEnding(ending);
        yield return c.Letterbox(true, 0.5f);
        yield return c.Fade(1f, 1.5f, Color.black);
        yield return c.Dawn();                                                     // the world switches to morning light

        if (ending == 1) yield return TheFlight(c);
        else yield return LivingRoom(c);

        // The memorial candle.
        yield return c.CutCamera(c.House(-8.4f, 1.00f, 2.45f), c.House(-7.3f, 0.90f, 2.45f), 22f);
        yield return c.Fade(0f, 1.5f);
        yield return c.Sfx("candle_out", 0.9f);
        yield return c.Extinguish("INT_Hallway_MemorialCandle", ending == 4 ? 6f : 3.5f);
        yield return c.Wait(1.5f);

        string title;
        switch (ending)
        {
            case 1: title = "Everything is temporary."; break;
            case 2: title = "Some things stay."; break;
            case 3: title = "She will not remember. But she was loved."; break;
            default: title = "Nothing lasts. Some things stay."; break;
        }
        yield return c.Title(title, 5f);
        yield return c.Fade(1f, 2f);

        // A short credits card, then the main menu.
        yield return c.Title("FADING  -  made for the jam \"Everything Is Temporary\"", 4f);
        yield return c.Title("Furniture and nature by Kenney (CC0)  -  Textures by Poly Haven (CC0)", 4f);
        yield return c.Title("Models by Poly by Google and Ray Larson (CC-BY), via Poly Pizza  -  Characters and animations: Mixamo", 4.5f);
        yield return c.Title("Thank you for playing.", 3.5f);
        yield return c.Title(SecretMemories.Found < SecretMemories.Total
            ? "You found " + SecretMemories.Found + " of " + SecretMemories.Total + " secret memories. Choose other dreams, rest in other ways, reach Mom or Luna differently, and see what else stays hidden."
            : "You found every secret memory. Thank you for remembering them all.", 5.5f);
        yield return c.Letterbox(false, 0.3f);
    }

    // 1: frightened, Mom leaves the house in a hurry. Nobody looks back.
    IEnumerator TheFlight(CutsceneContext c)
    {
        yield return c.Spawn("Mom", new Vector3(-8.1f, 0f, 1.2f), new Vector3(-8.1f, 0f, -3f), CastStance.Standing);
        yield return c.CutCamera(c.House(-8.1f, 1.5f, -6.0f), c.House(-8.1f, 1.2f, 0f), 45f);
        yield return c.Fade(0f, 2f);
        yield return c.Say("MOM", "I can't stay here another night. Something in this house won't let us rest.", 5.5f, "voice_end1_mom");
        yield return c.Touch("INT_Door_Front");                                    // she opens the front door
        yield return c.MoveActor("Mom", new Vector3(-8.1f, 0f, -4.5f), 3.5f);
        yield return c.Say("", "I only wanted them to know I was here.", 4.5f, "voice_end1_ghost");
        yield return c.Fade(1f, 1.5f);
    }

    // 2, 3, 4: the bare living room at dawn, Mom beside Luna's cradle.
    IEnumerator LivingRoom(CutsceneContext c)
    {
        Vector3 cradleSpot = new Vector3(-11.7f, 0f, 2.3f);
        yield return c.Spawn("Mom", new Vector3(-12.6f, 0f, 1.8f), new Vector3(-12.6f, 1.6f, 4.4f), CastStance.Standing);
        yield return c.Cradle(cradleSpot, new Vector3(-12.6f, 1.6f, 4.4f), true);
        yield return c.CutCamera(c.House(-12.6f, 1.50f, 4.4f), c.House(-12.6f, 1.60f, 0.0f), 55f);
        yield return c.Fade(0f, 2.5f);

        if (ending == 2)
        {
            yield return c.Say("MOM", "I know it's you. I always knew.", 4.5f, "voice_end2_mom_a");
            yield return c.Say("MOM", "Thank you for staying with us. You can rest now.", 5f, "voice_end2_mom_b");
        }
        else if (ending == 3)
        {
            yield return c.Say("MOM", "Who are you smiling at, Luna?", 4f, "voice_end3_mom");
            yield return c.Fade(1f, 1f);
            yield return c.CameraLight(true, 0.5f);
            yield return c.BabyLooksUpAndSmiles(6f);
            yield return c.Say("", "She will not remember me. But she knew I was here.", 5f, "voice_end3_ghost");
            yield return c.CameraLight(false);
        }
        else
        {
            yield return c.Say("MOM", "We'll be okay. Both of us.", 4f, "voice_end4_mom_a");
            yield return c.Say("MOM", "Go home, love.", 3.5f, "voice_end4_mom_b");
            yield return c.Fade(1f, 1f);
            yield return c.CameraLight(true, 0.5f);
            yield return c.BabyLooksUpAndSmiles(6f);
            yield return c.Say("", "I'm home.", 3.5f, "voice_end4_ghost");
            yield return c.CameraLight(false);
        }
        yield return c.Fade(1f, 1.5f);
    }
}
