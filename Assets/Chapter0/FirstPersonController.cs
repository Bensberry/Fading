using UnityEngine;
using UnityEngine.InputSystem;

// First-person walking for the player (ghost). Goes on the FPP prefab root (already attached).
// WASD = move, Left Shift = run, HOLD the LEFT MOUSE BUTTON and move the mouse to look around.
// (The cursor is only hidden while you hold the button, so you can click normally otherwise.)
// Gravity is on, but the player can never sink below the floor level they started on,
// and if they ever fall out of the house they are put back at their starting point.
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
    [Tooltip("Degrees turned per mouse count, like an FPS game. 0.1 = typical FPS default, 0.05 = slower, 0.2 = fast.")]
    public float lookSensitivity = 0.1f;
    public Transform playerCameraRoot; // Drag your 'Head' object here

    [Header("Safety Net")]
    [Tooltip("If the player falls below this height, they are teleported back to where they started.")]
    public float fallLimitY = -5f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float verticalRotation = 0f;
    private bool lookActive;
    private bool skipLookFrame;
    private bool hasLanded;
    private float floorY;
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.stepOffset = stepOffset;
        controller.skinWidth = skinWidth;
        startPosition = transform.position;
        startRotation = transform.rotation;
        SetCursorLocked(false);
    }

    void OnDisable()
    {
        lookActive = false;
        SetCursorLocked(false);       // e.g. during the cutscene
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
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null) return;

        // Look only while the left mouse button is held down.
        bool holding = Mouse.current.leftButton.isPressed;
        if (holding != lookActive)
        {
            lookActive = holding;
            skipLookFrame = holding;           // ignore the first frame: locking the cursor causes a jump
            SetCursorLocked(holding);
        }
        if (!lookActive) return;
        if (skipLookFrame) { skipLookFrame = false; return; }

        // The mouse delta is already "movement this frame", so it must NOT be multiplied by deltaTime.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        transform.Rotate(Vector3.up * (mouseDelta.x * lookSensitivity));

        verticalRotation -= mouseDelta.y * lookSensitivity;
        verticalRotation = Mathf.Clamp(verticalRotation, -89f, 89f);

        if (playerCameraRoot != null)
            playerCameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;

        Vector3 wanted = ReadWantedDirection() * CurrentSpeed();

        // Smoothly speed up / slow down instead of jumping straight to full speed.
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wanted, acceleration * Time.deltaTime);

        // Gravity: a small constant push while grounded keeps isGrounded reliable.
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        // ONE Move call per frame (two separate calls made isGrounded flicker).
        Vector3 total = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(total * Time.deltaTime);
    }

    // The first time we touch the floor we remember its height. After that the player can't sink below it
    // (so a gap in the floor can't swallow them), but gravity still brings them down off anything they stepped on.
    void KeepAboveFloor()
    {
        if (!hasLanded)
        {
            if (!controller.isGrounded) return;
            hasLanded = true;
            floorY = transform.position.y;
            return;
        }
        if (transform.position.y < floorY - 0.02f)
        {
            controller.Move(Vector3.up * (floorY - transform.position.y));
            verticalVelocity = 0f;
        }
    }

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

    float CurrentSpeed()
    {
        bool running = Keyboard.current.leftShiftKey.isPressed;
        return running ? runSpeed : walkSpeed;
    }

    void RespawnIfFallen()
    {
        if (transform.position.y > fallLimitY) return;

        // A CharacterController must be switched off while we teleport it.
        controller.enabled = false;
        transform.SetPositionAndRotation(startPosition, startRotation);
        controller.enabled = true;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = 0f;
        hasLanded = false;
    }
}
