using System.Collections;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in the chapters where the baby lives).
// Gives Luna a life of her own. Her AI script (BabyAI, your friend's) still does the crawling and her toy reactions; this adds:
//   - She lives ON HER BED (Bed_Child): by day she crawls around on top of it (random spots, random speed, random pauses)
//     and can never fall off or crawl away.
//   - NIGHT: she sleeps (lying down, eyes closed).
//   - She sometimes notices the ghost when he is close: she looks up at him, smiles and giggles.
//   - Cry(): when the ghost frightens her she stops, cries (face + 'baby_cry' sound), and Mom hears it from anywhere.
public class BabyLife : MonoBehaviour
{
    BabyAI baby;
    BabyFace face;
    PosePlayer pose;
    DayNightCycle cycle;
    ClueGoal cryClue;
    Transform player;
    Coroutine crying;

    bool onBed;
    Bounds bedArea;               // where on the bed she may crawl
    float lyingY;                 // the height of her AI object while on the bed
    float awakeIdleSpeed;

    public BabyFace Face { get { return face; } }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        baby = FindFirstObjectByType<BabyAI>();
        if (baby == null) { enabled = false; yield break; }

        cycle = FindFirstObjectByType<DayNightCycle>();
        face = BabyFace.On(baby.gameObject);
        pose = PosePlayer.On(baby.gameObject);
        awakeIdleSpeed = baby.idleAnimationSpeed;
        if (Camera.main != null) player = Camera.main.transform;

        baby.pickRoamPath = PickSpotOnBed;
        baby.roamWaitMin = 3f;
        baby.roamWaitMax = 8f;
        MakeCryClue();
        yield return PutHerOnTheBed();

        if (cycle != null) cycle.onPhaseChanged.AddListener(delegate { OnPhaseChanged(); });
        OnPhaseChanged();
        StartCoroutine(SeesTheGhostSometimes());
    }

    // ---------- her bed
    IEnumerator PutHerOnTheBed()
    {
        float top;
        Bounds bed;
        if (!HouseRooms.TryGetBedTop("Bed_Child", out top, out bed))
        {
            Debug.LogWarning("[BABY] Bed_Child not found: Luna crawls on the floor as before.");
            yield break;
        }

        float marginX = Mathf.Min(0.3f, bed.size.x * 0.3f), marginZ = Mathf.Min(0.3f, bed.size.z * 0.3f);
        bedArea = new Bounds(bed.center, new Vector3(bed.size.x - 2f * marginX, bed.size.y, bed.size.z - 2f * marginZ));

        Rigidbody body = baby.GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true;                         // no physics: she rests on the mattress
        foreach (SkinnedMeshRenderer r in baby.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;

        baby.transform.position = new Vector3(bed.center.x, top + 1f, bed.center.z);
        for (int i = 0; i < 3; i++) yield return null;
        Bounds model = ModelBounds();
        lyingY = baby.transform.position.y + (top - model.min.y);
        baby.transform.position = new Vector3(bed.center.x, lyingY, bed.center.z);
        onBed = true;
    }

    Bounds ModelBounds()
    {
        Bounds b = new Bounds(baby.transform.position, Vector3.zero);
        bool first = true;
        foreach (Renderer r in baby.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        return b;
    }

    // Where she crawls next: a random spot on her bed.
    Vector3[] PickSpotOnBed(Vector3 from)
    {
        if (!onBed || baby.asleep) return null;
        baby.moveSpeed = Random.Range(0.18f, 0.32f);
        Vector3 p = new Vector3(Random.Range(bedArea.min.x, bedArea.max.x), lyingY, Random.Range(bedArea.min.z, bedArea.max.z));
        return new[] { p };
    }

    // Whatever moves her (crawling, her teddy reaction), she stays on the bed.
    void LateUpdate()
    {
        if (!onBed || baby == null) return;
        Vector3 p = baby.transform.position;
        p.x = Mathf.Clamp(p.x, bedArea.min.x, bedArea.max.x);
        p.z = Mathf.Clamp(p.z, bedArea.min.z, bedArea.max.z);
        p.y = lyingY;
        baby.transform.position = p;
    }

    // ---------- night: she sleeps
    void OnPhaseChanged()
    {
        if (cycle != null && cycle.IsNight) Sleep();
        else WakeUp();
    }

    void Sleep()
    {
        if (baby.asleep) return;
        baby.asleep = true;
        baby.idleAnimationSpeed = 1f;                                      // the sleeping pose plays at normal speed
        face.LookAt(null);
        face.Smile(false);
        face.eyesClosed = true;
        pose.Play(GameClips.Get("sleeping", baby.gameObject), 0.6f);
    }

    void WakeUp()
    {
        if (!baby.asleep) return;
        baby.asleep = false;
        baby.idleAnimationSpeed = awakeIdleSpeed;
        face.eyesClosed = false;
        pose.Stop(0.6f);
    }

    // ---------- she sees him
    IEnumerator SeesTheGhostSometimes()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(7f, 14f));
            if (player == null || baby.asleep || CutsceneRunner.IsPlaying || crying != null) continue;
            if (Vector3.Distance(player.position, baby.transform.position) > 4f) continue;

            baby.pauseUntil = Time.time + 3.5f;
            face.LookAt(player);
            yield return new WaitForSeconds(0.6f);
            face.Smile(true);
            GameAudio.PlayAt("baby_giggle", baby.transform.position, 0.8f);
            FamilyProgress.Award("luna:sees-you", 4f, 2f, "Luna smiled at you");
            yield return new WaitForSeconds(2.6f);
            face.Smile(false);
            face.LookAt(null);
        }
    }

    // A sign close to her: a quick happy smile and a giggle.
    public void Delight()
    {
        if (baby == null || baby.asleep || crying != null) return;
        StartCoroutine(SmileFor(2.2f));
    }

    IEnumerator SmileFor(float seconds)
    {
        face.Smile(true);
        GameAudio.PlayAt("baby_giggle", baby.transform.position, 0.7f);
        yield return new WaitForSeconds(seconds);
        if (crying == null) face.Smile(false);
    }

    // ---------- crying
    void MakeCryClue()
    {
        cryClue = baby.gameObject.AddComponent<ClueGoal>();
        cryClue.points = 0;                                  // Mom comforting her earns nothing
        cryClue.heardAnywhere = true;
        foreach (GrandmaAI mom in FindObjectsByType<GrandmaAI>(FindObjectsSortMode.None))
            if (mom.clues != null) mom.clues.Add(cryClue);
    }

    public void Cry(float seconds = 7f)
    {
        if (baby == null) return;
        if (crying != null) StopCoroutine(crying);
        crying = StartCoroutine(CryFor(seconds));
    }

    IEnumerator CryFor(float seconds)
    {
        baby.pauseUntil = Time.time + seconds;
        face.LookAt(null);
        face.eyesClosed = false;
        face.Cry(seconds);
        GameAudio.PlayAt("baby_cry", baby.transform.position, 1f);
        cryClue.reactionStarted = false;
        cryClue.pointsAwarded = false;
        cryClue.ActivateClue();                              // Mom hears it and comes (unless she is asleep)
        yield return new WaitForSeconds(seconds);
        cryClue.DeactivateClue();
        if (baby.asleep) face.eyesClosed = true;
        crying = null;
    }
}
