using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere by hand. ChapterRules adds it in every chapter.
// "Everything Is Temporary": as the chapters pass, the family packs the house into boxes.
// The packing BOXES come from DayNightCycle (Packing_Night1/2/3). This script makes the things inside them disappear
// from the shelves, walls and floors, so the house looks emptier and emptier:
//   stage 0  Night 0              nothing packed
//   stage 1  Day 1 + Night 1      a few small things (a vase, a frame, a plant)
//   stage 2  Day 2 + Night 2      more: photo frames, the gramophone, kitchen things, plants
//   stage 3  Day 3 + Night 3      barren: every frame, rugs, bookshelves, coat rack, extra chairs and lamps
// Beds, tables, counters, the fridge and the memorial always stay. Some touchable things (INT_...) are packed too:
// a packed object is switched off completely, so it can no longer be touched. On the last day the pick-up photos go too.
// To pack something else, add its object name to the right list below (names are in the house model).
public class HouseEmptying : MonoBehaviour
{
    static readonly string[][] ItemsPackedAtStage =
    {
        new string[0],                                                                  // stage 0
        new[] { "Vase_Living", "Frame_Living_2", "Frame_Mother_1", "Plant_Hallway" },   // stage 1
        new[] { "Gramophone", "Frame_Dresser_1", "Frame_Dresser_2", "Kettle", "Plant_Kitchen",
                "Frame_Hallway_1", "Frame_Hallway_2", "Frame_Living_1", "Plant_Living",
                "INT_Hallway_Calendar", "INT_Mom_TablePhoto", "INT_Prop_WineGlass", "Prop_Books", "Prop_Trophy" },   // stage 2 (adds to stage 1)
        new[] { "Frame_Hallway_3", "Frame_Guest_1", "Frame_Guest_2", "Frame_Child_1", "Plant_Guest",
                "Rug_Runner", "Rug_Guest", "Rug_Child", "Rug_Living", "Coat_Rack",
                "Bookshelf_Guest", "Bookshelf_Child", "CoffeeTable", "ToyChest",
                "Armchair_Mother", "Armchair", "FloorLamp_Living", "Mom_PhotoBox",
                "INT_Hallway_FamilyPhoto", "INT_Child_Mobile", "INT_Prop_Globe", "INT_Prop_Present",
                "INT_Prop_Flowers", "Prop_Telescope", "Prop_Lamp" },    // stage 3 (adds to stages 1 and 2)
    };

    readonly Dictionary<string, GameObject> byName = new Dictionary<string, GameObject>();
    int shownStage = -1;

    void Start()
    {
        FadingInteractablesSetup house = FindFirstObjectByType<FadingInteractablesSetup>();
        if (house == null) return;
        foreach (Transform t in house.GetComponentsInChildren<Transform>(true))
            if (!byName.ContainsKey(t.name)) byName[t.name] = t.gameObject;

        DayNightCycle cycle = FindFirstObjectByType<DayNightCycle>();
        if (cycle == null) return;
        cycle.onPhaseChanged.AddListener(phase => Apply(StageOf(phase)));
        Apply(StageOf((int)cycle.Current));
    }

    // Night0 = 0 | Day1, Night1 = 1 | Day2, Night2 = 2 | Day3, Night3 = 3
    static int StageOf(int phase) { return (phase + 1) / 2; }

    void Apply(int stage)
    {
        if (stage == shownStage) return;
        shownStage = stage;
        for (int s = 1; s < ItemsPackedAtStage.Length; s++)
        {
            bool packed = stage >= s;
            foreach (string name in ItemsPackedAtStage[s])
            {
                GameObject g;
                if (byName.TryGetValue(name, out g) && g != null && g.activeSelf == packed) g.SetActive(!packed);
            }
        }

        // On the last day every frame is packed: so are the photos you can pick up (your friend's HoldableItem), unless one is in your hand.
        if (stage >= 3)
            foreach (HoldableItem photo in FindObjectsByType<HoldableItem>(FindObjectsSortMode.None))
                if (!photo.IsHeld) photo.gameObject.SetActive(false);
    }
}
