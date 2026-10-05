using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom and the baby live).
// Small things that make the house feel alive (Mom's and Luna's own routines are in MomLife and BabyLife):
//   - doors open by themselves when Mom or the baby comes up to them, and close again behind them
//   - FamilyLife.Say(...) shows what Mom says as a subtitle, but only when the player is close enough to hear it,
//     and never too often
//   - when the ghost touches something, Luna turns to look at it (she can feel him)
//   - soft dust floats in the air around the player
public class FamilyLife : MonoBehaviour
{
    const float HearingDistance = 11f;          // the player hears Mom's words within this distance
    const float SecondsBetweenLines = 9f;

    static float nextLine;

    GrandmaAI mom;
    BabyAI baby;
    readonly HashSet<DoorToggle> openedForFamily = new HashSet<DoorToggle>();

    IEnumerator Start()
    {
        nextLine = 0f;
        yield return new WaitForSeconds(0.5f);                   // the AI scripts have started by now
        mom = FindFirstObjectByType<GrandmaAI>();
        baby = FindFirstObjectByType<BabyAI>();
        MakeDust();
        StartCoroutine(DoorsForTheFamily());
    }

    // ---------- talking
    // A line from Mom (or the ghost's thought when speaker is ""), only if the player is near 'where'.
    public static void Say(string speaker, string line, Vector3 where, float maxDistance = HearingDistance)
    {
        if (CutsceneRunner.IsPlaying || Time.time < nextLine || Camera.main == null) return;
        if (Vector3.Distance(Camera.main.transform.position, where) > maxDistance) return;
        nextLine = Time.time + SecondsBetweenLines;
        string voice = GameAudio.VoiceFor(line);                          // the spoken line (Assets/Resources/Audio/voice_line_...)
        FadingHud.Subtitle(speaker, line, Mathf.Max(3.5f, GameAudio.Length(voice) + 0.5f));
        if (line.StartsWith("(")) return;                                 // Luna's reactions are actions, not words
        if (speaker == "") GameAudio.Play(voice, 0.9f);                   // the ghost's own thoughts: in your head
        else GameAudio.PlayVoiceAt(voice, where + Vector3.up * 1.5f, 1f); // Mom: from where she is
    }

    // ---------- doors
    IEnumerator DoorsForTheFamily()
    {
        DoorToggle[] doors = FindObjectsByType<DoorToggle>(FindObjectsSortMode.None);
        while (true)
        {
            foreach (DoorToggle door in doors)
            {
                if (door == null || door.locked) continue;
                float nearest = Mathf.Min(FlatDistance(mom, door), FlatDistance(baby, door));
                if (nearest < 1.8f && !door.IsOpen) Use(door, true);
                else if (nearest > 3.5f && door.IsOpen && openedForFamily.Contains(door)) Use(door, false);
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    void Use(DoorToggle door, bool opening)
    {
        FamilyFear.FamilyUsingDoor = true;
        door.TryInteract();
        FamilyFear.FamilyUsingDoor = false;
        if (opening) openedForFamily.Add(door);
        else openedForFamily.Remove(door);
    }

    static float FlatDistance(Component who, Component door)
    {
        if (who == null || !who.gameObject.activeInHierarchy) return float.MaxValue;
        Vector3 d = who.transform.position - door.transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    // Called by FamilyReactions when the ghost touches something: Luna turns to look at it (if she is near).
    // Luna (awake and within 8 m) feels it: she turns to look, smiles, and that counts on the bar.
    // A moment later, if NOBODY reacted, a small line tells the player (so they learn that Mom must SEE things).
    public void ReactTo(Transform touched)
    {
        if (CutsceneRunner.IsPlaying || touched == null) return;
        Vector3 where = touched.position;
        if (baby != null && !baby.asleep && baby.isActiveAndEnabled && Vector3.Distance(baby.transform.position, where) <= 8f)
        {
            FamilyGaze gaze = baby.GetComponent<FamilyGaze>();
            if (gaze != null) gaze.LookAt(where);
            BabyLife babyLife = GetComponent<BabyLife>();
            if (babyLife != null) babyLife.Delight();
            Say("LUNA", FamilyLines.LunaReaction(touched.name), baby.transform.position);
FamilyProgress.Award(FamilyProgress.Who.Luna, "luna:" + touched.name, 5f, 2f, "Luna felt you near the " + FamilyProgress.Pretty(touched.name));
        }
        StartCoroutine(TellIfNobodySaw(touched.name));
    }

    float nextNobodyLine;

    IEnumerator TellIfNobodySaw(string objectName)
    {
        yield return new WaitForSeconds(3.5f);
        if (FamilyProgress.SecondsSinceLastAward < 3.5f) yield break;          // somebody reacted
        if (Time.time < nextNobodyLine || CutsceneRunner.IsPlaying) yield break;
        nextNobodyLine = Time.time + 15f;
        bool asleep = (mom != null && mom.IsAsleep) || (baby != null && baby.asleep);
        FadingHud.Toast(asleep ? "They are asleep... (tonight: find the memory lights)" : "No one saw that. Let Mom SEE it.", 2.5f);
    }

    // ---------- floating dust
    void MakeDust()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null || Camera.main == null) return;

        GameObject g = new GameObject("DustMotes");
        g.transform.SetParent(Camera.main.transform, false);
        ParticleSystem ps = g.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = 8f;
        main.startSpeed = 0.05f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
        main.startColor = new Color(1f, 0.93f, 0.8f, 0.35f);
        main.maxParticles = 70;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.002f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 9f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(7f, 3f, 7f);
        shape.position = new Vector3(0f, 0f, 2.5f);

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 0.4f;

        ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
        fade.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;

        ParticleSystemRenderer renderer = g.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(shader);
        ps.Play();
    }
}
