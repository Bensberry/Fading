using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in every chapter).
// The "they feel you" progress bar at the top of the screen.
// Points come the MOMENT something good happens (FamilyProgress.Award):
//   Mom SEES one of your signs        +10 (the same object again: +3)
//   Luna feels a sign near her         +5 (again: +1),  Luna smiles at you  +4
//   the day's goals are done          +15 (ChapterGoals)
//   a night's dream is delivered      +15 (NightQuest)
// Scares (FamilyFear) push it back. When the bar is full, ChapterRules plays the good ending straight away.
// The bar CARRIES OVER between chapters (it only starts empty in a new game).
// At the end of the last night the bar picks the ending (see PickEnding).
// Change FillPoints to make the bar harder (bigger) or easier (smaller).
public class FamilyProgress : MonoBehaviour
{
    public const float FillPoints = 100f;

    public static event System.Action Filled;

    static float carried;                                   // from the chapters before this one
    static float earned;                                    // in this chapter
    static float penalty;                                   // taken away by scares in this chapter
    static readonly HashSet<string> rewarded = new HashSet<string>();
    static float lastAwardTime = -100f;

    float shown;
    bool fired;

    public static float Fraction { get { return Mathf.Clamp01(Total / FillPoints); } }
    static float Total { get { return carried + earned - penalty; } }

    // Something good happened. 'key' says what (e.g. "mom:INT_Mom_CoffeeMug"); the first time gives 'first' points, later 'repeat'.
    public static void Award(string key, float first, float repeat, string message)
    {
        float points = rewarded.Add(key) ? first : repeat;
        if (points <= 0f) return;
        earned += points;
        lastAwardTime = Time.time;
        FadingHud.ProgressGain("+" + Mathf.RoundToInt(points) + "   " + message);
        GameAudio.Play("notice_chime", 0.55f);
    }

    public static void AddPenalty(float points) { penalty = Mathf.Min(penalty + points, carried + earned); }

    // How many DIFFERENT things got this kind of reward in this chapter ("mom:" = things Mom saw, "luna:" = things Luna felt).
    public static int CountOf(string prefix)
    {
        int n = 0;
        foreach (string k in rewarded) if (k.StartsWith(prefix)) n++;
        return n;
    }

    public static bool WasRewarded(string key) { return rewarded.Contains(key); }
    public static float SecondsSinceLastAward { get { return Time.time - lastAwardTime; } }

    // A new game (Chapter 0 calls this): the bar starts empty.
    public static void ResetAll() { carried = earned = penalty = 0f; rewarded.Clear(); }

    // 1 THE LIGHT ... 4 THE FADING, from how full the bar is at the end of the last night.
    public static int PickEnding()
    {
        float f = Fraction;
        if (f >= 0.999f) return 1;
        if (f >= 0.6f) return 2;
        if (f >= 0.3f) return 3;
        return 4;
    }

    void Start()
    {
        earned = penalty = 0f;
        rewarded.Clear();
        shown = Fraction;                                        // the bar starts where the last chapter left it
        bool family = FindAnyObjectByType<GrandmaAI>(FindObjectsInactive.Include) != null ||
                      FindAnyObjectByType<BabyAI>(FindObjectsInactive.Include) != null;
        if (!family) enabled = false;                            // nobody lives here (Chapter 0)
    }

    void Update()
    {
        float real = Fraction;
        shown = Mathf.MoveTowards(shown, real, Time.deltaTime * (real < shown ? 0.6f : 0.5f));
        FadingHud.SetProgress(shown);

        if (!fired && shown >= 1f)
        {
            fired = true;
            if (Filled != null) Filled();
        }
    }

    // The chapter is over (the next scene is loading): keep what was earned for the next chapter.
    void OnDestroy()
    {
        carried = Mathf.Max(0f, Total);
        earned = penalty = 0f;
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
