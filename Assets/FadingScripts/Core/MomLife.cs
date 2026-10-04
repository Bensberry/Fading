using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Goes in: nowhere (ChapterRules adds it in the chapters where Mom lives).
// Gives Mom a life of her own. Her AI script (GrandmaAI, your friend's) still does the walking and the noticing; this decides
// WHERE she goes and WHAT she does when she gets there:
//   - NIGHT: she spends most of her time in HER ROOM, sitting on her bed, often crying. Now and then she checks on Luna.
//   - DAY:   she packs around the whole house (a new random spot in a random room every time) and sometimes goes to her room.
//   - At every stop she does something different (sits, cries, looks around, sighs) and waits a random time.
//   - VISION: she only notices a sign when her EYES (at the height of her head) can see it, or when it happens right beside her.
//
// Extra animations are optional. Download them from Mixamo and drop them into Assets/Resources/Animations/ with these names:
//   mom_sit, mom_sit_cry, mom_cry, mom_sad, mom_look, mom_pickup
// Without them she still cries (head down, shoulders shaking, the 'mom_cry' sound) but she stands instead of sitting.
public class MomLife : MonoBehaviour
{
    static readonly string[] Rooms = { "Hallway", "LivingRoom", "Kitchen", "ChildRoom", "MotherRoom", "GuestRoom", "LivingRoom", "Kitchen" };

    static readonly string[] NoticeLines = { "Did you see that?", "Hm? That moved...", "That's strange...", "Who's there?", "Was that you, Luna?" };
    static readonly string[] CryLines = { "She cries when she thinks nobody can see her.", "I'm right here. I'm right here...", "I wish I could hold her." };
    static readonly string[] SighLines = { "So many memories in these boxes.", "Almost done with this room.", "We'll be all right.", "Your father loved this house." };

    GrandmaAI mom;
    NavMeshAgent agent;
    PosePlayer pose;
    DayNightCycle cycle;
    List<Transform> original;
    Transform bedSpot, childSpot;
    readonly List<Transform> wanderSpots = new List<Transform>();
    Vector3 bedCenter;
    bool haveBed;
    float baseSpeed = 1f;
    Coroutine action;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);                    // the AI scripts have started by now
        mom = FindFirstObjectByType<GrandmaAI>();
        if (mom == null) { enabled = false; yield break; }

        agent = mom.GetComponent<NavMeshAgent>();
        if (agent != null) baseSpeed = agent.speed;
        cycle = FindFirstObjectByType<DayNightCycle>();
        pose = PosePlayer.On(mom.gameObject);
        original = new List<Transform>();
        if (mom.destinationWaypoints != null) original.AddRange(mom.destinationWaypoints);

        GiveHerEyes();
        MakeSpots();
        mom.onArrived += OnArrived;
        mom.onClueNoticed += OnNoticed;
        if (cycle != null) cycle.onPhaseChanged.AddListener(delegate { BuildSchedule(); });
        BuildSchedule();

        if (bedSpot != null && agent != null && agent.isOnNavMesh) agent.Warp(bedSpot.position);     // the chapter starts with her in her room
    }

    bool IsNight { get { return cycle != null && cycle.IsNight; } }

    // ---------- vision
    void GiveHerEyes()
    {
        mom.hearingRange = 1.5f;                                           // she must SEE what the ghost does
        mom.visionAngle = Mathf.Max(mom.visionAngle, 110f);
        mom.visionDistance = Mathf.Max(mom.visionDistance, 12f);

        Animator a = mom.animator != null ? mom.animator : mom.GetComponentInChildren<Animator>();
        Transform headBone = a != null && a.isHuman ? a.GetBoneTransform(HumanBodyBones.Head) : null;
        float eyeHeight = headBone != null ? headBone.position.y + 0.05f : mom.transform.position.y + 0.6f;

        Transform eyes = new GameObject("MomEyes").transform;
        eyes.SetParent(mom.transform, false);
        eyes.position = new Vector3(mom.transform.position.x, eyeHeight, mom.transform.position.z) + mom.transform.forward * 0.15f;
        eyes.localRotation = Quaternion.identity;
        mom.eyePoint = eyes;
    }

    // ---------- where she goes
    void MakeSpots()
    {
        Bounds bed;
        if (HouseRooms.TryGetObjectBounds("Bed_Mother", out bed))
        {
            haveBed = true;
            bedCenter = bed.center;
            bedSpot = Spot("Mom_BedSpot", BedSide(bed));
        }
        else
        {
            Vector3 p;
            if (HouseRooms.RandomPoint("MotherRoom", out p)) bedSpot = Spot("Mom_RoomSpot", p);
            else Debug.LogWarning("[MOM] Could not find her room (MotherRoom / Floor_Mother / Bed_Mother).");
        }

        Vector3 c;
        if (HouseRooms.RandomPoint("ChildRoom", out c)) childSpot = Spot("Mom_ChildSpot", c);
        for (int i = 0; i < 3; i++) wanderSpots.Add(Spot("Mom_Wander" + i, mom.transform.position));
        MoveWanderSpots(null);
    }

    static Transform Spot(string spotName, Vector3 position)
    {
        Transform t = new GameObject(spotName).transform;
        t.position = position;
        return t;
    }

    // A walkable point beside the bed, on the side facing the middle of her room.
    static Vector3 BedSide(Bounds bed)
    {
        Bounds room;
        Vector3 middle = HouseRooms.TryGetRoomBounds("MotherRoom", out room) ? room.center : bed.center + Vector3.forward;
        Vector3 edge = bed.ClosestPoint(new Vector3(middle.x, bed.center.y, middle.z));
        Vector3 outward = edge - bed.center;
        outward.y = 0f;
        outward = outward.sqrMagnitude > 0.001f ? outward.normalized : Vector3.forward;
        Vector3 p = new Vector3(edge.x, bed.min.y, edge.z) + outward * 0.3f;
        NavMeshHit hit;
        return NavMesh.SamplePosition(p, out hit, 1.5f, NavMesh.AllAreas) ? hit.position : p;
    }

    void MoveWanderSpots(Transform except)
    {
        foreach (Transform s in wanderSpots)
        {
            if (s == except) continue;
            Vector3 p;
            if (HouseRooms.RandomPoint(Rooms[Random.Range(0, Rooms.Length)], out p)) s.position = p;
        }
    }

    // Night: mostly her room. Day: everywhere (her usual places, random spots) and her room now and then.
    void BuildSchedule()
    {
        List<Transform> list = new List<Transform>();
        if (IsNight && bedSpot != null)
        {
            for (int i = 0; i < 3; i++) list.Add(bedSpot);
            if (childSpot != null) list.Add(childSpot);
            list.Add(wanderSpots[0]);
        }
        else
        {
            list.AddRange(original);
            list.AddRange(wanderSpots);
            if (bedSpot != null) list.Add(bedSpot);
            if (childSpot != null) list.Add(childSpot);
        }
        list.RemoveAll(t => t == null);
        if (list.Count >= 2) mom.destinationWaypoints = list;
    }

    // ---------- what she does when she gets there
    void OnArrived(Transform where)
    {
        if (wanderSpots.Contains(where)) MoveWanderSpots(where);          // next time she goes somewhere new
        mom.waitTimeAtLocation = Random.Range(2.5f, 8f);
        if (agent != null) agent.speed = baseSpeed * Random.Range(0.85f, 1.15f);

        StopAction();
        if (where == bedSpot) action = StartCoroutine(AtHerBed());
        else if (where == childSpot) action = StartCoroutine(CheckOnLuna());
        else action = StartCoroutine(SomethingSmall());
    }

    IEnumerator AtHerBed()
    {
        float stay = IsNight ? Random.Range(14f, 24f) : Random.Range(6f, 11f);
        bool cry = Random.value < (IsNight ? 0.6f : 0.3f);
        mom.holdUntil = Time.time + stay + 4f;

        yield return StepTo(bedSpot.position, 3f);
        if (haveBed) yield return TurnTo(mom.transform.position * 2f - bedCenter, 0.8f);     // back to the bed, ready to sit

        AnimationClip sit = cry ? GameClips.First(mom.gameObject, "mom_sit_cry", "mom_sit") : GameClips.Get("mom_sit");
        bool sitting = pose.Play(sit, 0.7f);
        if (cry) Cry(sitting && sit.name.Contains("cry") ? 0.4f : 1f);
        yield return new WaitForSeconds(stay);
        Finish();
    }

    IEnumerator CheckOnLuna()
    {
        mom.holdUntil = Time.time + Random.Range(5f, 9f);
        BabyAI baby = FindFirstObjectByType<BabyAI>();
        if (baby != null) yield return TurnTo(baby.transform.position, 1f);
        if (IsNight && Random.value < 0.5f) FamilyLife.Say("Mom", "Sleep, little one. Mommy's here.", mom.transform.position);
        pose.Play(GameClips.Get("mom_sad"), 0.6f);
        while (Time.time < mom.holdUntil) yield return null;
        Finish();
    }

    IEnumerator SomethingSmall()
    {
        float r = Random.value;
        if (r < 0.35f)                                                      // looks around the room
        {
            mom.holdUntil = Time.time + Random.Range(4f, 7f);
            pose.Play(GameClips.Get("mom_look"), 0.5f);
            Vector3 f = mom.transform.forward;
            yield return TurnTo(mom.transform.position + Quaternion.Euler(0f, Random.Range(50f, 110f), 0f) * f, 1.4f);
            yield return new WaitForSeconds(1f);
            yield return TurnTo(mom.transform.position + Quaternion.Euler(0f, -Random.Range(50f, 110f), 0f) * f, 1.6f);
        }
        else if (r < 0.6f)                                                  // packs something
        {
            mom.holdUntil = Time.time + Random.Range(4f, 8f);
            pose.Play(GameClips.First(mom.gameObject, "mom_pickup", "mom_look"), 0.4f);
        }
        else if (r < 0.82f)                                                 // a sad moment
        {
            mom.holdUntil = Time.time + Random.Range(5f, 9f);
            pose.Play(GameClips.Get("mom_sad"), 0.6f);
            GameAudio.PlayAt("mom_sigh", mom.transform.position, 0.7f);
            if (Random.value < 0.5f) FamilyLife.Say("Mom", SighLines[Random.Range(0, SighLines.Length)], mom.transform.position);
        }
        else                                                                // she cries, standing
        {
            mom.holdUntil = Time.time + Random.Range(6f, 10f);
            bool clip = pose.Play(GameClips.Get("mom_cry"), 0.6f);
            Cry(clip ? 0.3f : 1f);
        }
        while (Time.time < mom.holdUntil) yield return null;
        Finish();
    }

    void Cry(float sobAmount)
    {
        pose.sob = sobAmount;
        GameAudio.PlayAt("mom_cry", mom.transform.position, 0.8f);
        if (Random.value < 0.5f) FamilyLife.Say("", CryLines[Random.Range(0, CryLines.Length)], mom.transform.position);
    }

    void Finish()
    {
        pose.Stop(0.6f);
        pose.sob = 0f;
        mom.holdUntil = 0f;
        action = null;
    }

    void StopAction()
    {
        if (action != null) StopCoroutine(action);
        action = null;
        pose.Stop(0.25f);
        pose.sob = 0f;
    }

    // ---------- she noticed something: she gets up and goes to look
    void OnNoticed(ClueGoal clue)
    {
        StopAction();
        mom.holdUntil = 0f;
        if (clue != null && clue.heardAnywhere)                              // Luna is crying: she hurries
        {
            FamilyLife.Say("Mom", "Luna?! I'm coming, baby!", mom.transform.position, 25f);
            StartCoroutine(Hurry(7f));
        }
        else FamilyLife.Say("Mom", NoticeLines[Random.Range(0, NoticeLines.Length)], mom.transform.position);
    }

    IEnumerator Hurry(float seconds)
    {
        if (agent == null) yield break;
        agent.speed = baseSpeed * 1.7f;
        yield return new WaitForSeconds(seconds);
        agent.speed = baseSpeed;
    }

    // Called by FamilyFear when the ghost frightened them.
    public void Frightened()
    {
        if (mom == null) return;
        StopAction();
        FamilyLife.Say("Mom", Random.value < 0.5f ? "Who's there?! Stop it!" : "Stop... please, stop.", mom.transform.position, 25f);
        GameAudio.PlayAt("mom_gasp", mom.transform.position, 0.9f);
    }

    // ---------- small movement helpers
    IEnumerator StepTo(Vector3 point, float maxSeconds)
    {
        if (agent == null || !agent.isOnNavMesh) yield break;
        agent.isStopped = false;
        agent.stoppingDistance = 0.1f;
        agent.SetDestination(point);
        for (float t = 0f; t < maxSeconds; t += Time.deltaTime)
        {
            if (!agent.pathPending && agent.remainingDistance <= 0.15f) break;
            yield return null;
        }
        agent.ResetPath();
        agent.isStopped = true;
    }

    IEnumerator TurnTo(Vector3 point, float seconds)
    {
        Vector3 direction = point - mom.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) yield break;
        Quaternion from = mom.transform.rotation, to = Quaternion.LookRotation(direction);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            mom.transform.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        mom.transform.rotation = to;
    }

    void OnDestroy()
    {
        if (mom != null) { mom.onArrived -= OnArrived; mom.onClueNoticed -= OnNoticed; }
    }
}
