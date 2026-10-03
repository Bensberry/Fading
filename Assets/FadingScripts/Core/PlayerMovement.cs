using UnityEngine;
using UnityEngine.InputSystem;

// First-person walking for the ghost. Put this on the Main Camera.
// WASD = move, Left Shift = faster, hold Right Mouse = look around.
// No gravity: the player always stays at eye height.
// Walls only block the player if they have colliders (add Mesh Colliders later and it just works).
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 2.5f;
    public float runSpeed = 4.5f;
    public float mouseSensitivity = 0.15f;
    public float eyeHeight = 1.6f;

    CharacterController controller;
    float yaw, pitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.height = 1.4f;                          // body hangs below the eyes
        controller.radius = 0.25f;
        controller.center = new Vector3(0f, -0.7f, 0f);

        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x > 180f ? e.x - 360f : e.x;
        SnapToEyeHeight();
    }

    void Update()
    {
        Look();
        Move();
    }

    void Look()
    {
        Mouse mouse = Mouse.current;
        bool looking = mouse != null && mouse.rightButton.isPressed;
        Cursor.lockState = looking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !looking;
        if (!looking) return;

        Vector2 delta = mouse.delta.ReadValue();
        yaw += delta.x * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * mouseSensitivity, -85f, 85f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void Move()
    {
        Keyboard k = Keyboard.current;
        if (k == null) return;

        Vector2 input = Vector2.zero;
        if (k.wKey.isPressed) input.y += 1f;
        if (k.sKey.isPressed) input.y -= 1f;
        if (k.dKey.isPressed) input.x += 1f;
        if (k.aKey.isPressed) input.x -= 1f;

        // Move along the floor only, even when looking up or down.
        Quaternion flat = Quaternion.Euler(0f, yaw, 0f);
        Vector3 dir = flat * Vector3.forward * input.y + flat * Vector3.right * input.x;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        float speed = k.leftShiftKey.isPressed ? runSpeed : walkSpeed;
        controller.Move(dir * speed * Time.deltaTime);
        SnapToEyeHeight();
    }

    void SnapToEyeHeight()
    {
        Vector3 p = transform.position;
        if (Mathf.Abs(p.y - eyeHeight) < 0.001f) return;
        controller.enabled = false;                        // CharacterController overrides direct moves
        p.y = eyeHeight;
        transform.position = p;
        controller.enabled = true;
    }
}
