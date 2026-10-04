using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (a static helper). Remembers how well the ghost reached the family, to pick one of the four endings.
//   Every different touchable thing the player touches (doors do not count) is one "sign".
//   The family's own reaction points (from the Mom and baby AI) add a little on top.
// The four endings, best to worst:
//   1 THE LIGHT   they felt him and let him go in peace
//   2 THE ECHO    they half felt him
//   3 THE COLD    they only sensed the cold and left
//   4 THE FADING  nobody noticed
// Change the three numbers below to make the best endings harder or easier.
public static class StoryProgress
{
    const float LightScore = 13f;       // there are 15 touchable things in the house
    const float EchoScore = 8f;
    const float ColdScore = 4f;

    static readonly HashSet<string> signs = new HashSet<string>();

    public static void Reset() { signs.Clear(); }

    public static void Touched(string objectName) { signs.Add(objectName); }

    public static int UniqueSigns { get { return signs.Count; } }

    // Signs + a tenth of the family's reaction points (only available while a chapter with the AI is loaded).
    public static float Score()
    {
        float points = 0f;
        foreach (GrandmaAI mom in Object.FindObjectsByType<GrandmaAI>(FindObjectsInactive.Include, FindObjectsSortMode.None)) points += mom.totalPoints;
        foreach (BabyAI baby in Object.FindObjectsByType<BabyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None)) points += baby.totalPoints;
        return signs.Count + points / 10f;
    }

    // 1 = THE LIGHT ... 4 = THE FADING
    public static int PickEnding()
    {
        float score = Score();
        if (score >= LightScore) return 1;
        if (score >= EchoScore) return 2;
        if (score >= ColdScore) return 3;
        return 4;
    }
}
