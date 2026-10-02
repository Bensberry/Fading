using UnityEngine;
using UnityEngine.InputSystem;

public class FreeFlyCamera : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 0.15f;

    private float pitch = 0f;

    void Update()
    {
        // WASD movement
        Vector3 movement = Vector3.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                movement += transform.forward;

            if (Keyboard.current.sKey.isPressed)
                movement -= transform.forward;

            if (Keyboard.current.dKey.isPressed)
                movement += transform.right;

            if (Keyboard.current.aKey.isPressed)
                movement -= transform.right;

            // Q = down, E = up
            if (Keyboard.current.eKey.isPressed)
                movement += Vector3.up;

            if (Keyboard.current.qKey.isPressed)
                movement -= Vector3.up;
        }

        transform.position +=
            movement.normalized * moveSpeed * Time.deltaTime;


        // Mouse look ONLY while holding right mouse button
        if (Mouse.current != null &&
            Mouse.current.rightButton.isPressed)
        {
            Vector2 mouse = Mouse.current.delta.ReadValue();

            transform.Rotate(
                Vector3.up * mouse.x * mouseSensitivity
            );

            pitch -= mouse.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -90f, 90f);

            transform.localRotation =
                Quaternion.Euler(
                    pitch,
                    transform.eulerAngles.y,
                    0f
                );
        }
    }
}