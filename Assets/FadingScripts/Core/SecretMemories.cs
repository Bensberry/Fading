using UnityEngine;

// Goes in: nowhere (a static helper). Remembers (saved on this computer) which SECRET cutscenes the player has seen:
//   6 dreams (Mom's and Luna's for each of the 3 nights: you choose one per night)
//   3 quiet nights (resting under the stars, watching over Mom, watching over Luna)
//   4 endings                                                                          =  13 memories.
// The main menu shows how many are found, the nights and the ending credits remind players that there is more to find.
public static class SecretMemories
{
    public const int Total = 13;

    public static void SawDream(int night, bool mom) { Mark("fading_dream_" + night + (mom ? "_mom" : "_luna")); }
    public static void SawEnding(int ending) { Mark("fading_ending_" + ending); }
    public static void SawRest(string which) { Mark("fading_rest_" + which); }      // "stars", "mom", "luna"

    public static int Found
    {
        get
        {
            int n = 0;
            for (int night = 1; night <= 3; night++)
            {
                if (Seen("fading_dream_" + night + "_mom")) n++;
                if (Seen("fading_dream_" + night + "_luna")) n++;
            }
            for (int e = 1; e <= 4; e++) if (Seen("fading_ending_" + e)) n++;
            foreach (string r in new[] { "stars", "mom", "luna" }) if (Seen("fading_rest_" + r)) n++;
            return n;
        }
    }

    public static string Summary { get { return "Secret memories found:  " + Found + " / " + Total +"     (6 dreams, 3 quiet nights, 4 endings)"; } }

    static void Mark(string key) { PlayerPrefs.SetInt(key, 1); PlayerPrefs.Save(); }
    static bool Seen(string key) { return PlayerPrefs.GetInt(key, 0) == 1; }
}
