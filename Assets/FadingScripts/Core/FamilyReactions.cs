using UnityEngine;

// Goes in: nowhere (ChapterRules calls Run() when a chapter starts).
// Makes Mom and the baby REACT to what the ghost does. Their AI watches "ClueGoal" components: a clue is "active" while
// something has just happened to its object. This links the ghost's touches to those clues:
//   - touching an object switches its clue on, and the clue switches off again when the object has returned (about 6 seconds)
//     (this was missing for Mom's mug and radio, and the teddy's clue never switched off, so the baby kept waiting there)
//   - touchable objects that had no clue get one, and Mom's list of things to watch gets all of them
// Objects whose own script already runs the clue (the nightlight and the music box) are left alone.
// The baby also turns to LOOK at whatever was touched nearby (FamilyGaze); Mom only reacts to what she SEES (MomLife). Doors do not count as signs.
public static class FamilyReactions
{
    public static void Run()
    {
        GrandmaAI[] moms = Object.FindObjectsByType<GrandmaAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        BabyAI[] babies = Object.FindObjectsByType<BabyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (moms.Length == 0 && babies.Length == 0) return;                 // nobody to react in this scene

        foreach (GrandmaAI mom in moms) if (mom.GetComponent<FamilyGaze>() == null) mom.gameObject.AddComponent<FamilyGaze>();
        foreach (BabyAI baby in babies) if (baby.GetComponent<FamilyGaze>() == null) baby.gameObject.AddComponent<FamilyGaze>();

        foreach (Interactable sign in Object.FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sign is DoorToggle) continue;

            Transform touched = sign.transform;                                     // everybody looks at what was touched
            sign.onInteract.AddListener(() => LookAt(touched.position));

            if (sign is LightSign || sign is MusicBoxInteraction) continue;      // these run their own clue

            ClueGoal clue = sign.GetComponent<ClueGoal>();
            if (clue == null)
            {
                clue = sign.gameObject.AddComponent<ClueGoal>();
                clue.points = 5;
                foreach (GrandmaAI mom in moms)
                    if (mom.clues != null && !mom.clues.Contains(clue)) mom.clues.Add(clue);
            }

            ClueGoal linked = clue;
            sign.onInteract.AddListener(() => linked.ActivateClue());
            sign.onReturned.AddListener(() => linked.DeactivateClue());
        }
    }

    static void LookAt(Vector3 where)
    {
        FamilyLife life = Object.FindFirstObjectByType<FamilyLife>();
        if (life != null) life.ReactTo(where);
    }
}
