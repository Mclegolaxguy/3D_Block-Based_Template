using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5.0f;
    public float gravity = -20.0f;
    public float jumpHeight = 1.2f;

    [Header("Mouse Look")]
    public Transform playerCamera;
    public float mouseSensitivity = 2.0f;
    public float maxLookAngle = 90.0f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float rotationX = 0.0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        // Ensure player has the correct tag for WorldManager auto-detection
        if (!CompareTag("Player"))
        {
            tag = "Player";
        }

        // Lock and hide the cursor for first-person view
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Automatically find the camera if not assigned
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void Update()
    {
        // 1. Ground check
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Mouse Look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -maxLookAngle, maxLookAngle);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        }
        transform.Rotate(Vector3.up * mouseX);

        // 3. Movement
        float moveX = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        float moveZ = Input.GetAxis("Vertical");   // W/S or Up/Down arrows

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * walkSpeed * Time.deltaTime);

        // 4. Jumping
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // 5. Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}