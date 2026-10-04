using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Goes in: nowhere by hand (it creates itself). It plays a cutscene:
//   CutsceneRunner.Play(new IntroCutscene());                                  // play it
//   CutsceneRunner.Play(new IntroCutscene(), () => Debug.Log("done"));         // and do something when it ends
// While it plays: the player is frozen, the pause menu is off, and Space skips the cutscene.
public class CutsceneRunner : MonoBehaviour
{
    public static bool IsPlaying { get; private set; }

    static CutsceneRunner instance;

    readonly List<Behaviour> frozen = new List<Behaviour>();
    bool finished;

    public static void Play(Cutscene cutscene, System.Action onFinished = null)
    {
        if (IsPlaying) return;
        if (instance == null) instance = new GameObject("CutsceneRunner").AddComponent<CutsceneRunner>();
        instance.StartCoroutine(instance.Run(cutscene, onFinished));
    }

    IEnumerator Run(Cutscene cutscene, System.Action onFinished)
    {
        IsPlaying = true;
        finished = false;
        FreezePlayer();
        CutsceneContext context = new CutsceneContext();
        yield return context.Begin();                     // hides the player and the placeholder NPCs, sets up the camera

        Coroutine steps = StartCoroutine(RunSteps(cutscene, context));
        float startTime = Time.time;
        bool skipped = false;
        while (!finished)
        {
            if (Time.time - startTime > 0.5f && SkipPressed()) { StopCoroutine(steps); skipped = true; break; }
            yield return null;
        }

        yield return context.Finish(skipped);
        UnfreezePlayer();
        IsPlaying = false;
        if (onFinished != null) onFinished();
    }

    IEnumerator RunSteps(Cutscene cutscene, CutsceneContext context)
    {
        yield return cutscene.Play(context);
        finished = true;
    }

    static bool SkipPressed()
    {
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    // ---------- the player must not move or touch things during a cutscene
    void FreezePlayer()
    {
        Freeze<FirstPersonController>();
        Freeze<PlayerInteractor>();
        Freeze<CandleHint>();
        Freeze<PrologueTutorial>();
        Freeze<LampInteraction>();
        Freeze<ClockInteraction>();
        Freeze<PhotoAlbumInteraction>();
    }

    void Freeze<T>() where T : Behaviour
    {
        foreach (T b in FindObjectsByType<T>(FindObjectsSortMode.None))
        {
            if (!b.enabled) continue;
            b.enabled = false;
            frozen.Add(b);
        }
    }

    void UnfreezePlayer()
    {
        foreach (Behaviour b in frozen) if (b != null) b.enabled = true;
        frozen.Clear();
    }

    void OnDestroy() { IsPlaying = false; }
}
