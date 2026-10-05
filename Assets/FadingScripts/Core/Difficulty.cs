using UnityEngine;

// Goes in: nowhere (a static helper). The difficulty the player picks after PLAY (saved for next time):
//   STORY   just the story: the bars fill fast, no scares, no lost senses, no fog
//   EASY    a few gentle scares, no lost senses, the bars fill a bit faster
//   MEDIUM  the game as designed
//   HARD    scares come easily and hurt more, the bars fill slower, thicker fog (still completable)
// Other scripts read the numbers below (FamilyProgress, FamilyFear, AbilityLoss, GameSettings).
public static class Difficulty
{
    public enum Level { Story, Easy, Medium, Hard }

    const string Key = "fading_difficulty";

    public static Level Current
    {
        get { return (Level)Mathf.Clamp(PlayerPrefs.GetInt(Key, (int)Level.Medium), 0, 3); }
        set { PlayerPrefs.SetInt(Key, (int)value); PlayerPrefs.Save(); }
    }

    // How much every reward on the bars is worth.
    public static float ProgressMultiplier { get { return Pick(1.9f, 1.35f, 1f, 0.75f); } }

    // Scares: are there any, how much they take, and how easily they happen (bigger = easier to scare them).
    public static bool Scares { get { return Current != Level.Story; } }
    public static float ScareStrength { get { return Pick(0f, 0.5f, 1f, 1.6f); } }
    public static float ScareReach { get { return Pick(0f, 0.7f, 1f, 1.45f); } }
    public static int TooManySigns { get { return Current == Level.Hard ? 3 : Current == Level.Easy ? 6 : 4; } }

    // Does the ghost lose his senses (vision, speed, hearing) chapter by chapter?
    public static bool LosesAbilities { get { return Current == Level.Medium || Current == Level.Hard; } }

    // Fog: 0 = none at all.
    public static float FogMultiplier { get { return Pick(0f, 1f, 1f, 1.35f); } }

    public static string Name(Level l) { return l.ToString().ToUpper(); }

    public static string Describe(Level l)
    {
        switch (l)
        {
            case Level.Story: return "just the story: no scares, no lost senses, no fog";
            case Level.Easy: return "gentle: a few scares, no lost senses";
            case Level.Medium: return "the way it was meant to be played";
            default: return "they scare easily, the bars fill slowly, thicker fog";
        }
    }

    static float Pick(float story, float easy, float medium, float hard)
    {
        switch (Current)
        {
            case Level.Story: return story;
            case Level.Easy: return easy;
            case Level.Hard: return hard;
            default: return medium;
        }
    }
}
