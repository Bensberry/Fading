using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom and Luna live).
// A small checklist on the goal line at the bottom of the screen, so every day has something clear to do:
//   DAY:   "Let Mom SEE 3 things (1/3)   -   Let Luna feel you 2 times (0/2)"   -> done: +4 on both bars
//   NIGHT: the night quest (NightQuest) writes its own goal.
// The numbers below make the days easier or harder.
public class ChapterGoals : MonoBehaviour
{
    const int MomGoal = 4;                             // there are more clues now
    const int LunaGoal = 3;
    const float DayBonus = 4f;                         // on BOTH bars


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
FamilyProgress.Award(FamilyProgress.Who.Both, "day-goal:" + (cycle != null ? cycle.Current.ToString() : "day"), DayBonus, 0f, "They felt you today");
            FadingHud.Toast("They felt you today.", 3.5f);
        }

        FadingHud.SetObjective(dayDone
            ? "Today's signs are done. Keep reaching them until night falls."
            : "Today:  let Mom SEE  " + mom + "/" + MomGoal + " things     -     let Luna feel you  " + luna + "/" + LunaGoal);
    }
}
