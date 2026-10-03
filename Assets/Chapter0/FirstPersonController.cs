using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Look Settings")]
    [Tooltip("Lower this value significantly (e.g., between 0.05 and 0.5)")]
    public float mouseSensitivity = 0.5f;
    public Transform playerCameraRoot; // Drag your 'Head' object here

    private CharacterController controller;
    private Vector3 velocity;
    private float verticalRotation = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Lock the cursor to the center of the screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        if (Mouse.current == null) return;

        // Multiply by Time.deltaTime to frame-rate normalize and scale raw pixel delta
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime * 60f;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime * 60f;

        // Rotate the player body left and right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate the camera head up and down (with clamping to prevent flipping)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        if (playerCameraRoot != null)
        {
            playerCameraRoot.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    void HandleMovement()
    {
        if (Keyboard.current == null) return;

        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current.aKey.isPressed) moveX -= 1f;
        if (Keyboard.current.dKey.isPressed) moveX += 1f;
        if (Keyboard.current.sKey.isPressed) moveZ -= 1f;
        if (Keyboard.current.wKey.isPressed) moveZ += 1f;

        // Calculate movement direction relative to where the player is facing
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move.normalized * moveSpeed * Time.deltaTime);

        // Basic gravity handling
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}