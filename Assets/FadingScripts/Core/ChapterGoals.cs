using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom and Luna live).
// A small checklist on the goal line at the bottom of the screen, so every day has something clear to do:
//   DAY:   "Let Mom SEE 3 things (1/3)   -   Let Luna feel you 2 times (0/2)"   -> done: +15 on the bar
//   NIGHT: the night quest (NightQuest) writes its own goal.
// The numbers below make the days easier or harder.
public class ChapterGoals : MonoBehaviour
{
    const int MomGoal = 3;
    const int LunaGoal = 2;
    const float DayBonus = 15f;
    const float NightComesAfter = 25f;                  // seconds after the day's goals are done

    public static bool Active;          // ChapterRules switches it on after the chapter's first message

    DayNightCycle cycle;
    bool dayDone;
    float nextRefresh;

    void Start()
    {
        Active = false;
        cycle = FindAnyObjectByType<DayNightCycle>();
        if (cycle != null) cycle.onPhaseChanged.AddListener(delegate { dayDone = false; });
    }

    System.Collections.IEnumerator BringTheNight(DayNightCycle.Phase day)
    {
        yield return new WaitForSeconds(NightComesAfter);
        while (CutsceneRunner.IsPlaying) yield return null;
        if (cycle != null && cycle.Current == day) cycle.AdvancePhase();      // still the same day: the night begins
    }

    void Update()
    {
        if (!Active || Time.time < nextRefresh || CutsceneRunner.IsPlaying) return;
        nextRefresh = Time.time + 0.4f;
        if (cycle != null && cycle.IsNight) return;                         // the night quest owns the goal line at night

        int mom = Mathf.Min(MomGoal, FamilyProgress.CountOf("mom:"));
        int luna = Mathf.Min(LunaGoal, FamilyProgress.CountOf("luna:"));
        if (!dayDone && mom >= MomGoal && luna >= LunaGoal)
        {
            dayDone = true;
            FamilyProgress.Award("day-goal:" + (cycle != null ? cycle.Current.ToString() : "day"), DayBonus, 0f, "They felt you today");
            FadingHud.Toast("They felt you today. Night will come soon...", 3.5f);
            StartCoroutine(BringTheNight(cycle != null ? cycle.Current : DayNightCycle.Phase.Day1));
        }

        FadingHud.SetObjective(dayDone
            ? "Today's signs are done. Keep reaching them, or wait for the night."
            : "Today:  let Mom SEE  " + mom + "/" + MomGoal + " things     -     let Luna feel you  " + luna + "/" + LunaGoal);
    }
}
