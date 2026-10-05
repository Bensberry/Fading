using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom and the baby live).
// The SCARE factor: a ghost who is too rough frightens his family, and that pushes the progress bar back.
// They get frightened when the ghost:
//   - makes too many signs at once (TooManySigns signs within SpamSeconds)
//   - touches something right next to the baby (closer than CloseToBaby metres; her own toys are fine)
//   - slams a door shut while Mom or the baby is close (DoorScareRange metres)
//   - touches something right next to Mom (StartleMom metres): she jumps
//   - touches something near them while they SLEEP (WakeRange metres): they wake up frightened
// A scare: the bars drop at once and keep draining for a few seconds, the baby cries, Mom panics.
// Scaring them too much is how the worst ending happens: they flee the house.
// Change the numbers below to make the game kinder or harsher. The DIFFICULTY (Difficulty.cs) scales them:
// Story = no scares at all, Easy = gentle, Medium = these numbers, Hard = they scare easily and it hurts more.
public class FamilyFear : MonoBehaviour
{
    public static float ScarePoints { get { return 6f * Difficulty.ScareStrength; } }        // how much the bars drop at once
    public static float DrainPerSecond { get { return 0.6f * Difficulty.ScareStrength; } }  // and how fast they keep draining
    static int TooManySigns { get { return Difficulty.TooManySigns; } }
    const float SpamSeconds = 9f;
    static float CloseToBaby { get { return 1.5f * Difficulty.ScareReach; } }
    static float StartleMom { get { return 1.5f * Difficulty.ScareReach; } }
    static float WakeRange { get { return 3f * Difficulty.ScareReach; } }
    static float DoorScareRange { get { return 6f * Difficulty.ScareReach; } }
    const float CalmDownSeconds = 10f;
    const float MinSecondsBetweenScares = 4f;

    // True while FamilyLife opens a door for Mom or the baby (that is not a scare).
    public static bool FamilyUsingDoor;

    readonly List<float> recentSigns = new List<float>();
    float fear;                                        // 1 = just frightened, falls back to 0
    float lastScare = -100f;
    GrandmaAI mom;
    BabyAI baby;

    void Start()
    {
        mom = FindFirstObjectByType<GrandmaAI>();
        baby = FindFirstObjectByType<BabyAI>();
        if (mom == null && baby == null) { enabled = false; return; }

        foreach (Interactable i in FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Interactable touched = i;
            DoorToggle door = i as DoorToggle;
            if (door != null) door.onInteract.AddListener(() => OnDoor(door));
            else i.onInteract.AddListener(() => OnSign(touched));
        }
    }

    void OnSign(Interactable sign)
    {
        if (CutsceneRunner.IsPlaying) return;

        recentSigns.Add(Time.time);
        recentSigns.RemoveAll(t => Time.time - t > SpamSeconds);
        if (recentSigns.Count >= TooManySigns)
        {
            recentSigns.Clear();
            Scare("Too much at once... you frightened them.", ScarePoints, ScarePoints);
            return;
        }

        Vector3 at = sign.transform.position;
        bool momHere = mom != null && mom.isActiveAndEnabled;
        bool babyHere = baby != null && baby.isActiveAndEnabled;

        // While they sleep: anything close wakes them, frightened.
        if ((momHere && mom.IsAsleep && Flat(at, mom.transform.position) < WakeRange) ||
            (babyHere && baby.asleep && Flat(at, baby.transform.position) < WakeRange))
        {
            Scare("You woke them... they are frightened.", ScarePoints, ScarePoints);
            return;
        }
        if (babyHere && !IsBabyToy(sign.transform) && Flat(at, baby.transform.position) < CloseToBaby)
        {
            Scare("Too close... Luna is frightened.", ScarePoints * 0.5f, ScarePoints * 1.3f);
            return;
        }
        if (momHere && !mom.IsAsleep && Flat(at, mom.transform.position) < StartleMom)
            Scare("Too close... you startled Mom.", ScarePoints * 1.3f, ScarePoints * 0.4f);
    }

    void OnDoor(DoorToggle door)
    {
        if (CutsceneRunner.IsPlaying || FamilyUsingDoor || door.IsOpen) return;          // only slamming it shut is scary
        bool momNear = mom != null && mom.isActiveAndEnabled && Flat(door.transform.position, mom.transform.position) < DoorScareRange;
        bool babyNear = baby != null && baby.isActiveAndEnabled && Flat(door.transform.position, baby.transform.position) < DoorScareRange;
        if (momNear || babyNear) Scare("The slam frightened them.", ScarePoints, ScarePoints);
    }

    void Scare(string message, float momPoints, float lunaPoints)
    {
        if (!Difficulty.Scares) return;                                     // Story: nothing ever frightens them
        if (Time.time - lastScare < MinSecondsBetweenScares) return;
        lastScare = Time.time;
        fear = 1f;

        FamilyProgress.AddPenalty(momPoints, lunaPoints);
        FadingHud.ProgressScare();
        FadingHud.Toast(message, 3f);
        GameAudio.Play("scare_sting", 0.7f);

        BabyLife babyLife = FindFirstObjectByType<BabyLife>();
        if (babyLife != null) babyLife.Cry();
        MomLife momLife = FindFirstObjectByType<MomLife>();
        if (momLife != null) momLife.Frightened();
    }

    void Update()
    {
        if (fear <= 0f) return;
        FamilyProgress.AddPenalty(DrainPerSecond * Time.deltaTime, DrainPerSecond * Time.deltaTime);
        fear = Mathf.MoveTowards(fear, 0f, Time.deltaTime / CalmDownSeconds);
    }

    // The teddy, the music box and the nightlight are meant for her: touching them near her is not scary.
    static readonly string[] LunasThings = { "Doll", "RubberDuck", "ToyAirplane", "PiggyBank", "Present", "MusicBox", "Mobile", "Nightlight", "StuffedToy" };

    bool IsBabyToy(Transform t)
    {
        if (IsOrUnder(t, baby.teddy) || IsOrUnder(t, baby.windbox) || IsOrUnder(t, baby.lamp)) return true;
        foreach (string toy in LunasThings) if (t.name.Contains(toy)) return true;
        return false;
    }

    static bool IsOrUnder(Transform t, Transform toy)
    {
        return toy != null && (t == toy || t.IsChildOf(toy) || toy.IsChildOf(t));
    }

    static float Flat(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
