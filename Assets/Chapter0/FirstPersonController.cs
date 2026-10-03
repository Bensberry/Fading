using UnityEngine;
using UnityEngine.InputSystem;

// First-person walking for the player (ghost). Goes on the FPP prefab root (already attached).
// WASD = move, Left Shift = run, mouse = look, Escape = free / lock the cursor.
// If the player ever falls out of the house, they are put back at their starting point.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 4.5f;
    [Tooltip("Higher = reaches full speed / stops faster.")]
    public float acceleration = 12f;
    public float gravity = -9.81f;
    [Tooltip("Ghost: after landing on the floor once, the player stays at that height and can never fall again.")]
    public bool ghostMode = true;

    [Header("Look Settings")]
    [Tooltip("Lower this value significantly (e.g., between 0.05 and 0.5)")]
    public float mouseSensitivity = 0.5f;
    public Transform playerCameraRoot; // Drag your 'Head' object here

    [Header("Safety Net")]
    [Tooltip("If the player falls below this height, they are teleported back to where they started.")]
    public float fallLimitY = -5f;

    private CharacterController controller;
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private bool hasLanded;
    private float verticalRotation = 0f;
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        LockCursor(true);
    }

    void Update()
    {
        HandleCursorToggle();
        if (Cursor.lockState == CursorLockMode.Locked) HandleMouseLook();
        HandleMovement();
        RespawnIfFallen();
    }

    void HandleCursorToggle()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            LockCursor(Cursor.lockState != CursorLockMode.Locked);
    }

    void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null) return;

        // The mouse delta is already "movement this frame", so it must NOT be multiplied by deltaTime.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        if (playerCameraRoot != null)
            playerCameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;

        Vector3 wanted = ReadWantedDirection() * CurrentSpeed();

        // Smoothly speed up / slow down instead of jumping straight to full speed.
        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, wanted, acceleration * Time.deltaTime);

        UpdateVerticalVelocity();

        // ONE Move call per frame (two separate calls made isGrounded flicker).
        Vector3 total = horizontalVelocity + Vector3.up * verticalVelocity;
        controller.Move(total * Time.deltaTime);
    }

    void UpdateVerticalVelocity()
    {
        if (controller.isGrounded) hasLanded = true;

        // Ghost: once we have settled on the floor, stay at this height for good.
        if (ghostMode && hasLanded) { verticalVelocity = 0f; return; }

        // Gravity: a small constant push while grounded keeps isGrounded reliable.
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;
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
