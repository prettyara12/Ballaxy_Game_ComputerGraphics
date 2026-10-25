using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Reference to the player GameObject to follow.")]
    public GameObject player;

    [Header("Follow Distance & Height")]
    [Tooltip("Distance behind the player.")]
    public float distance = 4.5f;

    [Tooltip("Height above the player.")]
    public float height = 2.0f;

    [Tooltip("Offset point to look at above player's center.")]
    public float lookOffset = 0.3f;

    [Tooltip("Initial yaw angle in degrees (90 = facing +X down the starting track).")]
    public float initialYaw = 90f;

    [Header("Camera Smoothing")]
    [Tooltip("Time in seconds to smoothly rotate behind the ball direction.")]
    public float rotationSmoothTime = 0.25f;

    [Tooltip("Time in seconds to smoothly follow the ball position (absorbs bumps/jitter).")]
    public float positionSmoothTime = 0.05f;

    [Header("Pitch Settings")]
    [Tooltip("Downward viewing angle in degrees.")]
    public float defaultPitch = 15f;
    public float minPitch = 5f;
    public float maxPitch = 60f;

    [Header("Mouse Orbit Settings")]
    [Tooltip("Mouse sensitivity when dragging to rotate the camera.")]
    public float mouseSensitivity = 3.0f;

    private Rigidbody playerRb;
    private float currentYaw = 90f;
    private float currentPitch = 15f;
    private float yawVelocity;
    private Vector3 positionVelocity;
    private Vector3 currentPosition;
    private float manualControlTimer = 0f;

    void Awake()
    {
        // Ensure no Rigidbody component on the camera creates physics jitter
        Rigidbody camRb = GetComponent<Rigidbody>();
        if (camRb != null)
        {
            Destroy(camRb);
        }
    }

    void Start()
    {
        currentYaw = initialYaw;
        currentPitch = defaultPitch;

        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
            SnapToTarget();
        }
    }

    public void SnapToTarget()
    {
        if (player == null) return;

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        currentPosition = player.transform.position - (rotation * Vector3.forward * distance) + (Vector3.up * height);
        transform.position = currentPosition;

        Vector3 lookTarget = player.transform.position + Vector3.up * lookOffset;
        transform.LookAt(lookTarget);
    }

    void LateUpdate()
    {
        // Restart level with R key (works even after player ball is destroyed)
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
            return;
        }

        if (player == null) return;

        if (playerRb == null)
        {
            playerRb = player.GetComponent<Rigidbody>();
        }

        // 1. Mouse Drag Control (Hold Left-Click or Right-Click to orbit/look around)
        bool isMouseDragging = Input.GetMouseButton(0) || Input.GetMouseButton(1);
        if (isMouseDragging)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            if (Mathf.Abs(mouseX) > 0.001f || Mathf.Abs(mouseY) > 0.001f)
            {
                currentYaw += mouseX * mouseSensitivity;
                currentPitch -= mouseY * mouseSensitivity;
                currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
                manualControlTimer = 1.0f; // Pause auto-align for 1s after mouse drag
            }
        }

        if (manualControlTimer > 0f)
        {
            manualControlTimer -= Time.deltaTime;
        }

        // 2. Smooth Auto-Align Behind Ball Velocity
        // When moving without active mouse drag, smoothly sweep behind the ball
        if (manualControlTimer <= 0f && playerRb != null)
        {
            Vector3 flatVel = new Vector3(playerRb.velocity.x, 0, playerRb.velocity.z);
            if (flatVel.magnitude > 0.25f)
            {
                Vector3 camForward = Quaternion.Euler(0, currentYaw, 0) * Vector3.forward;
                float dot = Vector3.Dot(camForward, flatVel.normalized);

                // Follow turning/forward movements (avoids 180 snap when reversing)
                if (dot > -0.3f)
                {
                    float targetYaw = Mathf.Atan2(flatVel.x, flatVel.z) * Mathf.Rad2Deg;
                    currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, rotationSmoothTime);
                }
            }
        }

        // 3. Critically Damped Position & Orientation Following
        // Eliminates jitter, terrain bumps, and micro-stutters
        Quaternion currentRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 targetPosition = player.transform.position - (currentRotation * Vector3.forward * distance) + (Vector3.up * height);

        if (currentPosition == Vector3.zero)
        {
            currentPosition = targetPosition;
        }
        currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition, ref positionVelocity, positionSmoothTime);
        transform.position = currentPosition;

        // Smooth look target
        Vector3 lookTarget = player.transform.position + Vector3.up * lookOffset;
        transform.LookAt(lookTarget);
    }
}

