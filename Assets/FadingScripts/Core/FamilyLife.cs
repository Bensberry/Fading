using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom and the baby live).
// Makes the house feel alive:
//   - Mom spends a lot of her time in HER ROOM (a spot in "MotherRoom" is added to her walking list), starts the chapter
//     there, and opens her bedroom door when she walks up to it.
//   - Mom and the baby make small sounds and comments now and then ("Did you hear that?", a baby giggle).
//   - Soft dust floats in the air around the player.
public class FamilyLife : MonoBehaviour
{
    public int roomVisitsInList = 3;              // how many times her room is in Mom's list of places (more = more time there)

    GrandmaAI mom;
    BabyAI baby;
    Transform roomSpot;
    DoorToggle momDoor;
    bool weOpenedTheDoor;
    float nextTalk;

    static readonly string[] MomLinesNoticing =
    {
        "Did you hear that?", "Hm? Who is there?", "That's strange...", "I felt something just now.", "Luna, did you do that?",
    };
    static readonly string[] MomLinesQuiet =
    {
        "Almost done with this room.", "So many memories in these boxes.", "We'll be all right.", "Your father loved this house.",
    };

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);                   // the AI scripts have started by now
        mom = FindFirstObjectByType<GrandmaAI>();
        baby = FindFirstObjectByType<BabyAI>();
        nextTalk = Time.time + 25f;

        if (mom != null) SetUpMomRoom();
        MakeDust();
        StartCoroutine(BabyGiggles());
        if (mom != null) StartCoroutine(MomTalks());
    }

    // ---------- Mom's room
    void SetUpMomRoom()
    {
        GameObject room = GameObject.Find("MotherRoom");
        momDoor = FindFirstObjectByType<MotherRoomDoor>();
        if (room == null) { Debug.LogWarning("[LIFE] No object called MotherRoom found, Mom stays in her usual places."); return; }

        Bounds bounds = new Bounds(room.transform.position, Vector3.zero);
        foreach (Renderer r in room.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);

        if (!NavMesh.SamplePosition(bounds.center, out NavMeshHit hit, 4f, NavMesh.AllAreas))
        {
            Debug.LogWarning("[LIFE] Mom's room has no walkable floor (NavMesh) near " + bounds.center);
            return;
        }

        roomSpot = new GameObject("Mom_RoomSpot").transform;
        roomSpot.position = hit.position;
        for (int i = 0; i < roomVisitsInList; i++) mom.destinationWaypoints.Add(roomSpot);

        NavMeshAgent agent = mom.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isOnNavMesh) agent.Warp(hit.position);       // she starts the chapter in her room
        StartCoroutine(OpenHerDoorWhenNear());
    }

    IEnumerator OpenHerDoorWhenNear()
    {
        while (momDoor != null && mom != null)
        {
            Vector3 flat = mom.transform.position - momDoor.transform.position;
            flat.y = 0f;
            float distance = flat.magnitude;
            if (distance < 1.8f && !momDoor.IsOpen) { momDoor.TryInteract(); weOpenedTheDoor = true; }
            else if (distance > 3.5f && momDoor.IsOpen && weOpenedTheDoor) { momDoor.TryInteract(); weOpenedTheDoor = false; }
            yield return new WaitForSeconds(0.3f);
        }
    }

    // ---------- small sounds and comments
    IEnumerator BabyGiggles()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(18f, 40f));
            if (baby != null && baby.isActiveAndEnabled && !CutsceneRunner.IsPlaying) GameAudio.Play("baby_giggle", 0.6f);
        }
    }

    IEnumerator MomTalks()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(30f, 55f));
            if (CutsceneRunner.IsPlaying || mom == null || !mom.isActiveAndEnabled) continue;
            Say(MomLinesQuiet);
        }
    }

    // Called by FamilyReactions a moment after the ghost touches something.
    public void ReactTo(Vector3 where)
    {
        if (mom != null && mom.TryGetComponent(out FamilyGaze momGaze)) momGaze.LookAt(where);
        if (baby != null && baby.TryGetComponent(out FamilyGaze babyGaze)) babyGaze.LookAt(where);

        if (Time.time < nextTalk - 10f) return;                                   // not too chatty
        if (mom == null || Vector3.Distance(mom.transform.position, where) > 12f) return;
        StartCoroutine(SayLater(MomLinesNoticing, 0.9f));
        if (baby != null) GameAudio.Play("baby_giggle", 0.7f);
    }

    IEnumerator SayLater(string[] lines, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (!CutsceneRunner.IsPlaying) Say(lines);
    }

    void Say(string[] lines)
    {
        FadingHud.Subtitle("Mom", lines[Random.Range(0, lines.Length)], 3.5f);
        nextTalk = Time.time + 20f;
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
