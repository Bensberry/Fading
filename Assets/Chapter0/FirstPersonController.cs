using UnityEngine;
using UnityEngine.InputSystem;

// First-person walking for the player (ghost).
// WASD = move
// Left Shift = run
// Mouse = look around (no mouse button required)

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

    public Transform playerCameraRoot; // Drag your 'Head' object here

    [Header("Safety Net")]
    [Tooltip("If the player falls below this height, they are teleported back to where they started.")]
    public float fallLimitY = -5f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float verticalRotation = 0f;

    private bool hasLanded;
    private float floorY;

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

        SetCursorLocked(true);
    }

    void OnDisable()
    {
        SetCursorLocked(false);
    }

    void Update()
    {
        HandleMouseLook();
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
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // Horizontal rotation
        transform.Rotate(Vector3.up * (mouseDelta.x * lookSensitivity));

        // Vertical rotation
        verticalRotation -= mouseDelta.y * lookSensitivity;
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
        if (Keyboard.current == null) return;

        Vector3 wanted = ReadWantedDirection() * CurrentSpeed();

        // Accelerate / Decelerate
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            wanted,
            acceleration * Time.deltaTime
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
            verticalVelocity += gravity * Time.deltaTime;
        }

        // Clamp downward movement if trying to sink below initial floor Y
        Vector3 movementVector = horizontalVelocity + (Vector3.up * verticalVelocity);

        if (hasLanded && transform.position.y <= floorY && verticalVelocity < 0f)
        {
            // Zero out downward gravity to prevent sinking below floorY
            movementVector.y = Mathf.Max(movementVector.y, 0f);
        }

        // Single Move call per frame eliminates stutter
        controller.Move(movementVector * Time.deltaTime);
    }

    // =========================================================
    // MOVEMENT INPUT
    // =========================================================
    Vector3 ReadWantedDirection()
    {
        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current.aKey.isPressed) moveX -= 1f;
        if (Keyboard.current.dKey.isPressed) moveX += 1f;
        if (Keyboard.current.sKey.isPressed) moveZ -= 1f;
        if (Keyboard.current.wKey.isPressed) moveZ += 1f;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        return move.normalized;
    }

    // =========================================================
    // WALK / RUN SPEED
    // =========================================================
    float CurrentSpeed()
    {
        bool running = Keyboard.current.leftShiftKey.isPressed;
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