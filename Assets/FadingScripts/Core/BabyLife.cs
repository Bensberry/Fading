using System.Collections;
using UnityEngine;

// Goes in: nowhere (ChapterRules adds it in the chapters where the baby lives).
// Gives Luna a life of her own. Her AI script (BabyAI, your friend's) still does the crawling and her toy reactions; this adds:
//   - DAY:   she crawls around the HALLWAY and HER ROOM, and now and then into MOM'S ROOM (random spots, random speed, random pauses),
//            always along walkable paths (NavMesh) so she goes through doorways, not walls.
//   - NIGHT: she crawls back to her room and uses her usual crawl spots there.
//   - She sometimes notices the ghost when he is close: she looks up at him, smiles and giggles.
//   - Cry(): when the ghost frightens her she stops, cries (face + 'baby_cry' sound), and Mom hears it from anywhere.
public class BabyLife : MonoBehaviour
{
    BabyAI baby;
    BabyFace face;
    DayNightCycle cycle;
    ClueGoal cryClue;
    Transform player;
    Coroutine crying;

    public BabyFace Face { get { return face; } }

    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.5f);
        baby = FindFirstObjectByType<BabyAI>();
        if (baby == null) { enabled = false; yield break; }

        cycle = FindFirstObjectByType<DayNightCycle>();
        face = BabyFace.On(baby.gameObject);
        FirstPersonController p = FindFirstObjectByType<FirstPersonController>();
        if (p != null) player = Camera.main != null ? Camera.main.transform : p.transform;

        baby.pickRoamPath = PickPath;
        MakeCryClue();
        StartCoroutine(SeesTheGhostSometimes());
    }

    // ---------- where she crawls next
    Vector3[] PickPath(Vector3 from)
    {
        string room;
        if (cycle != null && cycle.IsNight)
        {
            if (HouseRooms.Contains("ChildRoom", from)) return null;      // at night, in her room: her usual crawl spots
            room = "ChildRoom";
        }
        else
        {
            float r = Random.value;
            room = r < 0.45f ? "Hallway" : r < 0.85f ? "ChildRoom" : "MotherRoom";
        }

        Vector3 to;
        if (!HouseRooms.RandomPoint(room, out to)) return null;
        baby.moveSpeed = Random.Range(0.5f, 0.85f);
        return HouseRooms.Path(from, to);
    }

    // ---------- she sees him
    IEnumerator SeesTheGhostSometimes()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(12f, 25f));
            if (player == null || CutsceneRunner.IsPlaying || crying != null) continue;
            if (Vector3.Distance(player.position, baby.transform.position) > 4f) continue;

            baby.pauseUntil = Time.time + 3.5f;
            face.LookAt(player);
            yield return new WaitForSeconds(0.6f);
            face.Smile(true);
            GameAudio.PlayAt("baby_giggle", baby.transform.position, 0.8f);
            yield return new WaitForSeconds(2.6f);
            face.Smile(false);
            face.LookAt(null);
        }
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
        face.Cry(seconds);
        GameAudio.PlayAt("baby_cry", baby.transform.position, 1f);
        cryClue.reactionStarted = false;
        cryClue.pointsAwarded = false;
        cryClue.ActivateClue();                              // Mom hears it and comes
        yield return new WaitForSeconds(seconds);
        cryClue.DeactivateClue();
        crying = null;
    }
}
