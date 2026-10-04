using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BabyAI : MonoBehaviour
{
    // ============================================================
    // CRAWL POSITIONS
    // ============================================================

    [Header("Crawl Positions")]
    public Transform[] crawlPositions;


    // ============================================================
    // NORMAL MOVEMENT
    // ============================================================

    [Header("Normal Movement")]
    public float moveSpeed = 0.7f;
    public float rotationSpeed = 5f;
    public float arrivalDistance = 0.05f;


    // ============================================================
    // WAITING
    // ============================================================

    [Header("Waiting")]
    public float waitTimeAtPosition = 3f;


    // ============================================================
    // VISION
    // ============================================================

    [Header("Baby Eye / Vision")]
    public Transform babyEyePoint;
    public float visionDistance = 10f;
    public float visionAngle = 180f;

    [Header("Vision Layers")]
    public LayerMask visionLayers = ~0;


    // ============================================================
    // ANIMATION
    // ============================================================

    [Header("Baby Animation")]
    public Animator babyAnimator;

    [Tooltip("Animation speed while she is standing still (0 = frozen pose, a small value = she gently sways instead of freezing).")]
    public float idleAnimationSpeed = 0.12f;


    // ============================================================
    // TEDDY
    // ============================================================

    [Header("Teddy")]
    public ClueGoal teddyClue;
    public Transform teddy;

    public float teddyMoveSpeed = 1.2f;
    public float teddyArrivalDistance = 0.4f;

    // Prevents Teddy from triggering repeatedly while its
    // ClueGoal remains active.
    private bool teddyHandledUntilReset = false;

    private bool warnedNoCollider = false;


    // ============================================================
    // WINDBOX
    // ============================================================

    [Header("Windbox")]
    public ClueGoal windboxClue;
    public Transform windbox;

    // Kept for Inspector compatibility.
    // Baby does NOT move toward Windbox.
    public float windboxMoveSpeed = 1.0f;
    public float windboxArrivalDistance = 0.5f;


    // ============================================================
    // NIGHTLIGHT
    // ============================================================

    [Header("Nightlight")]
    public ClueGoal lampClue;
    public Transform lamp;

    public float lampLookTime = 3f;
    public float lampLookRotationSpeed = 5f;


    // ============================================================
    // SCORE
    // ============================================================

    [Header("Baby Score")]
    public int totalPoints = 0;

    private const int SLIDER_MAX = 60;

    public Slider progressSlider;


    // ============================================================
    // INTERNAL MOVEMENT STATE
    // ============================================================

    private int currentPosition = -1;

    private bool reactingToClue = false;

    private Coroutine movementCoroutine;


    // ============================================================
    // CURRENT TARGET
    // ============================================================

    // The normal crawl position the baby is currently travelling toward.
    // This survives clue interruptions.

    // Optional (set by BabyLife): picks where she crawls next and returns the path (corner points) to get there.
    // When it is empty or returns null she uses her crawl positions as before.
    public System.Func<Vector3, Vector3[]> pickRoamPath;
    public float roamWaitMin = 2f;
    public float roamWaitMax = 7f;
    [HideInInspector] public float pauseUntil;          // she stays where she is until this time (e.g. while crying)

    private Transform currentTarget;

    private int currentTargetIndex = -1;

    // True when baby reached a normal crawl position
    // and is waiting there.

    private bool waitingAtTarget = false;

    // Preserves remaining wait time when a clue interrupts.

    private float waitTimer = 0f;


    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        // --------------------------------------------------------
        // Validate crawl positions
        // --------------------------------------------------------

        if (crawlPositions == null ||
            crawlPositions.Length != 3)
        {
            Debug.LogError(
                "BabyAI needs exactly 3 crawl positions."
            );

            return;
        }


        // --------------------------------------------------------
        // Validate eye
        // --------------------------------------------------------

        if (babyEyePoint == null)
        {
            Debug.LogWarning(
                "BabyAI: Baby Eye Point is not assigned."
            );
        }


        // --------------------------------------------------------
        // Find Animator automatically
        // --------------------------------------------------------

        if (babyAnimator == null)
        {
            babyAnimator =
                GetComponentInChildren<Animator>();

            if (babyAnimator == null)
            {
                Debug.LogWarning(
                    "BabyAI: No Animator found on Baby or its children."
                );
            }
        }


        // --------------------------------------------------------
        // Start crawling animation
        // --------------------------------------------------------

        SetCrawlingAnimation(true);


        // --------------------------------------------------------
        // Setup slider
        // --------------------------------------------------------

        if (progressSlider != null)
        {
            progressSlider.minValue = 0;
            progressSlider.maxValue = SLIDER_MAX;

            progressSlider.value =
                Mathf.Clamp(
                    totalPoints,
                    0,
                    SLIDER_MAX
                );
        }


        // --------------------------------------------------------
        // Reset reaction states
        // --------------------------------------------------------

        if (teddyClue != null)
        {
            teddyClue.reactionStarted = false;
        }

        if (windboxClue != null)
        {
            windboxClue.reactionStarted = false;
        }

        if (lampClue != null)
        {
            lampClue.reactionStarted = false;
        }


        // --------------------------------------------------------
        // Start normal crawling
        // --------------------------------------------------------

        movementCoroutine =
            StartCoroutine(
                MovementRoutine()
            );
    }


    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        WatchForStall();

        // ========================================================
        // RESET TEDDY LOCK WHEN TEDDY CLUE ENDS
        // ========================================================

        if (teddyClue != null &&
            !teddyClue.goalAchieved)
        {
            teddyHandledUntilReset = false;
        }


        // Baby is already reacting.
        if (reactingToClue)
            return;


        // ========================================================
        // NIGHTLIGHT
        // ========================================================

        // IMPORTANT:
        // Nightlight does NOT require vision.
        //
        // Baby reacts purely because the ClueGoal is active.

        if (lampClue != null &&
            lampClue.goalAchieved &&
            !lampClue.reactionStarted)
        {
            Debug.Log(
                "[BABY] Nightlight detected!"
            );

            lampClue.reactionStarted = true;

            StartClueReaction(
                LampReactionRoutine()
            );

            return;
        }


        // ========================================================
        // TEDDY
        // ========================================================

        // Teddy DOES require actual vision.

        if (teddyClue != null &&
            teddy != null &&
            teddyClue.goalAchieved &&
            !teddyClue.reactionStarted &&
            !teddyHandledUntilReset)
        {
            if (CanSeeObject(teddy))
            {
                Debug.Log(
                    "[BABY] Baby saw Teddy!"
                );

                teddyClue.reactionStarted = true;

                teddyHandledUntilReset = true;

                StartClueReaction(
                    TeddyReactionRoutine()
                );

                return;
            }
        }


        // ========================================================
        // WINDBOX
        // ========================================================

        // Windbox does NOT require vision.
        // Active clue means baby hears it.

        if (windboxClue != null &&
            windboxClue.goalAchieved &&
            !windboxClue.reactionStarted)
        {
            Debug.Log(
                "[BABY] Baby heard Windbox!"
            );

            windboxClue.reactionStarted = true;

            StartClueReaction(
                WindboxReactionRoutine()
            );

            return;
        }
    }


    // ============================================================
    // START CLUE REACTION
    // ============================================================

    // Safety net: if the baby has not moved for 15 seconds while NOT reacting to anything, her crawling restarts.
    private Vector3 watchdogPosition;
    private float watchdogTime;

    private void WatchForStall()
    {
        if (reactingToClue || crawlPositions == null || crawlPositions.Length != 3)
        {
            watchdogTime = Time.time;
            watchdogPosition = transform.position;
            return;
        }

        if ((transform.position - watchdogPosition).sqrMagnitude > 0.0004f)
        {
            watchdogPosition = transform.position;
            watchdogTime = Time.time;
            return;
        }

        if (Time.time - watchdogTime < 15f) return;

        Debug.LogWarning("[BABY] Standing still for too long: restarting her crawling.");
        watchdogTime = Time.time;
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        currentTarget = null;
        currentTargetIndex = -1;
        waitingAtTarget = false;
        waitTimer = 0f;
        movementCoroutine = StartCoroutine(MovementRoutine());
    }

    private void StartClueReaction(
        IEnumerator reaction
    )
    {
        reactingToClue = true;


        // --------------------------------------------------------
        // Freeze current crawl animation
        // --------------------------------------------------------

        SetCrawlingAnimation(false);


        // --------------------------------------------------------
        // Stop normal movement
        // --------------------------------------------------------

        if (movementCoroutine != null)
        {
            StopCoroutine(
                movementCoroutine
            );

            movementCoroutine = null;
        }


        // --------------------------------------------------------
        // Start reaction
        // --------------------------------------------------------

        StartCoroutine(reaction);
    }


    // ============================================================
    // NORMAL BABY MOVEMENT
    // ============================================================

    private IEnumerator MovementRoutine()
    {
        while (true)
        {
            // ----------------------------------------------------
            // Don't move while reacting.
            // ----------------------------------------------------

            if (reactingToClue)
            {
                SetCrawlingAnimation(false);

                yield return null;
                continue;
            }


            // ----------------------------------------------------
            // Choose a new target ONLY if there isn't one.
            //
            // This allows the baby to resume the same target
            // after a clue interruption.
            // ----------------------------------------------------

            if (Time.time < pauseUntil)
            {
                SetCrawlingAnimation(false);
                yield return null;
                continue;
            }

            if (currentTarget == null && pickRoamPath != null)
            {
                Vector3[] path = pickRoamPath(transform.position);
                if (path != null && path.Length > 0)
                {
                    yield return Roam(path);
                    continue;
                }
            }

            if (currentTarget == null)
            {
                currentTargetIndex =
                    GetNextPosition();

                currentTarget =
                    crawlPositions[currentTargetIndex];

                waitingAtTarget = false;
                waitTimer = 0f;
            }


            // ----------------------------------------------------
            // Validate target
            // ----------------------------------------------------

            if (currentTarget == null)
            {
                Debug.LogWarning(
                    "BabyAI: Current crawl position is empty."
                );

                currentTarget = null;
                currentTargetIndex = -1;

                yield return null;
                continue;
            }


            // ====================================================
            // MOVE TOWARD CURRENT TARGET
            // ====================================================

            if (!waitingAtTarget)
            {
                SetCrawlingAnimation(true);

                yield return MoveToPosition(
                    currentTarget,
                    moveSpeed
                );


                // ------------------------------------------------
                // Clue interrupted movement.
                // ------------------------------------------------

                if (reactingToClue)
                {
                    SetCrawlingAnimation(false);
                    yield break;
                }


                // ------------------------------------------------
                // Reached target.
                // ------------------------------------------------

                waitingAtTarget = true;

                waitTimer = 0f;

                currentPosition =
                    currentTargetIndex;
            }


            // ====================================================
            // WAIT AT CURRENT TARGET
            // ====================================================

            SetCrawlingAnimation(false);


            while (
                waitTimer <
                waitTimeAtPosition
            )
            {
                // ------------------------------------------------
                // Clue interrupted waiting.
                // ------------------------------------------------

                if (reactingToClue)
                {
                    SetCrawlingAnimation(false);
                    yield break;
                }


                SetCrawlingAnimation(false);

                waitTimer +=
                    Time.deltaTime;

                yield return null;
            }


            // ----------------------------------------------------
            // Finished waiting.
            // Choose a new target.
            // ----------------------------------------------------

            Debug.Log(
                "Baby finished waiting at crawl position " +
                currentTargetIndex
            );


            currentTarget = null;

            currentTargetIndex = -1;

            waitingAtTarget = false;

            waitTimer = 0f;
        }
    }


    // ============================================================
    // MOVE TO CRAWL POSITION
    // ============================================================

    // Crawl along a path of points (from BabyLife), then sit for a random moment.
    private IEnumerator Roam(Vector3[] path)
    {
        foreach (Vector3 corner in path)
        {
            while (true)
            {
                if (Time.time < pauseUntil)
                {
                    SetCrawlingAnimation(false);
                    yield return null;
                    continue;
                }

                Vector3 target = new Vector3(corner.x, transform.position.y, corner.z);
                Vector3 direction = target - transform.position;
                if (direction.magnitude <= 0.08f) break;

                SetCrawlingAnimation(true);
                transform.position += direction.normalized * Mathf.Min(moveSpeed * Time.deltaTime, direction.magnitude);
                RotateTowards(direction);
                yield return null;
            }
        }

        SetCrawlingAnimation(false);
        float wait = Random.Range(roamWaitMin, roamWaitMax);
        for (float t = 0f; t < wait; t += Time.deltaTime) yield return null;
    }

    private IEnumerator MoveToPosition(
        Transform target,
        float speed
    )
    {
        if (target == null)
        {
            SetCrawlingAnimation(false);
            yield break;
        }


        while (true)
        {
            // ----------------------------------------------------
            // Clue interruption.
            // ----------------------------------------------------

            if (reactingToClue)
            {
                SetCrawlingAnimation(false);
                yield break;
            }


            Vector3 targetPosition =
                target.position;


            // Keep baby's current height.

            targetPosition.y =
                transform.position.y;


            Vector3 direction =
                targetPosition -
                transform.position;


            float distance =
                direction.magnitude;


            // ----------------------------------------------------
            // Reached target.
            //
            // IMPORTANT:
            // Do NOT snap transform.position.
            // ----------------------------------------------------

            if (distance <= arrivalDistance)
            {
                SetCrawlingAnimation(false);
                yield break;
            }


            // ----------------------------------------------------
            // Smooth movement.
            // ----------------------------------------------------

            float movementDistance =
                speed *
                Time.deltaTime;


            // Don't overshoot.

            if (movementDistance > distance)
            {
                movementDistance = distance;
            }


            transform.position +=
                direction.normalized *
                movementDistance;


            // ----------------------------------------------------
            // Face movement direction.
            // ----------------------------------------------------

            RotateTowards(direction);


            yield return null;
        }
    }


    // ============================================================
    // ANIMATION CONTROL
    // ============================================================

    // crawling = animation plays
    // stopped  = current crawl pose freezes

    private void SetCrawlingAnimation(
        bool crawling
    )
    {
        if (babyAnimator == null)
            return;


        if (crawling)
        {
            babyAnimator.speed = 1f;
        }
        else
        {
            babyAnimator.speed = idleAnimationSpeed;
        }
    }


    // ============================================================
    // VISION
    // ============================================================

    private bool CanSeeObject(
        Transform target
    )
    {
        if (babyEyePoint == null ||
            target == null)
        {
            return false;
        }


        // --------------------------------------------------------
        // Find target collider.
        // --------------------------------------------------------

        Collider targetCollider =
            target.GetComponent<Collider>();


        if (targetCollider == null)
        {
            targetCollider =
                target.GetComponentInChildren<Collider>();
        }


        if (targetCollider == null)
        {
            // Say this only once: this method runs every frame, and logging every frame makes the game lag.
            if (!warnedNoCollider)
            {
                warnedNoCollider = true;
                Debug.LogWarning(
                    "Baby cannot see " +
                    target.name +
                    " because it has no Collider."
                );
            }

            return false;
        }


        // --------------------------------------------------------
        // Find closest visible point.
        // --------------------------------------------------------

        Vector3 targetPoint =
            targetCollider.ClosestPoint(
                babyEyePoint.position
            );


        Vector3 direction =
            targetPoint -
            babyEyePoint.position;


        float distance =
            direction.magnitude;


        // --------------------------------------------------------
        // Too far.
        // --------------------------------------------------------

        if (distance > visionDistance)
            return false;


        // --------------------------------------------------------
        // Field of view.
        // --------------------------------------------------------

        float angle =
            Vector3.Angle(
                babyEyePoint.forward,
                direction.normalized
            );


        if (
            angle >
            visionAngle * 0.5f
        )
        {
            return false;
        }


        // --------------------------------------------------------
        // Raycast.
        // --------------------------------------------------------

        if (
            Physics.Raycast(
                babyEyePoint.position,
                direction.normalized,
                out RaycastHit hit,
                distance + 0.1f,
                visionLayers,
                QueryTriggerInteraction.Collide
            )
        )
        {
            // ----------------------------------------------------
            // Direct target hit.
            // ----------------------------------------------------

            if (
                hit.collider ==
                targetCollider
            )
            {
                Debug.DrawLine(
                    babyEyePoint.position,
                    hit.point,
                    Color.green,
                    1f
                );

                return true;
            }


            // ----------------------------------------------------
            // Target child hit.
            // ----------------------------------------------------

            if (
                hit.transform == target ||
                hit.transform.IsChildOf(target)
            )
            {
                Debug.DrawLine(
                    babyEyePoint.position,
                    hit.point,
                    Color.green,
                    1f
                );

                return true;
            }


            // ----------------------------------------------------
            // Something blocked Teddy.
            // ----------------------------------------------------

            Debug.DrawLine(
                babyEyePoint.position,
                hit.point,
                Color.red,
                1f
            );

            return false;
        }


        return false;
    }


    // ============================================================
    // NIGHTLIGHT REACTION
    // ============================================================

    private IEnumerator LampReactionRoutine()
    {
        Debug.Log(
            "[BABY] ================================="
        );

        Debug.Log(
            "[BABY] NIGHTLIGHT REACTION STARTED"
        );

        Debug.Log(
            "[BABY] ================================="
        );


        // --------------------------------------------------------
        // Freeze current crawl pose.
        // --------------------------------------------------------

        SetCrawlingAnimation(false);


        // --------------------------------------------------------
        // Stop exactly where baby currently is.
        // Turn toward light.
        // --------------------------------------------------------

        if (lamp != null)
        {
            Vector3 direction =
                lamp.position -
                transform.position;

            direction.y = 0f;


            if (
                direction.sqrMagnitude >
                0.001f
            )
            {
                while (
                    lampClue != null &&
                    lampClue.goalAchieved
                )
                {
                    direction =
                        lamp.position -
                        transform.position;

                    direction.y = 0f;


                    if (
                        direction.sqrMagnitude >
                        0.001f
                    )
                    {
                        Quaternion targetRotation =
                            Quaternion.LookRotation(
                                direction.normalized
                            );


                        transform.rotation =
                            Quaternion.RotateTowards(
                                transform.rotation,
                                targetRotation,
                                lampLookRotationSpeed *
                                Time.deltaTime
                            );
                    }


                    SetCrawlingAnimation(false);

                    yield return null;
                }
            }
        }


        Debug.Log(
            "[BABY] Baby is looking at the nightlight."
        );


        // --------------------------------------------------------
        // Stay stopped while light is active.
        // --------------------------------------------------------

        while (
            lampClue != null &&
            lampClue.goalAchieved
        )
        {
            SetCrawlingAnimation(false);

            yield return null;
        }


        // --------------------------------------------------------
        // Light turned off.
        // --------------------------------------------------------

        Debug.Log(
            "[BABY] Nightlight turned off."
        );


        // --------------------------------------------------------
        // Award points.
        // --------------------------------------------------------

        AwardPoints(lampClue);


        // --------------------------------------------------------
        // Reset reaction state.
        // --------------------------------------------------------

        if (lampClue != null)
        {
            lampClue.reactionStarted = false;
        }


        reactingToClue = false;


        // --------------------------------------------------------
        // Resume previous movement.
        // --------------------------------------------------------

        SetCrawlingAnimation(true);


        movementCoroutine =
            StartCoroutine(
                MovementRoutine()
            );


        Debug.Log(
            "[BABY] Nightlight reaction finished. " +
            "Resuming normal movement."
        );
    }


    // ============================================================
    // TEDDY REACTION
    // ============================================================

    private IEnumerator TeddyReactionRoutine()
    {
        Debug.Log(
            "[BABY] Baby saw Teddy!"
        );


        // --------------------------------------------------------
        // Crawl toward Teddy.
        // --------------------------------------------------------

        SetCrawlingAnimation(true);


        while (true)
        {
            if (teddy == null)
                break;


            Vector3 teddyPosition =
                teddy.position;


            // Keep baby at current height.

            teddyPosition.y =
                transform.position.y;


            Vector3 direction =
                teddyPosition -
                transform.position;


            float distance =
                direction.magnitude;


            // ----------------------------------------------------
            // Reached Teddy.
            //
            // No snapping.
            // No pickup.
            // No waiting.
            // ----------------------------------------------------

            if (
                distance <=
                teddyArrivalDistance
            )
            {
                SetCrawlingAnimation(false);

                Debug.Log(
                    "[BABY] Baby reached Teddy."
                );

                break;
            }


            // ----------------------------------------------------
            // Smooth movement toward Teddy.
            // ----------------------------------------------------

            float movementDistance =
                teddyMoveSpeed *
                Time.deltaTime;


            if (
                movementDistance >
                distance
            )
            {
                movementDistance =
                    distance;
            }


            transform.position +=
                direction.normalized *
                movementDistance;


            // Face Teddy.

            RotateTowards(direction);


            yield return null;
        }


        // --------------------------------------------------------
        // Teddy reaction complete.
        //
        // Baby does NOT:
        // - pick up Teddy
        // - move Teddy
        // - wait beside Teddy
        // - modify Teddy Rigidbody
        // --------------------------------------------------------

        SetCrawlingAnimation(false);


        Debug.Log(
            "[BABY] Finished reacting to Teddy."
        );


        // --------------------------------------------------------
        // Award points.
        // --------------------------------------------------------

        AwardPoints(teddyClue);


        // --------------------------------------------------------
        // IMPORTANT:
        //
        // Do NOT allow Teddy to immediately trigger again.
        //
        // teddyHandledUntilReset stays TRUE until the
        // Teddy ClueGoal becomes FALSE.
        // --------------------------------------------------------

        if (teddyClue != null)
        {
            teddyClue.reactionStarted = false;
        }


        // --------------------------------------------------------
        // Resume normal movement.
        //
        // currentTarget was never cleared.
        // Therefore baby continues toward the same position.
        // --------------------------------------------------------

        reactingToClue = false;


        SetCrawlingAnimation(true);


        movementCoroutine =
            StartCoroutine(
                MovementRoutine()
            );
    }


    // ============================================================
    // WINDBOX REACTION
    // ============================================================

    private IEnumerator WindboxReactionRoutine()
    {
        // --------------------------------------------------------
        // Freeze current crawl pose.
        // --------------------------------------------------------

        SetCrawlingAnimation(false);


        Debug.Log(
            "[BABY] Baby heard the Windbox!"
        );


        // --------------------------------------------------------
        // Baby DOES NOT move toward Windbox.
        //
        // Baby stays exactly where it is.
        // Turn toward sound source.
        // --------------------------------------------------------

        if (windbox != null)
        {
            Vector3 direction =
                windbox.position -
                transform.position;

            direction.y = 0f;


            if (
                direction.sqrMagnitude >
                0.001f
            )
            {
                while (
                    windboxClue != null &&
                    windboxClue.goalAchieved
                )
                {
                    direction =
                        windbox.position -
                        transform.position;

                    direction.y = 0f;


                    if (
                        direction.sqrMagnitude >
                        0.001f
                    )
                    {
                        Quaternion targetRotation =
                            Quaternion.LookRotation(
                                direction.normalized
                            );


                        transform.rotation =
                            Quaternion.RotateTowards(
                                transform.rotation,
                                targetRotation,
                                rotationSpeed *
                                Time.deltaTime
                            );
                    }


                    SetCrawlingAnimation(false);

                    yield return null;
                }
            }
        }


        Debug.Log(
            "[BABY] Baby is listening to the Windbox."
        );


        // --------------------------------------------------------
        // Stay stopped while Windbox is active.
        // --------------------------------------------------------

        while (
            windboxClue != null &&
            windboxClue.goalAchieved
        )
        {
            SetCrawlingAnimation(false);

            yield return null;
        }


        Debug.Log(
            "[BABY] Baby stopped listening to the Windbox."
        );


        // --------------------------------------------------------
        // Award points.
        // --------------------------------------------------------

        AwardPoints(windboxClue);


        // --------------------------------------------------------
        // Reset reaction state.
        // --------------------------------------------------------

        if (windboxClue != null)
        {
            windboxClue.reactionStarted = false;
        }


        reactingToClue = false;


        // --------------------------------------------------------
        // Resume same target / same wait.
        // --------------------------------------------------------

        SetCrawlingAnimation(true);


        movementCoroutine =
            StartCoroutine(
                MovementRoutine()
            );
    }


    // ============================================================
    // SCORE
    // ============================================================

    private void AwardPoints(
        ClueGoal clue
    )
    {
        if (clue == null)
            return;


        // Prevent duplicate scoring.

        if (clue.pointsAwarded)
        {
            Debug.Log(
                "[BABY SCORE] " +
                clue.name +
                " was already awarded."
            );

            return;
        }


        // --------------------------------------------------------
        // Add points.
        // HARD CAP = 60
        // --------------------------------------------------------

        totalPoints +=
            clue.points;

        totalPoints =
            Mathf.Clamp(
                totalPoints,
                0,
                SLIDER_MAX
            );


        clue.pointsAwarded = true;


        UpdateProgressSlider();


        Debug.Log(
            "[BABY SCORE] " +
            clue.name +
            " awarded " +
            clue.points +
            " points. " +
            "Total = " +
            totalPoints +
            "/" +
            SLIDER_MAX
        );
    }


    // ============================================================
    // UPDATE SCORE SLIDER
    // ============================================================

    private void UpdateProgressSlider()
    {
        if (progressSlider == null)
            return;


        progressSlider.minValue =
            0;


        progressSlider.maxValue =
            SLIDER_MAX;


        progressSlider.value =
            Mathf.Clamp(
                totalPoints,
                0,
                SLIDER_MAX
            );
    }


    // ============================================================
    // ROTATION
    // ============================================================

    private void RotateTowards(
        Vector3 direction
    )
    {
        direction.y = 0f;


        if (
            direction.sqrMagnitude <=
            0.001f
        )
        {
            return;
        }


        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction.normalized
            );


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed *
                Time.deltaTime
            );
    }


    // ============================================================
    // RANDOM CRAWL POSITION
    // ============================================================

    private int GetNextPosition()
    {
        if (currentPosition == -1)
        {
            return Random.Range(
                0,
                crawlPositions.Length
            );
        }


        int nextPosition;


        do
        {
            nextPosition =
                Random.Range(
                    0,
                    crawlPositions.Length
                );

        } while (
            nextPosition ==
            currentPosition
        );


        return nextPosition;
    }


    // ============================================================
    // DEBUG GIZMOS
    // ============================================================

    private void OnDrawGizmos()
    {
        if (babyEyePoint == null)
            return;


        // --------------------------------------------------------
        // Forward vision
        // --------------------------------------------------------

        Gizmos.color =
            Color.yellow;


        Gizmos.DrawRay(
            babyEyePoint.position,
            babyEyePoint.forward *
            visionDistance
        );


        // --------------------------------------------------------
        // Teddy
        // --------------------------------------------------------

        if (teddy != null)
        {
            Gizmos.color =
                Color.red;


            Gizmos.DrawLine(
                babyEyePoint.position,
                teddy.position
            );
        }


        // --------------------------------------------------------
        // Windbox
        // --------------------------------------------------------

        if (windbox != null)
        {
            Gizmos.color =
                Color.cyan;


            Gizmos.DrawLine(
                babyEyePoint.position,
                windbox.position
            );
        }


        // --------------------------------------------------------
        // Nightlight
        // --------------------------------------------------------

        if (lamp != null)
        {
            Gizmos.color =
                Color.yellow;


            Gizmos.DrawLine(
                babyEyePoint.position,
                lamp.position
            );
        }
    }
}