using UnityEngine;

// INT_Hallway_MemorialCandle: the memorial candle on the hallway table.
// Touch: the flame flares up and flickers.
// 5-second rule: after exactly 5 seconds it drifts back (handled by Interactable).
// Put this script on the INT_Hallway_MemorialCandle object. Tweak the numbers in the Inspector if needed.
public class MemorialCandle : CandleFlare
{
    public override void ApplyDefaults()
    {
        prompt = "Touch the candle";
        startsLit = true;
    }
}
