using System.Collections;
using UnityEngine;

// Goes in: nowhere (NightQuest plays one when the memories are brought to a sleeping bed).
// The SIX dreams: each night you choose whose dream to visit, Mom's or Luna's.
//   Night 1   Mom: "The first dance" (living room)      Luna: "Stars" (her room, the mobile turning)
//   Night 2   Mom: "Morning coffee" (kitchen)           Luna: "First steps" (living room)
//   Night 3   Mom: "The porch" (the last evening)       Luna: "The candle" (hallway)
// "Father" is the ghost himself, a pale glowing figure. Positions are house-model points c.House(x, height, z), like StoryCutscenes.
// The last word of every Say is an OPTIONAL voice file (see the audio list). The text can be changed freely.
public class DreamCutscene : Cutscene
{
    static readonly Color DreamWhite = new Color(0.9f, 0.93f, 1f);
    static readonly Color Warm = new Color(1f, 0.82f, 0.58f);
    static readonly Color Moon = new Color(0.7f, 0.8f, 1f);

    readonly int night;
    readonly bool mom;

    public DreamCutscene(int night, bool momsDream)
    {
        this.night = Mathf.Clamp(night, 1, 3);
        mom = momsDream;
    }

    public override IEnumerator Play(CutsceneContext c)
    {
        SecretMemories.SawDream(night, mom);
        yield return c.FadeNow(1f, DreamWhite);
        yield return c.Letterbox(true, 0.3f);
        yield return c.CameraLight(true, 1f);
        MusicPlayer.Create().Play("music_dream", 2f);
        yield return c.Sfx("dream_swell", 0.7f);
        yield return c.Title(mom ? "Mom dreams..." : "Luna dreams...", 2.5f);

        if (mom)
        {
            if (night == 1) yield return FirstDance(c);
            else if (night == 2) yield return MorningCoffee(c);
            else yield return ThePorch(c);
        }
        else
        {
            if (night == 1) yield return Stars(c);
            else if (night == 2) yield return FirstSteps(c);
            else yield return TheCandle(c);
        }

        yield return c.Fade(1f, 1.8f, DreamWhite);
        MusicPlayer.Create().Play("music_night", 3f);
        yield return c.Letterbox(false, 0.3f);
    }

    // ---------- Mom, night 1: the first dance
    IEnumerator FirstDance(CutsceneContext c)
    {
        yield return c.Spawn("Mom", new Vector3(-12.6f, 0f, 2.4f), new Vector3(-12.6f, 0f, 3.3f));
        yield return c.Spawn("Father", new Vector3(-12.6f, 0f, 3.3f), new Vector3(-12.6f, 0f, 2.4f));
        yield return c.Glow(new Vector3(-12.6f, 2.2f, 2.85f), Warm, 2.5f, 6f);
        yield return c.Glimmers(new Vector3(-12.6f, 1.2f, 2.85f), 16, 1.8f);
        yield return c.CutCamera(c.House(-10.2f, 1.55f, 1.0f), c.House(-12.6f, 1.3f, 2.85f), 50f);
        yield return c.DreamHaze(0.14f, 2.5f);
        yield return c.Say("MOM", "You always stepped on my feet.", 3.5f, "voice_dream_mom1_a");
        yield return c.Say("FATHER", "I was nervous. I still am.", 3.5f, "voice_dream_mom1_b");
        yield return c.MoveCamera(c.House(-10.6f, 1.5f, 4.6f), c.House(-12.6f, 1.3f, 2.85f), 6f);
        yield return c.Say("MOM", "Stay a little longer... please.", 4f, "voice_dream_mom1_c");
    }

    // ---------- Mom, night 2: morning coffee
    IEnumerator MorningCoffee(CutsceneContext c)
    {
        yield return c.Spawn("Mom", new Vector3(-12.15f, 0f, 9.0f), new Vector3(-12.15f, 0.8f, 7.94f));
        yield return c.Spawn("Father", new Vector3(-12.15f, 0f, 6.9f), new Vector3(-12.15f, 0.8f, 7.94f));
        yield return c.Glow(new Vector3(-12.15f, 2.2f, 7.9f), Warm, 3f, 6f);
        yield return c.Glimmers(new Vector3(-12.15f, 1.3f, 7.94f), 12, 1.5f);
        yield return c.CutCamera(c.House(-14.6f, 1.5f, 8.0f), c.House(-12.15f, 1.1f, 7.94f), 50f);
        yield return c.DreamHaze(0.14f, 2.5f);
        yield return c.Say("MOM", "You always made the coffee too strong.", 3.5f, "voice_dream_mom2_a");
        yield return c.Say("FATHER", "And you drank it anyway.", 3f, "voice_dream_mom2_b");
        yield return c.MoveCamera(c.House(-13.9f, 1.4f, 9.6f), c.House(-12.15f, 1.2f, 8.4f), 5f);
        yield return c.Say("MOM", "I still make two cups.", 4f, "voice_dream_mom2_c");
    }

    // ---------- Mom, night 3: the porch, the last evening
    IEnumerator ThePorch(CutsceneContext c)
    {
        yield return c.Spawn("Mom", new Vector3(-8.6f, 0f, -1.6f), new Vector3(-4f, 0f, -9f));
        yield return c.Spawn("Father", new Vector3(-7.6f, 0f, -1.6f), new Vector3(-4f, 0f, -9f));
        yield return c.Glow(new Vector3(-8.1f, 2.4f, -1.0f), Moon, 2.5f, 7f);
        yield return c.Glimmers(new Vector3(-8.1f, 1.4f, -2.2f), 14, 2f);
        yield return c.CutCamera(c.House(-8.1f, 1.3f, -6.5f), c.House(-8.1f, 1.2f, -1.6f), 45f);
        yield return c.DreamHaze(0.12f, 2.5f);
        yield return c.Say("MOM", "Tomorrow we leave this house.", 3.5f, "voice_dream_mom3_a");
        yield return c.Say("FATHER", "The house was never what I loved.", 4f, "voice_dream_mom3_b");
        yield return c.TurnActor("Mom", new Vector3(-7.6f, 0f, -1.6f), 1.5f);
        yield return c.MoveCamera(c.House(-8.1f, 1.4f, -4.6f), c.House(-8.1f, 1.3f, -1.6f), 4f, 40f);
        yield return c.Say("MOM", "...I know.", 3.5f, "voice_dream_mom3_c");
    }

    // ---------- Luna, night 1: stars over her bed
    IEnumerator Stars(CutsceneContext c)
    {
        yield return c.Spawn("Baby", new Vector3(-2.86f, 0.40f, 13.85f), new Vector3(-3.6f, 0.4f, 12.2f), CastStance.Sitting);
        yield return c.Spawn("Father", new Vector3(-3.7f, 0f, 12.1f), new Vector3(-2.86f, 0.6f, 13.8f));
        yield return c.Glimmers(new Vector3(-2.86f, 1.7f, 13.4f), 22, 1.2f);
        yield return c.Glow(new Vector3(-2.86f, 2.3f, 13.4f), Moon, 2f, 5f);
        yield return c.CutCamera(c.House(-4.8f, 1.4f, 11.2f), c.House(-2.9f, 0.8f, 13.6f), 55f);
        yield return c.DreamHaze(0.14f, 2.5f);
        yield return c.Touch("INT_Child_Mobile");
        yield return c.BabyLooksAt("Father");
        yield return c.Sfx("baby_giggle", 0.8f);
        yield return c.Say("FATHER", "Every star up there is a night I'll watch over you.", 5f, "voice_dream_luna1_a");
        yield return c.MoveCamera(c.House(-3.9f, 1.2f, 12.3f), c.House(-2.86f, 0.75f, 13.85f), 4f, 45f);
    }

    // ---------- Luna, night 2: her first steps toward him
    IEnumerator FirstSteps(CutsceneContext c)
    {
        yield return c.Spawn("Baby", new Vector3(-14.4f, 0f, 2.2f), new Vector3(-11.6f, 0f, 2.6f), CastStance.Standing);
        yield return c.Spawn("Father", new Vector3(-11.6f, 0f, 2.6f), new Vector3(-14.4f, 0f, 2.2f));
        yield return c.Glow(new Vector3(-13f, 2.2f, 2.4f), Warm, 2.5f, 6f);
        yield return c.Glimmers(new Vector3(-13f, 0.8f, 2.4f), 14, 1.6f);
        yield return c.CutCamera(c.House(-13f, 1.2f, 0.6f), c.House(-13f, 0.5f, 2.4f), 55f);
        yield return c.DreamHaze(0.14f, 2.5f);
        yield return c.Say("FATHER", "Come here... that's it.", 3f, "voice_dream_luna2_a");
        yield return c.MoveActor("Baby", new Vector3(-12.4f, 0f, 2.5f), 5f);
        yield return c.BabyLooksAt("Father");
        yield return c.Sfx("baby_giggle", 0.9f);
        yield return c.Say("FATHER", "That's my girl.", 3.5f, "voice_dream_luna2_b");
    }

    // ---------- Luna, night 3: the candle in the hallway
    IEnumerator TheCandle(CutsceneContext c)
    {
        yield return c.Spawn("Baby", new Vector3(-8.4f, 0f, 2.4f), new Vector3(-7.3f, 0.9f, 2.4f), CastStance.Sitting);
        yield return c.Spawn("Father", new Vector3(-8.75f, 0f, 3.5f), new Vector3(-8.4f, 0f, 2.4f));
        yield return c.Glimmers(new Vector3(-7.5f, 1.1f, 2.4f), 12, 0.7f);
        yield return c.Glow(new Vector3(-7.4f, 1.3f, 2.4f), Warm, 2f, 4f);
        yield return c.CutCamera(c.House(-8.9f, 0.9f, 0.9f), c.House(-7.8f, 0.6f, 2.4f), 50f);
        yield return c.DreamHaze(0.14f, 2.5f);
        yield return c.Say("FATHER", "You won't remember me, little one.", 4f, "voice_dream_luna3_a");
        yield return c.BabyLooksAt("Father");
        yield return c.Sfx("baby_giggle", 0.7f);
        yield return c.Say("FATHER", "That's all right. I'll remember for both of us.", 5f, "voice_dream_luna3_b");
    }
}
