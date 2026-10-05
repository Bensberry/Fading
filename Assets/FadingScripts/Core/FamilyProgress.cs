using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in every chapter).
// TWO bars at the top of the screen: how much MOM feels you, and how much LUNA feels you.
// Points come the MOMENT something good happens (FamilyProgress.Award):
//   Mom SEES one of your signs         Mom  +6 (the same object again: +1)
//   Luna feels a sign near her          Luna +4 (again: +1),   Luna smiles at you  Luna +3 (again: +1)
//   the day's goals are done            both +4 (ChapterGoals)
//   a night's dream is delivered        Mom +15 or Luna +15 (NightQuest: you choose whose bed)
// Scares (FamilyFear) push the bars back. The bars CARRY OVER between chapters (empty only in a new game).
// A full bar does NOT end the game: at the end of the last night the two bars pick the ending (PickEnding):
//   1 neither feels you (you scared them away)   2 only Mom   3 only Luna   4 both
// Change BarPoints to make the bars harder (bigger) or easier (smaller), and FeltAt for how full "feels you" is.
public class FamilyProgress : MonoBehaviour
{
    public const float BarPoints = 70f;
    public const float FeltAt = 0.7f;                         // a bar at least this full at the end = "she noticed you"

    public enum Who { Mom, Luna, Both }

    static float momCarried, lunaCarried, mom, luna;          // carried = from earlier chapters, mom/luna = this chapter
    static readonly HashSet<string> rewarded = new HashSet<string>();
    static float lastAwardTime = -100f;
    static bool momFullShown, lunaFullShown;

    float momShown, lunaShown;

    public static float MomFraction { get { return Mathf.Clamp01((momCarried + mom) / BarPoints); } }
    public static float LunaFraction { get { return Mathf.Clamp01((lunaCarried + luna) / BarPoints); } }

    // Something good happened. 'key' says what (e.g. "mom:INT_Mom_CoffeeMug"); the first time gives 'first' points, later 'repeat'.
    public static void Award(Who who, string key, float first, float repeat, string message)
    {
        float points = (rewarded.Add(key) ? first : repeat) * Difficulty.ProgressMultiplier;   // Story fills fastest, Hard slowest
        if (points <= 0f) return;
        if (who != Who.Luna) mom = Mathf.Min(mom + points, BarPoints - momCarried + 0.01f);
        if (who != Who.Mom) luna = Mathf.Min(luna + points, BarPoints - lunaCarried + 0.01f);
        lastAwardTime = Time.time;
        FadingHud.ProgressGain("+" + Mathf.RoundToInt(points) + "   " + message, who != Who.Luna, who != Who.Mom);
        GameAudio.Play("notice_chime", 0.55f);
    }

    // Scares: the bars go down (never below 0).
    public static void AddPenalty(float momPoints, float lunaPoints)
    {
        mom = Mathf.Max(-momCarried, mom - momPoints);
        luna = Mathf.Max(-lunaCarried, luna - lunaPoints);
    }

    // How many DIFFERENT things got this kind of reward in this chapter ("mom:" = things Mom saw, "luna:" = things Luna felt).
    public static int CountOf(string prefix)
    {
        int n = 0;
        foreach (string k in rewarded) if (k.StartsWith(prefix)) n++;
        return n;
    }

    public static float SecondsSinceLastAward { get { return Time.time - lastAwardTime; } }

    // A new game (Chapter 0 calls this): both bars start empty.
    // How many dreams the player has brought to Mom / to Luna in this playthrough (NightRest needs at least one).
    static int momDreams, lunaDreams;
    public static void DreamGiven(bool toMom) { if (toMom) momDreams++; else lunaDreams++; }
    public static int DreamsGiven(bool toMom) { return toMom ? momDreams : lunaDreams; }

    public static void ResetAll()
    {
        momDreams = lunaDreams = 0;
        momCarried = lunaCarried = mom = luna = 0f;
        rewarded.Clear();
        momFullShown = lunaFullShown = false;
    }

    // 1 nobody (scared away), 2 only Mom, 3 only Luna, 4 both.
    public static int PickEnding()
    {
        bool momFelt = MomFraction >= FeltAt, lunaFelt = LunaFraction >= FeltAt;
        if (momFelt && lunaFelt) return 4;
        if (momFelt) return 2;
        if (lunaFelt) return 3;
        return 1;
    }

    void Start()
    {
        mom = luna = 0f;
        rewarded.Clear();
        momShown = MomFraction;
        lunaShown = LunaFraction;
        bool family = FindAnyObjectByType<GrandmaAI>(FindObjectsInactive.Include) != null ||
                      FindAnyObjectByType<BabyAI>(FindObjectsInactive.Include) != null;
        if (!family) enabled = false;                            // nobody lives here (Chapter 0)
    }

    void Update()
    {
        momShown = Glide(momShown, MomFraction);
        lunaShown = Glide(lunaShown, LunaFraction);
        FadingHud.SetProgress(momShown, lunaShown);

        // A bar reaching full is a quiet moment of its own (the game goes on).
        if (!momFullShown && momShown >= 0.999f) { momFullShown = true; FullMoment("Mom feels you are here."); }
        if (!lunaFullShown && lunaShown >= 0.999f) { lunaFullShown = true; FullMoment("Luna feels you are here."); }
    }

    static float Glide(float shown, float real) { return Mathf.MoveTowards(shown, real, Time.deltaTime * (real < shown ? 0.6f : 0.5f)); }

    static void FullMoment(string text)
    {
        FadingHud.Toast(text, 3.5f);
        GameAudio.Play("dream_swell", 0.6f);
    }

    // The chapter is over (the next scene is loading): keep what was earned for the next chapter.
    void OnDestroy()
    {
        momCarried = Mathf.Max(0f, momCarried + mom);
        lunaCarried = Mathf.Max(0f, lunaCarried + luna);
        mom = luna = 0f;
        FadingHud.HideProgress();
    }

    // "INT_Mom_CoffeeMug" -> "coffee mug"
    public static string Pretty(string objectName)
    {
        string n = objectName.StartsWith("INT_") ? objectName.Substring(4) : objectName;
        int last = n.LastIndexOf('_');
        if (last >= 0) n = n.Substring(last + 1);
        System.Text.StringBuilder b = new System.Text.StringBuilder();
        for (int i = 0; i < n.Length; i++)
        {
            if (i > 0 && char.IsUpper(n[i])) b.Append(' ');
            b.Append(char.ToLower(n[i]));
        }
        return b.ToString();
    }
}
