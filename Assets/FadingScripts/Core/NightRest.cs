using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom and Luna live).
// At night the ghost can REST and end the night early, beside someone he has reached:
//   stand next to sleeping Luna's bed (or Mom's) and press F (TOUCH): he lies down beside her (SleepBesideCutscene),
//   and morning comes. This is only possible once you have brought her a memory (a dream) at least once in this game.
// (The third way to rest is the bench in front of the house: see Playground / RestSpot / StarsCutscene.)
public class NightRest : MonoBehaviour
{
    const float Reach = 2.3f;
    const int DreamsNeeded = 1;

    GrandmaAI mom;
    BabyAI baby;
    bool nearMom, nearLuna;
    float nextRefusal;
    GUIStyle style;

    void Start()
    {
        mom = FindAnyObjectByType<GrandmaAI>();
        baby = FindAnyObjectByType<BabyAI>();
        if (mom == null && baby == null) enabled = false;
    }

    void Update()
    {
        nearMom = nearLuna = false;
        if (CutsceneRunner.IsPlaying || !NightQuest.Running || NightQuest.Complete || NightQuest.Carrying || Camera.main == null) return;
        Vector3 me = Camera.main.transform.position;
        nearMom = mom != null && mom.IsAsleep && Flat(me, mom.transform.position) < Reach;
        nearLuna = !nearMom && baby != null && baby.asleep && Flat(me, baby.transform.position) < Reach;
        if (!(nearMom || nearLuna) || PlayerInteractor.HasTarget || !Pressed()) return;

        if (FamilyProgress.DreamsGiven(nearMom) < DreamsNeeded)
        {
            if (Time.time < nextRefusal) return;
            nextRefusal = Time.time + 3f;
            FadingHud.Toast(nearMom ? "She doesn't feel you close enough yet... bring her a memory first."
                                    : "She doesn't know you yet... bring her a memory first.", 3.5f);
            return;
        }
        bool besideMom = nearMom;
        CutsceneRunner.Play(new SleepBesideCutscene(besideMom), NightQuest.FinishNight);   // he sleeps beside her; morning comes
    }

    static bool Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame) || MobileControls.InteractPressed;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }

    void OnGUI()
    {
        if (!(nearMom || nearLuna) || PlayerInteractor.HasTarget || CutsceneRunner.IsPlaying || PauseMenu.IsOpen) return;
        if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        style.fontSize = Mathf.Max(18, Mathf.RoundToInt(Screen.height * 0.024f));
        bool allowed = FamilyProgress.DreamsGiven(nearMom) >= DreamsNeeded;
        style.normal.textColor = allowed ? new Color(0.75f, 0.85f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.45f);
        string who = nearMom ? "Mom" : "Luna";
        string text = allowed ? "Lie down beside " + who + "  " + MobileControls.Label("F") + "   (rest until morning)"
                              : "Bring " + who + " a memory first, then you can rest beside her";
        GUI.Label(new Rect(0, Screen.height * 0.62f, Screen.width, 40f), text, style);
    }

    static float Flat(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
