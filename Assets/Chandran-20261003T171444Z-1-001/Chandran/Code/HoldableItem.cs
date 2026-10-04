using UnityEngine;
using System;
using System.Collections;

public class HoldableItem : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 4f;
    public GameObject promptUI;

    [Header("Throw Settings")]
    public float throwForce = 8f;
    public float upwardForce = 0.15f;
    public float extraDownwardForce = 5f;

    [Header("Return Settings")]
    public float returnDelay = 5f;
    public float returnDuration = 2f;
    public float returnSpinDegrees = 720f;

    private Transform playerCameraTransform;
    private Transform handSlot;

    private bool isPlayerNearby;
    private bool isHeld;
    private bool isReturning;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Transform originalParent;
    private Vector3 originalLocalScale;

    private Rigidbody rb;
    private Collider col;

    private Coroutine returnCoroutine;

    // Event fired when the object has completely returned
    // to its original position.
    public event Action OnReturnedToOriginalPosition;

    public bool IsHeld => isHeld;
    public Transform HandSlot => handSlot;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // Store the original transform.
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalParent = transform.parent;
        originalLocalScale = transform.localScale;

        // Find the player's camera.
        if (Camera.main != null)
        {
            playerCameraTransform = Camera.main.transform;

            // Find existing HandSlot.
            handSlot = playerCameraTransform.Find("HandSlot");

            // Create HandSlot if it doesn't exist.
            if (handSlot == null)
            {
                GameObject newHand = new GameObject("HandSlot");

                newHand.transform.SetParent(playerCameraTransform);

                newHand.transform.localPosition =
                    new Vector3(0.1f, -0.1f, 0.1f);

                newHand.transform.localRotation =
                    Quaternion.Euler(180f, 0f, 0f);

                handSlot = newHand.transform;
            }
        }

        // Hide interaction prompt at the beginning.
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    void Update()
    {
        // Don't interact while the object is returning.
        if (playerCameraTransform == null || isReturning)
            return;

        // --------------------------------------------------
        // OBJECT IS NOT HELD
        // --------------------------------------------------

        if (!isHeld)
        {
            float distance = Vector3.Distance(
                transform.position,
                playerCameraTransform.position
            );

            isPlayerNearby = distance <= interactionRange;

            if (promptUI != null)
                promptUI.SetActive(isPlayerNearby);

            // Press E to pick up.
            if (isPlayerNearby && Input.GetKeyDown(KeyCode.E))
            {
                PickUpItem();
            }
        }

        // --------------------------------------------------
        // OBJECT IS HELD
        // --------------------------------------------------

        else
        {
            if (promptUI != null)
                promptUI.SetActive(false);

            // Press E to throw.
            if (Input.GetKeyDown(KeyCode.E))
            {
                ThrowItem();
            }
        }
    }

    // ======================================================
    // PICK UP
    // ======================================================

    void PickUpItem()
    {
        if (handSlot == null)
            return;

        isHeld = true;

        // If a return coroutine is currently running,
        // stop it.
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        if (promptUI != null)
            promptUI.SetActive(false);

        // Stop physics.
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            rb.useGravity = false;
            rb.detectCollisions = false;
        }

        // Disable collider while held.
        if (col != null)
            col.enabled = false;

        // Attach to player's hand.
        transform.SetParent(handSlot, false);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        Debug.Log(gameObject.name + " picked up.");
    }

    // ======================================================
    // THROW
    // ======================================================

    void ThrowItem()
    {
        if (!isHeld || isReturning)
            return;

        isHeld = false;

        // Detach from the hand or inspection slot.
        transform.SetParent(null, true);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Throw forward from the camera.
            Vector3 direction =
                playerCameraTransform.forward +
                Vector3.up * upwardForce;

            direction.Normalize();

            rb.AddForce(
                direction * throwForce,
                ForceMode.Impulse
            );

            // Extra downward force.
            rb.AddForce(
                Vector3.down * extraDownwardForce,
                ForceMode.Impulse
            );

            // Natural tumbling.
            rb.AddTorque(
                UnityEngine.Random.insideUnitSphere * 2f,
                ForceMode.Impulse
            );
        }

        // Enable collider after throwing.
        if (col != null)
            col.enabled = true;

        // Start return timer.
        if (returnCoroutine != null)
            StopCoroutine(returnCoroutine);

        returnCoroutine = StartCoroutine(
            WaitThenReturn()
        );

        Debug.Log("Item thrown. Return timer started.");
    }

    // ======================================================
    // WAIT THEN RETURN
    // ======================================================

    IEnumerator WaitThenReturn()
    {
        // Let the object remain thrown for the delay.
        yield return new WaitForSeconds(returnDelay);

        isReturning = true;

        // Stop physics before smooth return.
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Disable collider during return.
        if (col != null)
            col.enabled = false;

        // Smoothly return to original position.
        yield return StartCoroutine(
            SmoothReturn()
        );

        // --------------------------------------------------
        // RESTORE ORIGINAL TRANSFORM
        // --------------------------------------------------

        transform.SetParent(
            originalParent,
            true
        );

        transform.position = originalPosition;
        transform.rotation = originalRotation;
        transform.localScale = originalLocalScale;

        // --------------------------------------------------
        // RESTORE PHYSICS
        // --------------------------------------------------

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.detectCollisions = true;
        }

        if (col != null)
            col.enabled = true;

        isReturning = false;
        returnCoroutine = null;

        Debug.Log(
            "Item smoothly returned to its original position."
        );

        // --------------------------------------------------
        // NOTIFY LISTENERS
        // --------------------------------------------------

        // PhotoInteractable listens to this event.
        // This is what makes the photo clue become false
        // ONLY after the photo has completely returned.
        OnReturnedToOriginalPosition?.Invoke();
    }

    // ======================================================
    // SMOOTH RETURN
    // ======================================================

    IEnumerator SmoothReturn()
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;

        float elapsed = 0f;

        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / returnDuration
            );

            float smoothT = Mathf.SmoothStep(
                0f,
                1f,
                t
            );

            // Move toward original position.
            transform.position = Vector3.Lerp(
                startPosition,
                originalPosition,
                smoothT
            );

            // Rotate toward original rotation.
            Quaternion baseRotation =
                Quaternion.Slerp(
                    startRotation,
                    originalRotation,
                    smoothT
                );

            // Add spinning effect.
            float spin =
                returnSpinDegrees * t;

            transform.rotation =
                baseRotation *
                Quaternion.Euler(
                    0f,
                    spin,
                    0f
                );

            yield return null;
        }

        // Guarantee exact final transform.
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }
}