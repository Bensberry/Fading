using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Goes in: nowhere by hand. It starts by itself in the scenes named "Chapter0", "Chapter1", "Chapter2" and "Chapter3"
// (other scenes are not affected), so we never have to edit the scene files.
//
// All chapters: the player's candle gets its flame light and the hint system (H); Esc opens the pause menu;
//               the house packs itself away (HouseEmptying) and the ghost loses an ability (AbilityLoss).
//
// Chapter0 = Night 0 (the only night with Grandma):
//   - the short tutorial runs
//   - only Grandma's door can be opened; other doors rattle and show a message
//   - the intro cutscene plays first, then the tutorial runs
//   - when Grandma's cutscene ends, the Chapter 0 ending cutscene plays and then Chapter1 is loaded (N skips straight to Chapter1)
//
// Chapter1 = Day 1 + Night 1,  Chapter2 = Day 2 + Night 2,  Chapter3 = Day 3 + the last night:
//   - Grandma is gone, her room stands open, every other door is unlocked too
//   - each chapter starts in its own day; when the next day begins (N key for now) the next chapter loads
//   - Chapter1: when Night 1 begins, the bedroom cutscene plays.   Chapter2: the living room cutscene plays at the start of Day 2.
//   - Chapter3 is the end of the game: after the last night (2 minutes, or N) the ending cutscene plays and the main menu loads
public class ChapterRules : MonoBehaviour
{
    const int LastChapter = 3;
    const float LastNightSeconds = 120f;                 // how long the last night lasts before the ending (N skips the wait)
    const string MainMenuScene = "MainMenu";

    DayNightCycle cycle;
    int chapter;
    bool loadingNext, nightOnePlayed, endingStarted;

    // Unity runs this start-up hook only ONCE (for the first scene), so we listen for every scene load instead.
    // That way it also works when the game is started from the main menu.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Also run once for the very first scene (when you press Play directly in a chapter). Safe to run twice.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RunForFirstScene()
    {
        TryCreate(SceneManager.GetActiveScene().name);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene.name);
    }

    static void TryCreate(string sceneName)
    {
        if (ChapterNumber(sceneName) < 0) return;
        NpcModelSetup.Run();                           // hook Mom's model to her AI before the NPCs start (safe to run twice)
        if (FindFirstObjectByType<ChapterRules>() != null) return;        // already running
        LightFadeIn.StartIfPending();                  // coming from the main menu: start in the blinding light and fade out of it
        new GameObject("ChapterRules").AddComponent<ChapterRules>();
    }

    // "Chapter2" -> 2.   Anything that is not a chapter scene -> -1.
    static int ChapterNumber(string sceneName)
    {
        for (int i = 0; i <= LastChapter; i++)
            if (sceneName == ChapterName(i)) return i;
        return -1;
    }

    static string ChapterName(int number) { return "Chapter" + number; }

    // The day/night phase each chapter starts in.
    static DayNightCycle.Phase StartPhase(int number)
    {
        switch (number)
        {
            case 0: return DayNightCycle.Phase.Night0;
            case 1: return DayNightCycle.Phase.Day1;
            case 2: return DayNightCycle.Phase.Day2;
            default: return DayNightCycle.Phase.Day3;
        }
    }

    IEnumerator Start()
    {
        yield return null;          // wait one frame so doors, the candle and the day/night cycle are all set up
        chapter = ChapterNumber(SceneManager.GetActiveScene().name);
        cycle = FindFirstObjectByType<DayNightCycle>();
        if (cycle != null)
        {
            cycle.UseStoryLook();                      // clear day/night difference, slow calm changes
            cycle.SetPhase(cycle.Current, true);       // show the new look straight away
        }

        GameSettings.CaptureSceneDefaults();
        AddCandle();
        if (chapter == 0) SetUpChapter0();
        else SetUpLaterChapter();

        gameObject.AddComponent<HouseEmptying>();      // boxes appear, things on shelves and walls disappear
        AbilityLoss.StartFor(gameObject, chapter);     // vision, then speed, then hearing
        gameObject.AddComponent<PauseMenu>();          // Esc opens the pause menu
        GameSettings.ApplyAll();                       // the player's saved fog / lighting / sensitivity
    }

    void OnDestroy()
    {
        FinalCutsceneController.OnCutsceneFinished -= OnCutsceneFinished;
    }

    // ---------- the candle (all chapters)
    void AddCandle()
    {
        GameObject candle = GameObject.Find("MemorialCandle");      // the candle the player holds (the hallway one is INT_...)
        if (candle == null) candle = MakeFallbackCandle();
        if (candle == null) return;

        // The held candle must not collide with anything: its collider rides in front of the player
        // and can shove the player's body around (that made the player pop up into the air).
        foreach (Collider c in candle.GetComponentsInChildren<Collider>(true)) c.enabled = false;

        if (candle.GetComponent<CandleHint>() == null) candle.AddComponent<CandleHint>();
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
    // Grandma's puzzle is easier: the zones where F works around the lamp, clock and album are bigger,
    // and the "look away" windows are a bit longer. (These are the numbers saved in the scene, changed here when the chapter starts.)
    const float PuzzleZoneScale = 1.8f;
    const float LampWindowSeconds = 8f;       // was 5.8
    const float ClockWindowSeconds = 4.5f;    // was 2.5

    void MakeGrandmaPuzzleEasier()
    {
        foreach (LampInteraction x in FindObjectsByType<LampInteraction>(FindObjectsSortMode.None))
        {
            x.lampLookAnimDuration = LampWindowSeconds;
            GrowTriggerZones(x.gameObject);
        }
        foreach (ClockInteraction x in FindObjectsByType<ClockInteraction>(FindObjectsSortMode.None))
        {
            x.clockLookAnimDuration = ClockWindowSeconds;
            GrowTriggerZones(x.gameObject);
        }
        foreach (PhotoAlbumInteraction x in FindObjectsByType<PhotoAlbumInteraction>(FindObjectsSortMode.None))
            GrowTriggerZones(x.gameObject);
    }

    static void GrowTriggerZones(GameObject g)
    {
        foreach (Collider c in g.GetComponents<Collider>())
        {
            if (!c.isTrigger) continue;
            BoxCollider box = c as BoxCollider;
            SphereCollider sphere = c as SphereCollider;
            CapsuleCollider capsule = c as CapsuleCollider;
            if (box != null) box.size *= PuzzleZoneScale;
            else if (sphere != null) sphere.radius *= PuzzleZoneScale;
            else if (capsule != null) capsule.radius *= PuzzleZoneScale;
        }
    }

    void SetUpChapter0()
    {
        MakeGrandmaPuzzleEasier();
        LockOtherDoors();
        StartCoroutine(OpeningCutscene());
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

    // The intro cutscene plays a moment after the scene starts (after the white fade from the main menu), then the tutorial begins.
    IEnumerator OpeningCutscene()
    {
        yield return new WaitForSeconds(3.5f);
        CutsceneRunner.Play(new IntroCutscene(), AddTutorial);
    }

    void AddTutorial()
    {
        if (this != null && GetComponent<PrologueTutorial>() == null) gameObject.AddComponent<PrologueTutorial>();
    }

    // Grandma's puzzle cutscene (your friend's) is over: the Chapter 0 ending plays, then the next chapter loads.
    void OnCutsceneFinished()
    {
        CutsceneRunner.Play(new ChapterZeroEndCutscene(), () => StartCoroutine(GoToNextChapterAfter(0.5f)));
    }

    // ---------- Chapters 1, 2, 3
    void SetUpLaterChapter()
    {
        if (cycle != null) cycle.SetPhase(StartPhase(chapter), true);     // start in this chapter's day (this also shuts all doors)
        RemoveGrandma();

        foreach (DoorToggle d in FindObjectsByType<DoorToggle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            d.SetLocked(false);
            if (d is GuestRoomDoor) d.OpenInstant();                          // her room stands open
        }
        FadingHud.SetObjective("");

        if (cycle != null) cycle.onPhaseChanged.AddListener(OnPhaseChanged);
        if (chapter == 2) StartCoroutine(PlayAfter(3f, new DayTwoCutscene()));
    }

    IEnumerator PlayAfter(float seconds, Cutscene cutscene)
    {
        yield return new WaitForSeconds(seconds);
        CutsceneRunner.Play(cutscene);
    }

    // ---------- moving on to the next chapter
    // When the NEXT chapter's starting phase begins (the N testing key for now), this chapter is over.
    void OnPhaseChanged(int phase)
    {
        // Story cutscenes that start with a phase.
        if (chapter == 1 && phase == (int)DayNightCycle.Phase.Night1 && !nightOnePlayed)
        {
            nightOnePlayed = true;
            CutsceneRunner.Play(new NightOneCutscene());
        }
        if (chapter == LastChapter && phase == (int)DayNightCycle.Phase.Night3 && !endingStarted)
        {
            endingStarted = true;
            StartCoroutine(EndingAfterTheLastNight());
        }

        if (chapter >= LastChapter) return;                                    // the last chapter leads to the ending, not to a scene
        if (phase == (int)StartPhase(chapter + 1)) StartCoroutine(GoToNextChapterAfter(0.5f));
    }

    // The last night lasts LastNightSeconds (press N to skip the wait), then the ending plays and the main menu loads.
    IEnumerator EndingAfterTheLastNight()
    {
        float waited = 0f;
        while (waited < LastNightSeconds && !(Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame))
        {
            waited += Time.deltaTime;
            yield return null;
        }
        while (CutsceneRunner.IsPlaying) yield return null;
        CutsceneRunner.Play(new EndingCutscene(), () => SceneManager.LoadScene(MainMenuScene));
    }

    IEnumerator GoToNextChapterAfter(float seconds)
    {
        if (loadingNext || chapter >= LastChapter) yield break;
        loadingNext = true;
        string next = ChapterName(chapter + 1);
        FadingHud.Toast(chapter == 0 ? "Morning comes..." : "Another day begins...", seconds + 1f);
        yield return new WaitForSeconds(seconds);

        if (Application.CanStreamedLevelBeLoaded(next)) SceneManager.LoadScene(next);
        else
        {
            FadingHud.Toast(next + " scene is missing from File > Build Profiles (Scene List).", 6f);
            loadingNext = false;
        }
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
