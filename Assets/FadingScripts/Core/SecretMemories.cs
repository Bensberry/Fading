using UnityEngine;

// Goes in: nowhere (a static helper). Remembers (saved on this computer) which SECRET cutscenes the player has seen:
//   6 dreams (Mom's and Luna's for each of the 3 nights: you choose one per night)  +  4 endings  =  10 memories.
// The main menu shows how many are found, the nights and the ending credits remind players that there is more to find.
public static class SecretMemories
{
    public const int Total = 10;

    public static void SawDream(int night, bool mom) { Mark("fading_dream_" + night + (mom ? "_mom" : "_luna")); }
    public static void SawEnding(int ending) { Mark("fading_ending_" + ending); }

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
            return n;
        }
    }

    public static string Summary { get { return "Secret memories found:  " + Found + " / " + Total + "     (6 hidden dreams, 4 endings)"; } }

    static void Mark(string key) { PlayerPrefs.SetInt(key, 1); PlayerPrefs.Save(); }
    static bool Seen(string key) { return PlayerPrefs.GetInt(key, 0) == 1; }
}
