using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class GrandmaAI : MonoBehaviour
{
    [Header("Work Waypoints")]
    public List<Transform> destinationWaypoints;
    public float waitTimeAtLocation = 5f;
    public float workArrivalDistance = 1.5f;

    [Header("Clue Detection")]
    public List<ClueGoal> clues;
    public Transform eyePoint;
    public float visionDistance = 10f;
    [Range(0f, 180f)] public float visionAngle = 100f;
    public LayerMask lineOfSightLayers = ~0;

    [Header("Radio Hearing")]
    [Tooltip("Maximum distance at which Grandma can hear an active radio clue.")]
    public float radioDetectionRange = 30f;

    [Header("Clue Reaction")]
    public float clueStoppingDistance = 1.2f;
    public float clueArrivalDistance = 1.5f;

    [Tooltip("How long Grandma waits after the clue becomes inactive.")]
    public float stayAtClueSeconds = 3f;

    [Header("Score")]
    public int totalPoints = 0;

    [Tooltip("Grandma is fully convinced when progress reaches this value.")]
    public int maxPoints = 60;

    [Header("Progress Slider")]
    public Slider progressSlider;

    private NavMeshAgent agent;
    private int currentIndex = -1;

    private bool isReacting = false;
    private ClueGoal activeClue;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (eyePoint == null)
            eyePoint = transform;

        // Initialize progress slider.
        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = maxPoints;

            // Display capped progress without changing totalPoints.
            progressSlider.value = Mathf.Clamp(totalPoints, 0, maxPoints);
        }
        else
        {
            Debug.LogWarning("[GRANDMA] Progress Slider is not assigned.");
        }

        if (destinationWaypoints == null || destinationWaypoints.Count < 2)
        {
            Debug.LogWarning("[GRANDMA] Assign at least 2 work waypoints.");
            return;
        }

        StartCoroutine(RoutineLoop());
    }

    private void Update()
    {
        if (isReacting)
            return;

        ClueGoal detectedClue = FindDetectedClue();

        if (detectedClue == null)
            return;

        detectedClue.reactionStarted = true;
        activeClue = detectedClue;

        Debug.Log(
            $"[GRANDMA] DETECTED CLUE: {detectedClue.name}. " +
            $"goalAchieved = {detectedClue.goalAchieved}"
        );

        StartCoroutine(ReactToClue(detectedClue));
    }

    private ClueGoal FindDetectedClue()
    {
        if (clues == null)
            return null;

        foreach (ClueGoal clue in clues)
        {
            if (clue == null)
                continue;

            if (!clue.goalAchieved)
                continue;

            if (clue.reactionStarted)
                continue;

            // Radio clues use hearing range, not vision or raycasting.
            RadioClue radio = clue.GetComponent<RadioClue>();

            if (radio != null)
            {
                float radioDistance = Vector3.Distance(
                    transform.position,
                    clue.transform.position
                );

                Debug.Log(
                    $"[GRANDMA RADIO CHECK] {clue.name}: " +
                    $"active = {clue.goalAchieved}, " +
                    $"distance = {radioDistance:F1}, " +
                    $"hearing range = {radioDetectionRange}"
                );

                if (radioDistance <= radioDetectionRange)
                {
                    Debug.Log($"[GRANDMA] HEARD RADIO CLUE: {clue.name}");
                    return clue;
                }

                continue;
            }

            // Non-radio clues use vision detection.
            if (CanSeeClue(clue))
                return clue;
        }

        return null;
    }

    private bool CanSeeClue(ClueGoal clue)
    {
        Vector3 direction = clue.transform.position - eyePoint.position;
        float distance = direction.magnitude;

        if (distance > visionDistance)
            return false;

        if (Vector3.Angle(eyePoint.forward, direction) > visionAngle * 0.5f)
            return false;

        if (Physics.Raycast(
            eyePoint.position,
            direction.normalized,
            out RaycastHit hit,
            visionDistance,
            lineOfSightLayers,
            QueryTriggerInteraction.Ignore))
        {
            return hit.transform == clue.transform ||
                   hit.transform.IsChildOf(clue.transform);
        }

        return false;
    }

    private IEnumerator ReactToClue(ClueGoal clue)
    {
        if (clue == null || !agent.isOnNavMesh)
        {
            Debug.LogWarning(
                "[GRANDMA] Cannot react: clue is missing or Grandma is off the NavMesh."
            );

            activeClue = null;
            isReacting = false;
            yield break;
        }

        isReacting = true;

        Debug.Log($"[GRANDMA] Interrupting work and approaching {clue.name}.");

        agent.isStopped = false;
        agent.ResetPath();
        agent.stoppingDistance = clueStoppingDistance;

        // Find a nearby walkable position around the clue.
        if (!NavMesh.SamplePosition(
            clue.transform.position,
            out NavMeshHit navHit,
            5f,
            agent.areaMask))
        {
            Debug.LogWarning(
                $"[GRANDMA] No NavMesh position found near {clue.name}."
            );

            FinishClueReaction();
            yield break;
        }

        Debug.Log(
            $"[GRANDMA] Navigation target for {clue.name}: {navHit.position}"
        );

        // Check whether a complete path exists.
        NavMeshPath path = new NavMeshPath();

        bool pathCalculated = agent.CalculatePath(navHit.position, path);

        if (!pathCalculated || path.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning(
                $"[GRANDMA] No complete NavMesh path to {clue.name}. " +
                $"Path status: {path.status}"
            );

            FinishClueReaction();
            yield break;
        }

        bool destinationSet = agent.SetDestination(navHit.position);

        if (!destinationSet)
        {
            Debug.LogWarning(
                $"[GRANDMA] Could not set navigation destination for {clue.name}."
            );

            FinishClueReaction();
            yield break;
        }

        bool reachedClue = false;

        while (agent.isOnNavMesh)
        {
            if (agent.pathPending)
            {
                yield return null;
                continue;
            }

            float arrivalThreshold = Mathf.Max(
                clueArrivalDistance,
                agent.stoppingDistance
            );

            // Check arrival before checking hasPath.
            // Unity may clear hasPath after the agent reaches its destination.
            if (agent.remainingDistance <= arrivalThreshold)
            {
                reachedClue = true;
                break;
            }

            if (!agent.hasPath)
            {
                Debug.LogWarning(
                    $"[GRANDMA] No active path to {clue.name}. " +
                    $"Remaining distance: {agent.remainingDistance}"
                );
                break;
            }

            if (agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                Debug.LogWarning(
                    $"[GRANDMA] Path to {clue.name} is not complete: " +
                    $"{agent.pathStatus}"
                );
                break;
            }

            yield return null;
        }

        if (reachedClue)
        {
            agent.isStopped = true;

            Debug.Log(
                $"[GRANDMA] Reached {clue.name}. " +
                "Waiting while the clue remains active."
            );

            // Stay at the clue for as long as it remains active.
            while (clue != null && clue.goalAchieved)
            {
                yield return null;
            }

            // The clue has now become inactive.
            // Start the additional wait only after it becomes false.
            Debug.Log(
                $"[GRANDMA] Clue {clue.name} is now inactive. " +
                $"Waiting another {stayAtClueSeconds} seconds before leaving."
            );

            yield return new WaitForSeconds(stayAtClueSeconds);

            // Award the clue's full points only once.
            if (clue != null && !clue.pointsAwarded)
            {
                // Keep the full score. Do not clamp it to the slider maximum.
                totalPoints += clue.points;

                clue.pointsAwarded = true;

                UpdateProgressSlider();

                Debug.Log(
                    $"[GRANDMA] {clue.name} awarded {clue.points} points. " +
                    $"Total earned: {totalPoints}. " +
                    $"Grandma's progress: " +
                    $"{Mathf.Min(totalPoints, maxPoints)}/{maxPoints}"
                );
            }
            else if (clue != null)
            {
                Debug.Log(
                    $"[GRANDMA] {clue.name} was already scored. No points added."
                );
            }
        }
        else
        {
            Debug.LogWarning(
                $"[GRANDMA] Could not reach {clue.name}. " +
                "Ending reaction and returning to work."
            );
        }

        FinishClueReaction();
    }

    private void UpdateProgressSlider()
    {
        if (progressSlider == null)
            return;

        progressSlider.minValue = 0;
        progressSlider.maxValue = maxPoints;

        // Slider progress is capped at 60.
        // totalPoints continues accumulating independently.
        progressSlider.value = Mathf.Clamp(totalPoints, 0, maxPoints);
    }

    private void FinishClueReaction()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.ResetPath();
            agent.stoppingDistance = workArrivalDistance;
        }

        activeClue = null;
        isReacting = false;

        Debug.Log("[GRANDMA] Resuming work routine.");
    }

    private IEnumerator RoutineLoop()
    {
        yield return new WaitUntil(
            () => agent != null && agent.isOnNavMesh
        );

        while (true)
        {
            if (destinationWaypoints == null ||
                destinationWaypoints.Count == 0)
            {
                yield return null;
                continue;
            }

            currentIndex = GetRandomDifferentIndex(currentIndex);

            Transform workTarget = destinationWaypoints[currentIndex];

            if (workTarget == null)
            {
                Debug.LogWarning("[GRANDMA] A work waypoint is missing.");
                yield return null;
                continue;
            }

            Debug.Log(
                $"[GRANDMA] Going to work waypoint: {workTarget.name}"
            );

            agent.isStopped = false;
            agent.stoppingDistance = workArrivalDistance;
            agent.SetDestination(workTarget.position);

            while (true)
            {
                if (isReacting)
                {
                    yield return null;
                    continue;
                }

                // The clue reaction clears Grandma's path.
                // Reassign her current work destination after the reaction.
                if (!agent.pathPending && !agent.hasPath)
                {
                    agent.isStopped = false;
                    agent.stoppingDistance = workArrivalDistance;

                    bool destinationSet =
                        agent.SetDestination(workTarget.position);

                    if (destinationSet)
                    {
                        Debug.Log(
                            $"[GRANDMA] Returning to work waypoint: " +
                            $"{workTarget.name}"
                        );
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[GRANDMA] Could not resume path to " +
                            $"{workTarget.name}."
                        );
                    }

                    yield return null;
                    continue;
                }

                if (!agent.pathPending &&
                    agent.hasPath &&
                    agent.remainingDistance <= workArrivalDistance)
                {
                    break;
                }

                yield return null;
            }

            Debug.Log(
                $"[GRANDMA] Arrived at work waypoint: {workTarget.name}"
            );

            float elapsed = 0f;

            while (elapsed < waitTimeAtLocation)
            {
                if (isReacting)
                {
                    yield return null;
                    continue;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }

    private int GetRandomDifferentIndex(int current)
    {
        if (destinationWaypoints.Count <= 1)
            return 0;

        int newIndex;

        do
        {
            newIndex = Random.Range(0, destinationWaypoints.Count);
        }
        while (newIndex == current);

        return newIndex;
    }
}