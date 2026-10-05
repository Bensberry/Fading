using System.Collections;
using UnityEngine;

// Goes in: nowhere (NightRest and the star bench play these). Three ways for the ghost to REST and end the night:
//   StarsCutscene        the bench in front of the house: he looks up at the stars and falls asleep
//   SleepBesideCutscene  (false) on the floor beside Luna's bed     (true) beside Mom, on her bed
// "Father" is the ghost's own model (Resources/Cast/Father), poses come from Assets/Resources/Animations.
// Positions are house-model points c.House(x, height, z), like the other cutscenes. The text can be changed freely;
// the last word of each Say is the voice file.
public class StarsCutscene : Cutscene
{
    readonly Vector3 seat, facing;           // world: the bench seat and the way it faces

    public StarsCutscene(Vector3 seatWorld, Vector3 facingWorld)
    {
        seat = seatWorld;
        facing = new Vector3(facingWorld.x, 0f, facingWorld.z).normalized;
    }

    public override IEnumerator Play(CutsceneContext c)
    {
        yield return c.FadeNow(1f, Color.black);
        yield return c.Letterbox(true, 0.3f);
        yield return c.CameraLight(true, 0.4f);
        MusicPlayer.Create().Play("music_dream", 2f);

        Vector3 houseSeat = c.ToHouse(seat), houseAhead = c.ToHouse(seat + facing * 4f);
        yield return c.Spawn("Father", houseSeat, houseAhead);
        yield return c.Pose("Father", "mom_sit");
        yield return c.SitAt("Father", houseSeat + Vector3.up * 0.08f, houseAhead);

        Vector3 right = Vector3.Cross(Vector3.up, facing);
        Vector3 cam = seat + facing * 2.4f + right * 1.3f + Vector3.up * 0.8f;
        yield return c.CutCamera(cam, seat + Vector3.up * 0.6f, 50f);
        yield return c.Fade(0f, 2f);
        yield return c.Say("", "We used to sit out here and count them. She always lost count first.", 5f, "voice_rest_stars_a");

        // The camera turns up to the stars.
        yield return c.MoveCamera(cam + Vector3.up * 0.3f, seat + Vector3.up * 30f + facing * 18f, 5f, 62f);
        yield return c.Say("", "Just for a moment... I'll close my eyes.", 4f, "voice_rest_stars_b");
        yield return c.Fade(1f, 3f);
        yield return c.Letterbox(false, 0.3f);
    }
}

public class SleepBesideCutscene : Cutscene
{
    static readonly Color Moon = new Color(0.6f, 0.72f, 1f);
    readonly bool mom;

    public SleepBesideCutscene(bool besideMom) { mom = besideMom; }

    public override IEnumerator Play(CutsceneContext c)
    {
        yield return c.FadeNow(1f, Color.black);
        yield return c.Letterbox(true, 0.3f);
        yield return c.CameraLight(true, 0.45f);
        MusicPlayer.Create().Play("music_dream", 2f);
        if (mom) yield return BesideMom(c);
        else yield return BesideLuna(c);
        yield return c.Fade(1f, 3f);
        yield return c.Letterbox(false, 0.3f);
    }

    // On the floor beside her bed, close enough to hear her breathe.
    IEnumerator BesideLuna(CutsceneContext c)
    {
        yield return c.Spawn("Baby", new Vector3(-2.86f, 0.40f, 13.5f), new Vector3(-2.86f, 0.4f, 11f), CastStance.Standing);
        yield return c.Pose("Baby", "sleeping");
        yield return c.LieDown("Baby", new Vector3(-2.86f, 0.40f, 13.45f), new Vector3(-2.86f, 0.4f, 14.3f));
        yield return c.BabyAsleep();
        yield return c.Spawn("Father", new Vector3(-3.85f, 0f, 12.6f), new Vector3(-2.86f, 0f, 13.4f));
        yield return c.Pose("Father", "mom_sleep");
        yield return c.LieDown("Father", new Vector3(-3.85f, 0.02f, 12.7f), new Vector3(-3.85f, 0.02f, 13.9f));
        yield return c.Glow(new Vector3(-1.5f, 2.0f, 12f), Moon, 1.6f, 6f);
        yield return c.Glimmers(new Vector3(-3.2f, 1.0f, 13.3f), 14, 1.0f);

        yield return c.CutCamera(c.House(-4.9f, 1.5f, 11.2f), c.House(-3.3f, 0.45f, 13.3f), 50f);
        yield return c.Fade(0f, 2f);
        yield return c.Say("FATHER", "Shh... Daddy's here. Nothing to be afraid of.", 4.5f, "voice_rest_luna_a");
        yield return c.MoveCamera(c.House(-4.3f, 1.1f, 12.2f), c.House(-3.1f, 0.45f, 13.5f), 5f, 45f);
        yield return c.Say("", "Her breathing is the softest sound in the world.", 4.5f, "voice_rest_luna_b");
        yield return c.Say("FATHER", "Goodnight, little star.", 3.5f, "voice_rest_luna_c");
    }

    // Beside her on the bed, the way it used to be.
    IEnumerator BesideMom(CutsceneContext c)
    {
        yield return c.Spawn("Mom", new Vector3(-12.6f, 0f, 13f), new Vector3(-12.6f, 0f, 11f));
        yield return c.Pose("Mom", "mom_sleep");
        yield return c.LieDown("Mom", new Vector3(-12.62f, 0.58f, 13.05f), new Vector3(-12.62f, 0.58f, 14.2f));
        yield return c.Spawn("Father", new Vector3(-11.7f, 0f, 13f), new Vector3(-11.7f, 0f, 11f));
        yield return c.Pose("Father", "mom_sleep");
        yield return c.LieDown("Father", new Vector3(-11.72f, 0.58f, 13.05f), new Vector3(-11.72f, 0.58f, 14.2f));
        yield return c.Glow(new Vector3(-10f, 2.2f, 12.5f), Moon, 1.6f, 6f);
        yield return c.Glimmers(new Vector3(-12.15f, 1.2f, 13.3f), 12, 1.2f);

        yield return c.CutCamera(c.House(-12.15f, 2.0f, 10.8f), c.House(-12.15f, 0.75f, 13.5f), 48f);
        yield return c.Fade(0f, 2f);
        yield return c.Say("FATHER", "Twelve years, and I still can't sleep until you do.", 5f, "voice_rest_mom_a");
        yield return c.MoveCamera(c.House(-12.15f, 1.6f, 11.6f), c.House(-12.15f, 0.7f, 13.7f), 5f, 44f);
        yield return c.Say("MOM", "...stay.", 3f, "voice_rest_mom_b");
        yield return c.Say("FATHER", "Always.", 3f, "voice_rest_mom_c");
    }
}
