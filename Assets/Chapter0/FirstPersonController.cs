using UnityEngine;
using UnityEngine.InputSystem;

// First-person walking for the player (ghost).
// WASD = move
// Left Shift = run
// Mouse = look around: HOLD THE RIGHT MOUSE BUTTON and move the mouse (tick 'Hold Right Mouse To Look' off for free mouse look)

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 4.5f;

    [Tooltip("Higher = reaches full speed / stops faster.")]
    public float acceleration = 12f;

    public float gravity = -9.81f;

    [Header("Body (set in code so the prefab values don't matter)")]
    [Tooltip("Highest step the player can walk up (metres). Keep it small so low furniture can't be climbed.")]
    public float stepOffset = 0.15f;

    [Tooltip("Collision padding.")]
    public float skinWidth = 0.08f; // Recommended 0.08f to prevent mesh overlaps!

    [Header("Look Settings")]
    [Tooltip("Degrees turned per mouse count.")]
    public float lookSensitivity = 0.1f;

    [Tooltip("ON: you only look around while the right mouse button is held (the cursor is free otherwise). OFF: the mouse always looks.")]
    public bool holdRightMouseToLook = true;

    public Transform playerCameraRoot; // Drag your 'Head' object here

    // While true the player cannot walk (but can still look around): sitting on a bench, swinging, sliding (RestSpot).
    [HideInInspector] public bool movementLocked;

    [Header("Safety Net")]
    [Tooltip("If the player falls below this height, they are teleported back to where they started.")]
    public float fallLimitY = -5f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float verticalRotation = 0f;

    private bool hasLanded;
    private float floorY;
    private bool lookActive;
    private bool skipLookFrame;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Force CharacterController settings
        controller.stepOffset = stepOffset;
        controller.skinWidth = skinWidth;

        // Remember starting position and rotation
        startPosition = transform.position;
        startRotation = transform.rotation;

        SetCursorLocked(!holdRightMouseToLook);        // free mouse look hides the cursor; right-mouse look keeps it visible
    }

    void OnDisable()
    {
        lookActive = false;
        SetCursorLocked(false);
    }

    void Update()
    {
        HandleMouseLook();
        if (movementLocked) return;
        HandleMovement();
        RespawnIfFallen();
    }

    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // =========================================================
    // MOUSE LOOK
    // =========================================================
    void HandleMouseLook()
    {
        if (MobileControls.Active)                                   // phone: drag on the right half of the screen to look
        {
            LookBy(MobileControls.LookDegrees.x, MobileControls.LookDegrees.y);
            return;
        }
        if (Mouse.current == null) return;

        if (holdRightMouseToLook)
        {
            bool holding = Mouse.current.rightButton.isPressed;
            if (holding != lookActive)
            {
                lookActive = holding;
                skipLookFrame = holding;               // locking the cursor makes the first frame jump, so ignore it
                SetCursorLocked(holding);
            }
            if (!lookActive) return;
            if (skipLookFrame) { skipLookFrame = false; return; }
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        LookBy(mouseDelta.x * lookSensitivity, mouseDelta.y * lookSensitivity);
    }

    void LookBy(float yawDegrees, float pitchDegrees)
    {

        // Horizontal rotation
        transform.Rotate(Vector3.up * yawDegrees);

        // Vertical rotation
        verticalRotation -= pitchDegrees;
        verticalRotation = Mathf.Clamp(verticalRotation, -89f, 89f);

        if (playerCameraRoot != null)
        {
            playerCameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    // =========================================================
    // MOVEMENT
    // =========================================================
    void HandleMovement()
    {
        if (Keyboard.current == null && !MobileControls.Active) return;

        Vector3 wanted = ReadWantedDirection() * CurrentSpeed();
        // Phones report uneven frame times (12 ms, 21 ms, ...) even at a steady 60 fps: walking with them judders.
        // The smoothed frame time keeps every step the same size.
        float dt = MobileControls.Active ? Time.smoothDeltaTime : Time.deltaTime;

        // Accelerate / Decelerate
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            wanted,
            acceleration * dt
        );

        // Ground check & floor lock logic combined BEFORE Move() call
        if (controller.isGrounded)
        {
            if (!hasLanded)
            {
                hasLanded = true;
                floorY = transform.position.y;
            }

            // Keep vertical velocity small and stable while grounded
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * dt;
        }

        // Clamp downward movement if trying to sink below initial floor Y
        Vector3 movementVector = horizontalVelocity + (Vector3.up * verticalVelocity);

        // (Outside the house the ground is lower than the floor: if there is ground below, just fall onto it.)
        if (hasLanded && transform.position.y <= floorY && verticalVelocity < 0f && !GroundBelow())
        {
            // Zero out downward gravity to prevent sinking below floorY
            movementVector.y = Mathf.Max(movementVector.y, 0f);
        }

        // Single Move call per frame eliminates stutter
        controller.Move(movementVector * dt);
    }

    // Is there something to stand on within 3 m below the player? (then gravity may work normally)
    bool GroundBelow()
    {
        Vector3 from = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(from, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
    }

    // =========================================================
    // MOVEMENT INPUT
    // =========================================================
    Vector3 ReadWantedDirection()
    {
        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed) moveX -= 1f;
            if (Keyboard.current.dKey.isPressed) moveX += 1f;
            if (Keyboard.current.sKey.isPressed) moveZ -= 1f;
            if (Keyboard.current.wKey.isPressed) moveZ += 1f;
        }
        moveX += MobileControls.Move.x;                              // the on-screen joystick (phones)
        moveZ += MobileControls.Move.y;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        return Vector3.ClampMagnitude(move, 1f);
    }

    // =========================================================
    // WALK / RUN SPEED
    // =========================================================
    float CurrentSpeed()
    {
        bool running = (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) || MobileControls.Running;
        float speed = running ? runSpeed : walkSpeed;
        return speed * AbilityLoss.SpeedMultiplier;
    }

    // =========================================================
    // FALL / RESPAWN
    // =========================================================
    void RespawnIfFallen()
    {
        if (transform.position.y > fallLimitY) return;

        controller.enabled = false;
        transform.SetPositionAndRotation(startPosition, startRotation);
        controller.enabled = true;

        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        hasLanded = false;
    }
}