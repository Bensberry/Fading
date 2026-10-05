using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Goes in: nowhere by hand. ChapterRules adds it in Chapter 0 (the first night) only.
// A short guided start (the prologue). Each step shows one line at the bottom of the screen
// and moves on when the player does it:
//   1 move (WASD)  ->  2 run (Shift)  ->  3 look around  ->  4 touch something (F)  ->  5 use the candle (H)
// then the last line stays on screen as the goal for the night. Press Tab to cut the tutorial off completely.
public class PrologueTutorial : MonoBehaviour
{
    const string FinalGoal = "Tonight only Grandma's room is open. Get her attention: flicker her lamp, touch the clock, then the photo album.";

    int step;
    float stepStartTime, lookedPixels;
    bool touchedSomething, usedHint, finished;

    void Start()
    {
        foreach (Interactable i in FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            i.onInteract.AddListener(() => touchedSomething = true);
        CandleHint.HintUsed += OnHintUsed;
        FinalCutsceneController.OnCutsceneStarted += Finish;
        ShowStep();
    }

    void OnDestroy()
    {
        CandleHint.HintUsed -= OnHintUsed;
        FinalCutsceneController.OnCutsceneStarted -= Finish;
    }

    void OnHintUsed() { usedHint = true; }

    void Update()
    {
        if (finished) return;
        if (SkipPressed()) { Finish(); return; }

        // Wait a moment before the first step so the chapter title can show.
        if (Time.timeSinceLevelLoad < 4f) return;

        lookedPixels += MouseMovement();
        if (StepDone() && Time.time - stepStartTime > 1f) NextStep();
    }

    bool StepDone()
    {
        switch (step)
        {
            case 0: return AnyKey(Key.W) || AnyKey(Key.A) || AnyKey(Key.S) || AnyKey(Key.D) || MobileControls.Move.magnitude > 0.3f;
            case 1: return AnyKey(Key.LeftShift) || MobileControls.Running;
            case 2: return lookedPixels > 600f;
            case 3: return touchedSomething;
            case 4: return usedHint;
            default: return false;
        }
    }

    void NextStep()
    {
        step++;
        if (step > 4) { GoToFinalGoal(); return; }
        ShowStep();
    }

    void ShowStep()
    {
        stepStartTime = Time.time;
        MobileControls.TutorialShowing = true;
        if (MobileControls.Active) { ShowTouchStep(); return; }
        string tab = "      [Tab] skip tutorial";
        switch (step)
        {
            case 0: FadingHud.SetObjective("You are a ghost. Move around with  W A S D." + tab, true); break;
            case 1: FadingHud.SetObjective("Hold  Left Shift  to move faster." + tab, true); break;
            case 2: FadingHud.SetObjective("Hold the RIGHT MOUSE button and move the mouse to look around." + tab, true); break;
            case 3: FadingHud.SetObjective("Look at an object and press  F  to touch it. It drifts back after 5 seconds." + tab, true); break;
            case 4:
                FadingHud.CandleHudAllowed = true;                     // the candle display appears when the candle is explained
                FadingHud.SetObjective("Your candle can show you what to touch next. Press  H.  It burns low after each use." + tab, true);
                break;
        }
    }

    // The same steps for a phone (touch controls, see MobileControls).
    void ShowTouchStep()
    {
        switch (step)
        {
            case 0: FadingHud.SetObjective("You are a ghost. Put your thumb on the LEFT side of the screen and move it to walk.", true); break;
            case 1: FadingHud.SetObjective("Tap  RUN  to move faster (tap again to walk).", true); break;
            case 2: FadingHud.SetObjective("Drag on the RIGHT side of the screen to look around.", true); break;
            case 3: FadingHud.SetObjective("Put the crosshair on an object and tap the left side of the screen to touch it.  It drifts back after 5 seconds.", true); break;
            case 4:
                FadingHud.CandleHudAllowed = true;
                FadingHud.SetObjective("Your candle can show you what to touch next. Tap  HINT.  It burns low after each use.", true);
                break;
        }
    }

    void GoToFinalGoal()
    {
        step = 99;
        MobileControls.TutorialShowing = false;
        FadingHud.CandleHudAllowed = true;
        FadingHud.SetObjective(FinalGoal);
        FadingHud.Toast("Prologue: the family is asleep. They must notice you before the nights run out.", 5f);
    }

    void Finish()
    {
        finished = true;
        MobileControls.TutorialShowing = false;
        FadingHud.CandleHudAllowed = true;
        FadingHud.SetObjective("");
    }

    // ---------- input helpers
    static bool AnyKey(Key key)
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current[key].isPressed;
#else
        return false;
#endif
    }

    static bool SkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) || MobileControls.TutorialSkipPressed;
#else
        return Input.GetKeyDown(KeyCode.Tab);
#endif
    }

    static float MouseMovement()
    {
#if ENABLE_INPUT_SYSTEM
        float mouse = (Mouse.current != null && Mouse.current.rightButton.isPressed) ? Mouse.current.delta.ReadValue().magnitude : 0f;
        return mouse + MobileControls.LookPixels;
#else
        return 0f;
#endif
    }
}
