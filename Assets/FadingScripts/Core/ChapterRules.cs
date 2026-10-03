using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Goes in: nowhere by hand. It starts by itself in the scenes named "Chapter0" and "Chapter1"
// (other scenes are not affected), so we never have to edit the scene files.
//
// Both chapters: the player's candle gets its flame light and the hint system (H).
//
// Chapter0 (the first night, with Grandma):
//   - the short tutorial runs
//   - only Grandma's door can be opened; other doors rattle and show a message
//   - when Grandma's cutscene ends (or N is pressed), Chapter1 is loaded
//
// Chapter1 (the next morning):
//   - starts at Day 1, Grandma is gone, her room is already open
//   - every other door is unlocked too
public class ChapterRules : MonoBehaviour
{
    const string Chapter0 = "Chapter0";
    const string Chapter1 = "Chapter1";

    DayNightCycle cycle;
    bool loadingNext;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        string scene = SceneManager.GetActiveScene().name;
        if (scene != Chapter0 && scene != Chapter1) return;
        new GameObject("ChapterRules").AddComponent<ChapterRules>();
    }

    IEnumerator Start()
    {
        yield return null;          // wait one frame so doors, the candle and the day/night cycle are all set up
        cycle = FindFirstObjectByType<DayNightCycle>();

        AddCandle();
        if (SceneManager.GetActiveScene().name == Chapter0) SetUpChapter0();
        else SetUpChapter1();
    }

    void OnDestroy()
    {
        FinalCutsceneController.OnCutsceneFinished -= OnCutsceneFinished;
    }

    // ---------- the candle (both chapters)
    void AddCandle()
    {
        GameObject candle = GameObject.Find("MemorialCandle");      // the candle the player holds (the hallway one is INT_...)
        if (candle == null) candle = MakeFallbackCandle();
        if (candle != null && candle.GetComponent<CandleHint>() == null) candle.AddComponent<CandleHint>();
    }

    static GameObject MakeFallbackCandle()
    {
        if (Camera.main == null) return null;
        GameObject g = new GameObject("MemorialCandle");
        g.transform.SetParent(Camera.main.transform, false);
        g.transform.localPosition = new Vector3(0.3f, -0.25f, 0.5f);
        return g;
    }

    // ---------- Chapter 0
    void SetUpChapter0()
    {
        LockOtherDoors();
        gameObject.AddComponent<PrologueTutorial>();
        FinalCutsceneController.OnCutsceneFinished += OnCutsceneFinished;
        if (cycle != null) cycle.onPhaseChanged.AddListener(OnPhaseChanged);
    }

    void LockOtherDoors()
    {
        foreach (DoorToggle d in FindObjectsByType<DoorToggle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (d is GuestRoomDoor) d.SetLocked(false);                 // Grandma's room is the only open one
            else d.SetLocked(true, LockedMessage(d));
        }
    }

    static string LockedMessage(DoorToggle d)
    {
        if (d is FrontDoor) return "The front door won't open. I can't leave this house yet.";
        if (d is ChildRoomDoor) return "The child's door is shut tight. Not tonight...";
        if (d is MotherRoomDoor) return "Her door won't budge. Only Grandma's room is open tonight.";
        return "It won't open.";
    }

    void OnCutsceneFinished() { StartCoroutine(GoToChapter1After(2.5f)); }

    // N (testing skip) moves the cycle on to Day 1: that also means "Chapter 0 is over".
    void OnPhaseChanged(int phase)
    {
        if (phase == (int)DayNightCycle.Phase.Day1) StartCoroutine(GoToChapter1After(0.5f));
    }

    IEnumerator GoToChapter1After(float seconds)
    {
        if (loadingNext) yield break;
        loadingNext = true;
        FadingHud.Toast("Morning comes...", seconds + 1f);
        yield return new WaitForSeconds(seconds);

        if (Application.CanStreamedLevelBeLoaded(Chapter1)) SceneManager.LoadScene(Chapter1);
        else
        {
            FadingHud.Toast("Chapter1 scene is missing from File > Build Profiles (Scene List).", 6f);
            loadingNext = false;
        }
    }

    // ---------- Chapter 1
    void SetUpChapter1()
    {
        if (cycle != null) cycle.SetPhase(DayNightCycle.Phase.Day1, true);     // start from Day 1 (this also shuts all doors)
        RemoveGrandma();

        foreach (DoorToggle d in FindObjectsByType<DoorToggle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            d.SetLocked(false);
            if (d is GuestRoomDoor) d.OpenInstant();                          // her room stands open
        }
        FadingHud.SetObjective("");
    }

    // Grandma is gone: hide her and switch off the Chapter 0 puzzle that needed her.
    static void RemoveGrandma()
    {
        FinalCutsceneController cutscene = FindFirstObjectByType<FinalCutsceneController>();
        if (cutscene != null && cutscene.grannyAnimator != null) cutscene.grannyAnimator.gameObject.SetActive(false);

        foreach (Animator a in FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (a.gameObject.name.Contains("Grandmother")) a.gameObject.SetActive(false);

        foreach (LampInteraction x in FindObjectsByType<LampInteraction>(FindObjectsSortMode.None)) x.enabled = false;
        foreach (ClockInteraction x in FindObjectsByType<ClockInteraction>(FindObjectsSortMode.None)) x.enabled = false;
        foreach (PhotoAlbumInteraction x in FindObjectsByType<PhotoAlbumInteraction>(FindObjectsSortMode.None)) x.enabled = false;
        if (cutscene != null) cutscene.enabled = false;
    }
}
