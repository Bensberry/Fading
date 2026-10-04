using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotoStackInteractable : MonoBehaviour
{
    [Header("Paper Stack")]
    [Tooltip("Element 0 is the original top paper.")]
    public List<Transform> papers = new List<Transform>();

    public float paperSpacing = 0.003f;


    [Header("Paper Rotation")]
    public float maxRandomRotation = 10f;


    [Header("Interaction")]
    [Tooltip("Maximum distance the raycast can reach.")]
    public float raycastDistance = 3f;

    [Tooltip("Layers that the raycast can hit.")]
    public LayerMask raycastLayers = ~0;

    public GameObject promptUI;


    [Header("Camera Positions")]
    public Transform handSlot;
    public Transform inspectSlot;


    [Header("Stack Reset")]
    public float resetDelay = 5f;


    [Header("Clue")]
    public string cluePhotoName = "WeddingPhoto";


    private Transform playerCamera;
    private Transform originalParent;
    private ClueGoal clueGoal;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalScale;

    private List<Transform> originalPaperOrder =
        new List<Transform>();

    private Dictionary<Transform, float> paperRotations =
        new Dictionary<Transform, float>();

    private bool isHeld;
    private bool isInspecting;

    private Coroutine resetCoroutine;


    void Start()
    {
        originalParent = transform.parent;

        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalScale = transform.localScale;

        clueGoal = GetComponent<ClueGoal>();

        if (clueGoal == null)
        {
            Debug.LogError(
                $"{name}: ClueGoal component is missing!"
            );
        }


        originalPaperOrder =
            new List<Transform>(papers);


        foreach (Transform paper in papers)
        {
            if (paper != null &&
                !paperRotations.ContainsKey(paper))
            {
                paperRotations[paper] =
                    Random.Range(
                        -maxRandomRotation,
                        maxRandomRotation
                    );
            }
        }


        // Find the player's camera.
        if (Camera.main == null)
        {
            Debug.LogError(
                "PhotoStackInteractable: No Main Camera found!"
            );

            enabled = false;
            return;
        }


        playerCamera = Camera.main.transform;


        CreateCameraSlots();


        if (promptUI != null)
            promptUI.SetActive(false);


        // IMPORTANT:
        // Do NOT arrange the papers here.
        //
        // This prevents the photo stack from moving
        // to a different position when the game starts.
        DeactivateClue();
    }


    void Update()
    {
        if (playerCamera == null)
            return;


        // =====================================================
        // NOT HOLDING THE PHOTO STACK
        // =====================================================

        if (!isHeld)
        {
            bool lookingAtStack =
                IsLookingAtPhotoStack();


            // Show prompt only when the raycast is actually
            // hitting this photo stack.
            if (promptUI != null)
                promptUI.SetActive(lookingAtStack);


            // Pickup only if:
            // 1. The player is looking directly at the stack.
            // 2. E is pressed.
            if (lookingAtStack &&
                Input.GetKeyDown(KeyCode.E))
            {
                PickUpStack();
            }


            return;
        }


        // =====================================================
        // HOLDING THE PHOTO STACK
        // =====================================================

        if (promptUI != null)
            promptUI.SetActive(false);


        // Right-click toggles hand/inspection view.
        if (Input.GetMouseButtonDown(1))
        {
            if (isInspecting)
                ReturnToHand();
            else
                InspectStack();
        }


        // Left-click cycles photos only while inspecting.
        if (isInspecting &&
            Input.GetMouseButtonDown(0))
        {
            CycleTopPaper();
        }


        // E places the stack only when in hand.
        if (!isInspecting &&
            Input.GetKeyDown(KeyCode.E))
        {
            PutStackDown();
        }
    }


    // =========================================================
    // RAYCAST
    // =========================================================

    bool IsLookingAtPhotoStack()
    {
        if (playerCamera == null)
            return false;


        Ray ray = new Ray(
            playerCamera.position,
            playerCamera.forward
        );


        RaycastHit hit;


        bool hitSomething = Physics.Raycast(
            ray,
            out hit,
            raycastDistance,
            raycastLayers,
            QueryTriggerInteraction.Collide
        );


        if (!hitSomething)
            return false;


        // The raycast may hit a collider on a child
        // of the photo stack.
        //
        // Check whether the object hit belongs to
        // this PhotoStackInteractable.
        PhotoStackInteractable hitStack =
            hit.collider.GetComponentInParent<PhotoStackInteractable>();


        if (hitStack == this)
            return true;


        return false;
    }


    // =========================================================
    // CAMERA SLOTS
    // =========================================================

    void CreateCameraSlots()
    {
        handSlot = GetOrCreateSlot(
            "PhotoStackHandSlot",
            new Vector3(
                0.4f,
                -0.35f,
                0.8f
            )
        );


        inspectSlot = GetOrCreateSlot(
            "PhotoStackInspectSlot",
            new Vector3(
                0f,
                0f,
                1.2f
            )
        );
    }


    Transform GetOrCreateSlot(
        string slotName,
        Vector3 localPosition
    )
    {
        Transform existingSlot =
            playerCamera.Find(slotName);


        if (existingSlot != null)
            return existingSlot;


        GameObject slot =
            new GameObject(slotName);


        slot.transform.SetParent(
            playerCamera
        );


        slot.transform.localPosition =
            localPosition;


        slot.transform.localRotation =
            Quaternion.identity;


        return slot.transform;
    }


    // =========================================================
    // PICKUP
    // =========================================================

    void PickUpStack()
    {
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
        }


        isHeld = true;
        isInspecting = false;


        DeactivateClue();


        MoveToSlot(handSlot);


        Debug.Log(
            "Photo stack picked up. Current top: " +
            GetTopPhotoName()
        );
    }


    // =========================================================
    // INSPECTION
    // =========================================================

    void InspectStack()
    {
        isInspecting = true;


        MoveToSlot(inspectSlot);


        Debug.Log(
            "Photo stack brought into view. Current top: " +
            GetTopPhotoName()
        );
    }


    void ReturnToHand()
    {
        isInspecting = false;


        MoveToSlot(handSlot);


        Debug.Log(
            "Photo stack returned to hand."
        );
    }


    // =========================================================
    // MOVE TO CAMERA SLOT
    // =========================================================

    void MoveToSlot(Transform targetSlot)
    {
        transform.SetParent(
            targetSlot,
            false
        );


        transform.localPosition =
            Vector3.zero;


        // Rotate the XZ paper plane to face
        // the camera.
        transform.localRotation =
            Quaternion.Euler(
                90f,
                0f,
                0f
            );


        transform.localScale =
            originalScale;


        ArrangePapers();
    }


    // =========================================================
    // PUT DOWN
    // =========================================================

    void PutStackDown()
    {
        isHeld = false;
        isInspecting = false;


        // Restore the stack's original table transform.
        transform.SetParent(
            originalParent,
            false
        );


        transform.position =
            originalPosition;


        transform.rotation =
            originalRotation;


        transform.localScale =
            originalScale;


        // Keep the CURRENT photo order when placing it.
        ArrangePapers();


        Debug.Log(
            "Photo stack placed. Current top: " +
            GetTopPhotoName()
        );


        // Check clue only when placed.
        UpdatePlacedClueState();


        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);


        resetCoroutine =
            StartCoroutine(
                ResetStackAfterDelay()
            );
    }


    // =========================================================
    // CYCLE PHOTOS
    // =========================================================

    void CycleTopPaper()
    {
        if (papers == null ||
            papers.Count <= 1)
            return;


        // Move currently visible top paper
        // to the bottom.
        Transform topPaper =
            papers[0];


        papers.RemoveAt(0);
        papers.Add(topPaper);


        ArrangePapers();


        Debug.Log(
            "Photo cycled. Current top: " +
            GetTopPhotoName()
        );
    }


    // =========================================================
    // RESET
    // =========================================================

    IEnumerator ResetStackAfterDelay()
    {
        yield return new WaitForSeconds(
            resetDelay
        );


        if (!isHeld)
        {
            // End the clue window.
            DeactivateClue();


            // Restore original photo order.
            papers =
                new List<Transform>(
                    originalPaperOrder
                );


            ArrangePapers();


            Debug.Log(
                "Stack reset. Original top: " +
                GetTopPhotoName()
            );
        }


        resetCoroutine = null;
    }


    // =========================================================
    // CLUE
    // =========================================================

    void UpdatePlacedClueState()
    {
        if (clueGoal == null)
            return;


        bool weddingPhotoIsTop =
            papers != null &&
            papers.Count > 0 &&
            papers[0] != null &&
            papers[0].name == cluePhotoName;


        if (!isHeld &&
            weddingPhotoIsTop)
        {
            clueGoal.ActivateClue();


            Debug.Log(
                $"[PHOTO STACK CLUE] Placed with " +
                $"{cluePhotoName} on top. " +
                $"goalAchieved = " +
                $"{clueGoal.goalAchieved}. " +
                $"Reset in {resetDelay} seconds."
            );
        }
        else
        {
            DeactivateClue();


            Debug.Log(
                $"[PHOTO STACK CLUE] Placed, but top paper is " +
                $"{GetTopPhotoName()}. " +
                "goalAchieved = false."
            );
        }
    }


    void DeactivateClue()
    {
        if (clueGoal != null)
        {
            clueGoal.DeactivateClue();


            Debug.Log(
                $"[PHOTO STACK CLUE] goalAchieved = " +
                $"{clueGoal.goalAchieved}"
            );
        }
    }


    // =========================================================
    // ARRANGE PAPERS
    // =========================================================

    void ArrangePapers()
    {
        for (int i = 0;
             i < papers.Count;
             i++)
        {
            Transform paper =
                papers[i];


            if (paper == null)
                continue;


            paper.SetParent(
                transform,
                false
            );


            // Stack papers with a slight
            // vertical separation.
            paper.localPosition =
                Vector3.down *
                (i * paperSpacing);


            // Preserve each paper's
            // assigned random rotation.
            float rotation = 0f;


            if (paperRotations.ContainsKey(paper))
            {
                rotation =
                    paperRotations[paper];
            }


            paper.localRotation =
                Quaternion.Euler(
                    0f,
                    rotation,
                    0f
                );


            // Top paper should render above
            // the others.
            paper.SetSiblingIndex(
                papers.Count - 1 - i
            );
        }
    }


    // =========================================================
    // TOP PHOTO
    // =========================================================

    string GetTopPhotoName()
    {
        if (papers == null ||
            papers.Count == 0 ||
            papers[0] == null)
        {
            return "None";
        }


        return papers[0].name;
    }
}