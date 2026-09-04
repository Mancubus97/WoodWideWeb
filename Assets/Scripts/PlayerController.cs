using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    private Rigidbody rb;
    private Camera cam;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Called automatically by Player Input component
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    void Update()
    {
        // Mouse look
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX); 

        // Use the new Input System keyboard checks instead of the legacy Input API
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Time.timeScale = Mathf.Max(0.01f, Time.timeScale - Time.timeScale / 10f);
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Time.timeScale = Time.timeScale + Time.timeScale / 10f;
            }
        }
    }

    void FixedUpdate()
    {
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;
        move.y = rb.linearVelocity.y;
        rb.linearVelocity = move;
    }
}