using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom and the baby live).
// The "they feel you" progress bar. It adds up the points Mom and the baby earn by reacting to the ghost's signs
// (the points your friend's AI already counts) and shows them as a bar at the top of the screen.
// Scares (FamilyFear) push it back down. When the bar is full, ChapterRules plays the good ending straight away and the game ends.
// The bar CARRIES OVER: what you earned in one chapter is still there in the next (it only starts empty in a new game).
// Change FillPoints to make the bar harder (bigger) or easier (smaller). Each sign is worth about 5-10 points.
public class FamilyProgress : MonoBehaviour
{
    public const float FillPoints = 80f;

    public static event System.Action Filled;

    // Points taken away by scares (FamilyFear). It never takes away more than was earned.
    static float penalty;
    public static void AddPenalty(float points) { penalty += points; }

    // Points brought along from the chapters before this one.
    static float carried;

    // A new game (Chapter 0 calls this): the bar starts empty.
    public static void ResetAll() { carried = 0f; penalty = 0f; }

    GrandmaAI[] moms;
    BabyAI[] babies;
    float shown;                 // the bar value drawn on screen (it glides towards the real value)
    bool fired;

    void Start()
    {
        penalty = 0f;                                                    // scares of the last chapter are already counted in 'carried'
        moms = FindObjectsByType<GrandmaAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        babies = FindObjectsByType<BabyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (moms.Length == 0 && babies.Length == 0) enabled = false;       // nobody lives here (Chapter 0)
        shown = Mathf.Clamp01(carried / FillPoints);                      // the bar starts where the last chapter left it
    }

    void Update()
    {
        float earned = Points();
        penalty = Mathf.Min(penalty, carried + earned);                  // scares can eat into everything you have, but not below 0
        float real = Mathf.Clamp01(Total() / FillPoints);
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

    float Total() { return carried + Points() - penalty; }

    // The chapter is over (the next scene is loading): keep what was earned for the next chapter.
    void OnDestroy()
    {
        if (moms != null && babies != null) carried = Mathf.Max(0f, Total());
        penalty = 0f;
        FadingHud.HideProgress();
    }
}
