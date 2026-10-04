using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom and the baby live).
// The SCARE factor: a ghost who is too rough frightens his family, and that pushes the progress bar back.
// They get frightened when the ghost:
//   - makes too many signs at once (TooManySigns signs within SpamSeconds)
//   - touches something right next to the baby (closer than CloseToBaby metres; her own toys are fine)
//   - slams a door shut while Mom or the baby is close (DoorScareRange metres)
// A scare: the bar drops by ScarePoints at once and keeps draining for a few seconds, the baby cries, Mom panics.
// Change the numbers below to make the game kinder or harsher.
public class FamilyFear : MonoBehaviour
{
    public const float ScarePoints = 10f;            // how much the bar drops at once (the bar is full at FamilyProgress.FillPoints)
    public const float DrainPerSecond = 1.2f;        // and how fast it keeps draining while they are afraid
    const int TooManySigns = 4;
    const float SpamSeconds = 8f;
    const float CloseToBaby = 1.2f;
    const float DoorScareRange = 5f;
    const float CalmDownSeconds = 10f;
    const float MinSecondsBetweenScares = 5f;

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
            Scare("Too much at once... you frightened them.");
            return;
        }

        if (baby != null && baby.isActiveAndEnabled && !IsBabyToy(sign.transform) &&
            Flat(sign.transform.position, baby.transform.position) < CloseToBaby)
            Scare("Too close... Luna is frightened.");
    }

    void OnDoor(DoorToggle door)
    {
        if (CutsceneRunner.IsPlaying || FamilyUsingDoor || door.IsOpen) return;          // only slamming it shut is scary
        bool momNear = mom != null && mom.isActiveAndEnabled && Flat(door.transform.position, mom.transform.position) < DoorScareRange;
        bool babyNear = baby != null && baby.isActiveAndEnabled && Flat(door.transform.position, baby.transform.position) < DoorScareRange;
        if (momNear || babyNear) Scare("The slam frightened them.");
    }

    void Scare(string message)
    {
        if (Time.time - lastScare < MinSecondsBetweenScares) return;
        lastScare = Time.time;
        fear = 1f;

        FamilyProgress.AddPenalty(ScarePoints);
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
        FamilyProgress.AddPenalty(DrainPerSecond * Time.deltaTime);
        fear = Mathf.MoveTowards(fear, 0f, Time.deltaTime / CalmDownSeconds);
    }

    // The teddy, the music box and the nightlight are meant for her: touching them near her is not scary.
    bool IsBabyToy(Transform t)
    {
        return IsOrUnder(t, baby.teddy) || IsOrUnder(t, baby.windbox) || IsOrUnder(t, baby.lamp);
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
