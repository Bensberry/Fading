using UnityEngine;

// Goes in: nowhere (a static helper). What Mom SAYS when she notices a particular sign, and how Luna REACTS
// (she is a baby: she cannot speak yet, so her "lines" are sounds and actions in brackets).
// Matched by a word in the object's name, e.g. "CoffeeMug" in INT_Mom_CoffeeMug. Change or add lines freely.
public static class FamilyLines
{
    static readonly string[,] Mom =
    {
        { "CoffeeMug", "My coffee... I didn't leave it there." },
        { "Radio", "That song... we danced to it in this kitchen." },
        { "TablePhoto", "Our wedding photo moved... you always hated that picture." },
        { "FamilyPhoto", "The family photo... it's crooked again." },
        { "Calendar", "Three days left. I can't believe we're really leaving." },
        { "MemorialCandle", "The candle is burning brighter... is that you?" },
        { "FogWindow", "Someone wrote on the window... my name?" },
        { "MusicBox", "Luna's music box... you used to hum that song." },
        { "Mobile", "The mobile is turning... there's no wind in here." },
        { "Nightlight", "The nightlight... I'm sure I switched it off." },
        { "StuffedToy", "Her teddy fell over again." },
        { "CupOfTea", "My tea moved. You always made it for me, every night." },
        { "Flowers", "The flowers are moving. Is a window open?" },
        { "DeskFan", "Who turned the fan on?" },
        { "Globe", "You used to spin that globe and promise we'd travel." },
        { "AlarmClock", "The alarm? It's not even morning..." },
        { "CeilingFan", "The ceiling fan is turning by itself." },
        { "WineGlass", "Our anniversary glass... I can't pack it. Not yet." },
        { "Doll", "Luna, did you throw your doll again?" },
        { "RubberDuck", "Bath time already, little duck?" },
        { "ToyAirplane", "You bought her that plane. She's still too small for it." },
        { "PiggyBank", "Your old piggy bank... you were saving for her." },
        { "Present", "Her birthday present. You wrapped it before..." },
        { "Door", "Did that door just move?" },
    };

    static readonly string[,] Luna =
    {
        { "RubberDuck", "(squeaks along with the duck)" },
        { "Doll", "(reaches for her doll and hugs the air)" },
        { "ToyAirplane", "(\"vvvrrr...\" she babbles, waving her arms)" },
        { "PiggyBank", "(giggles at the rattling)" },
        { "Present", "(claps her hands at the present)" },
        { "MusicBox", "(sways to the music, eyes wide)" },
        { "Mobile", "(watches the mobile turn and coos)" },
        { "Nightlight", "(blinks at the light and smiles)" },
        { "StuffedToy", "(\"ba... ba...\" she says to her teddy)" },
    };

    static readonly string[] LunaAny =
    {
        "(giggles)", "(babbles: \"ba... ba... da?\")", "(claps her hands)", "(reaches out toward you, smiling)", "(squeals happily)", "(coos softly)",
    };

    // Mom's line for this object, or null if there is no special one.
    public static string MomLine(string objectName) { return Find(Mom, objectName); }

    // Luna's reaction to this object (a general one if there is no special one).
    public static string LunaReaction(string objectName)
    {
        string line = Find(Luna, objectName);
        return line ?? LunaAny[Random.Range(0, LunaAny.Length)];
    }

    static string Find(string[,] table, string objectName)
    {
        if (string.IsNullOrEmpty(objectName)) return null;
        for (int i = 0; i < table.GetLength(0); i++)
            if (objectName.Contains(table[i, 0])) return table[i, 1];
        return null;
    }
}
