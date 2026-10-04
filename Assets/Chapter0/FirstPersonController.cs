using UnityEngine;
using UnityEngine.InputSystem;

// First-person walking for the player (ghost).
// WASD = move
// Left Shift = run
// Mouse = look around (no mouse button required)
//
// Gravity is on, but the player can never sink below the floor level
// they started on, and if they fall out of the house they are returned
// to their starting point.

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

    [Tooltip("Collision padding. The player prefab is scaled down, so this must be tiny.")]
    public float skinWidth = 0.02f;


    [Header("Look Settings")]

    [Tooltip("Degrees turned per mouse count. 0.1 = typical FPS default, 0.05 = slower, 0.2 = fast.")]
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

        // Force the CharacterController settings from this script.
        controller.stepOffset = stepOffset;
        controller.skinWidth = skinWidth;

        // Remember starting position and rotation.
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Lock the cursor for FPS-style mouse look.
        SetCursorLocked(true);
    }


    void OnDisable()
    {
        // Unlock cursor if the player is disabled,
        // for example during a cutscene.
        SetCursorLocked(false);
    }


    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        KeepAboveFloor();
        RespawnIfFallen();
    }


    void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !locked;
    }


    // =========================================================
    // MOUSE LOOK
    // =========================================================

    void HandleMouseLook()
    {
        if (Mouse.current == null)
            return;

        // Read mouse movement.
        //
        // IMPORTANT:
        // Mouse delta is already movement for this frame,
        // so DO NOT multiply it by Time.deltaTime.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();


        // Horizontal rotation.
        transform.Rotate(
            Vector3.up * (mouseDelta.x * lookSensitivity)
        );


        // Vertical rotation.
        verticalRotation -= mouseDelta.y * lookSensitivity;


        // Prevent the player from looking completely upside down.
        verticalRotation = Mathf.Clamp(
            verticalRotation,
            -89f,
            89f
        );


        // Apply vertical rotation to the camera/head only.
        if (playerCameraRoot != null)
        {
            playerCameraRoot.localRotation =
                Quaternion.Euler(
                    verticalRotation,
                    0f,
                    0f
                );
        }
    }


    // =========================================================
    // MOVEMENT
    // =========================================================

    void HandleMovement()
    {
        if (Keyboard.current == null)
            return;


        Vector3 wanted =
            ReadWantedDirection() * CurrentSpeed();


        // Smoothly accelerate/decelerate.
        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            wanted,
            acceleration * Time.deltaTime
        );


        // Gravity.
        //
        // A small constant downward force while grounded
        // keeps CharacterController.isGrounded reliable.
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }


        verticalVelocity += gravity * Time.deltaTime;


        // ONE Move call per frame.
        //
        // This prevents isGrounded from flickering.
        Vector3 total =
            horizontalVelocity +
            Vector3.up * verticalVelocity;


        controller.Move(
            total * Time.deltaTime
        );
    }


    // =========================================================
    // FLOOR PROTECTION
    // =========================================================

    void KeepAboveFloor()
    {
        // Wait until we first touch the floor.
        if (!hasLanded)
        {
            if (!controller.isGrounded)
                return;

            hasLanded = true;

            floorY = transform.position.y;

            return;
        }


        // Prevent the player from sinking below
        // the original floor level.
        if (transform.position.y < floorY - 0.02f)
        {
            controller.Move(
                Vector3.up *
                (floorY - transform.position.y)
            );

            verticalVelocity = 0f;
        }
    }


    // =========================================================
    // MOVEMENT INPUT
    // =========================================================

    Vector3 ReadWantedDirection()
    {
        float moveX = 0f;
        float moveZ = 0f;


        if (Keyboard.current.aKey.isPressed)
            moveX -= 1f;


        if (Keyboard.current.dKey.isPressed)
            moveX += 1f;


        if (Keyboard.current.sKey.isPressed)
            moveZ -= 1f;


        if (Keyboard.current.wKey.isPressed)
            moveZ += 1f;


        Vector3 move =
            transform.right * moveX +
            transform.forward * moveZ;


        return move.normalized;
    }


    // =========================================================
    // WALK / RUN SPEED
    // =========================================================

    float CurrentSpeed()
    {
        bool running =
            Keyboard.current.leftShiftKey.isPressed;


        float speed =
            running
                ? runSpeed
                : walkSpeed;


        // The ghost slows down as the chapters pass.
        return speed * AbilityLoss.SpeedMultiplier;
    }


    // =========================================================
    // FALL / RESPAWN
    // =========================================================

    void RespawnIfFallen()
    {
        if (transform.position.y > fallLimitY)
            return;


        // CharacterController must be disabled
        // before teleporting.
        controller.enabled = false;


        transform.SetPositionAndRotation(
            startPosition,
            startRotation
        );


        controller.enabled = true;


        // Reset movement.
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;


        // Need to find the floor again.
        hasLanded = false;
    }
}