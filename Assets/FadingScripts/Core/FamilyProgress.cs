using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom and the baby live).
// The "they feel you" progress bar. It adds up the points Mom and the baby earn by reacting to the ghost's signs
// (the points your friend's AI already counts) and shows them as a bar at the top of the screen.
// Scares (FamilyFear) push it back down. When the bar is full, ChapterRules plays the good ending straight away and the game ends.
// Change FillPoints to make the bar harder (bigger) or easier (smaller). Each sign is worth about 5-10 points.
public class FamilyProgress : MonoBehaviour
{
    public const float FillPoints = 50f;

    public static event System.Action Filled;

    // Points taken away by scares (FamilyFear). It never takes away more than was earned.
    static float penalty;
    public static void AddPenalty(float points) { penalty += points; }

    GrandmaAI[] moms;
    BabyAI[] babies;
    float shown;                 // the bar value drawn on screen (it glides towards the real value)
    bool fired;

    void Start()
    {
        penalty = 0f;                                                    // every chapter starts with an empty bar
        moms = FindObjectsByType<GrandmaAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        babies = FindObjectsByType<BabyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (moms.Length == 0 && babies.Length == 0) enabled = false;       // nobody lives here (Chapter 0)
    }

    void Update()
    {
        float earned = Points();
        penalty = Mathf.Min(penalty, earned);
        float real = Mathf.Clamp01((earned - penalty) / FillPoints);
        shown = Mathf.MoveTowards(shown, real, Time.deltaTime * (real < shown ? 0.6f : 0.35f));
        FadingHud.SetProgress(shown);

        if (!fired && shown >= 1f)
        {
            fired = true;
            if (Filled != null) Filled();
        }
    }

    float Points()
    {
        float total = 0f;
        foreach (GrandmaAI mom in moms) if (mom != null) total += mom.totalPoints;
        foreach (BabyAI baby in babies) if (baby != null) total += baby.totalPoints;
        return total;
    }

    void OnDestroy() { FadingHud.HideProgress(); }
}
