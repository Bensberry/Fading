using UnityEngine;

public class PhotoInteractable : MonoBehaviour
{
    private Transform playerCameraTransform;
    private Transform handSlot;
    private Transform inspectSlot;

    private HoldableItem holdableItem;
    private ClueGoal clueGoal;

    private bool isInspecting;
    private bool wasHeld;


    void Start()
    {
        holdableItem = GetComponent<HoldableItem>();
        clueGoal = GetComponent<ClueGoal>();

        if (holdableItem == null)
            Debug.LogError(
                $"{name}: HoldableItem component is missing."
            );

        if (clueGoal == null)
            Debug.LogError(
                $"{name}: ClueGoal component is missing."
            );


        if (Camera.main == null)
        {
            Debug.LogError(
                "PhotoInteractable: No Main Camera found!"
            );

            enabled = false;
            return;
        }


        playerCameraTransform =
            Camera.main.transform;


        // =====================================================
        // HAND SLOT
        // =====================================================

        handSlot =
            playerCameraTransform.Find(
                "HandSlot"
            );


        if (handSlot == null)
        {
            GameObject hand =
                new GameObject("HandSlot");

            hand.transform.SetParent(
                playerCameraTransform
            );

            hand.transform.localPosition =
                new Vector3(
                    1f,
                    -1.3f,
                    2f
                );

            hand.transform.localRotation =
                Quaternion.identity;

            handSlot =
                hand.transform;
        }


        // =====================================================
        // INSPECT SLOT
        // =====================================================

        inspectSlot =
            playerCameraTransform.Find(
                "InspectSlot"
            );


        if (inspectSlot == null)
        {
            GameObject inspect =
                new GameObject(
                    "InspectSlot"
                );

            inspect.transform.SetParent(
                playerCameraTransform
            );

            inspect.transform.localPosition =
                new Vector3(
                    0f,
                    -0.2f,
                    0.5f
                );

            inspect.transform.localRotation =
                Quaternion.Euler(
                    180f,
                    0f,
                    180f
                );

            inspectSlot =
                inspect.transform;
        }


        // Listen for the HoldableItem's automatic return.
        if (holdableItem != null)
        {
            holdableItem.OnReturnedToOriginalPosition +=
                HandlePhotoReturned;
        }


        wasHeld =
            holdableItem != null &&
            holdableItem.IsHeld;
    }


    void OnDestroy()
    {
        // Prevent event reference from remaining subscribed.
        if (holdableItem != null)
        {
            holdableItem.OnReturnedToOriginalPosition -=
                HandlePhotoReturned;
        }
    }


    void Update()
    {
        if (holdableItem == null)
            return;


        bool isHeldNow =
            holdableItem.IsHeld;


        // =====================================================
        // PHOTO HAS JUST BEEN THROWN
        // =====================================================

        if (wasHeld && !isHeldNow)
        {
            isInspecting = false;


            if (clueGoal != null)
            {
                clueGoal.ActivateClue();


                Debug.Log(
                    $"[PHOTO CLUE] {name} thrown. " +
                    $"goalAchieved = " +
                    $"{clueGoal.goalAchieved}"
                );
            }
        }


        // IMPORTANT:
        //
        // We NO LONGER deactivate the clue here
        // when the player picks the photo up.
        //
        // The clue remains active until the photo
        // automatically returns to its original position.


        wasHeld = isHeldNow;


        // =====================================================
        // INSPECT PHOTO
        // =====================================================

        if (isHeldNow &&
            Input.GetMouseButtonDown(0))
        {
            ToggleInspectMode();
        }
    }


    // =========================================================
    // PHOTO AUTOMATICALLY RETURNED
    // =========================================================

    void HandlePhotoReturned()
    {
        isInspecting = false;


        if (clueGoal != null)
        {
            clueGoal.DeactivateClue();


            Debug.Log(
                $"[PHOTO CLUE] {name} automatically returned " +
                $"to original position. " +
                $"goalAchieved = " +
                $"{clueGoal.goalAchieved}"
            );
        }
    }


    // =========================================================
    // INSPECTION
    // =========================================================

    void ToggleInspectMode()
    {
        isInspecting =
            !isInspecting;


        Transform target =
            isInspecting
                ? inspectSlot
                : handSlot;


        transform.SetParent(
            target,
            false
        );


        transform.localPosition =
            Vector3.zero;


        transform.localRotation =
            Quaternion.identity;


        Debug.Log(
            isInspecting
                ? "Photo is being viewed."
                : "Photo returned to hand."
        );
    }
}