using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 8.0f;
    public float jumpForce = 7.5f;

    [Header("UI References")]
    public TextMeshProUGUI countText;
    public GameObject winTextObject;

    private Rigidbody rb;
    private int count;
    private float movementX;
    private float movementY;
    private bool isGroundedCollision = false;

    void Awake()
    {
        EnsureEnvironmentColliders();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureEnvironmentColliders()
    {
        MeshFilter[] meshFilters = UnityEngine.Object.FindObjectsOfType<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.gameObject == null) continue;
            if (mf.GetComponent<Collider>() != null) continue; // Already has collider (e.g. Player, Pickup)
            if (mf.sharedMesh == null || mf.sharedMesh.vertexCount == 0) continue;

            string nameLower = mf.gameObject.name.ToLower();
            if (nameLower.Contains("ocean") || nameLower.Contains("water")) continue;

            MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        count = 0;

        SetCountText();
        if (winTextObject != null)
        {
            winTextObject.SetActive(false);
        }
    }

    void Update()
    {
        // Check if player fell off the platform into the ocean / abyss
        if (transform.position.y < -5f)
        {
            GameOver();
            return;
        }

        // Restart level with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
            return;
        }

        // Jump input detection: Space key on Keyboard (supports both Input Systems)
        bool jumpPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jumpPressed = true;
        }
#endif
        if (!jumpPressed && Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
        }

        if (jumpPressed)
        {
            Jump();
        }
    }

    void OnJump(InputValue value)
    {
        Jump();
    }

    public void Jump()
    {
        if (rb == null) return;

        if (IsGrounded())
        {
            isGroundedCollision = false;
            rb.velocity = new Vector3(rb.velocity.x, jumpForce, rb.velocity.z);
        }
    }

    public bool IsGrounded()
    {
        if (isGroundedCollision) return true;

        // Dynamic SphereCast fallback that scales with the ball's actual size
        SphereCollider sc = GetComponent<SphereCollider>();
        float scaleFactor = Mathf.Max(transform.lossyScale.x, Mathf.Max(transform.lossyScale.y, transform.lossyScale.z));
        float radius = (sc != null ? sc.radius : 0.5f) * scaleFactor;

        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius * 0.8f, Vector3.down, radius * 0.5f, ~0, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject && !hit.collider.isTrigger)
            {
                return true;
            }
        }
        return false;
    }

    void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();

        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    void SetCountText()
    {
        if (countText != null)
        {
            countText.text = "Count: " + count.ToString();
        }

        if (count >= 8)
        {
            if (winTextObject != null)
            {
                winTextObject.SetActive(true);
            }

            // Safe search for Enemy if tag exists
            try
            {
                GameObject enemy = GameObject.FindGameObjectWithTag("Enemy");
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            catch
            {
                // Tag not defined or no enemy found
            }
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        float moveX = movementX;
        float moveY = movementY;

        // Fallback to legacy Input if InputSystem yields zero
        if (moveX == 0f && moveY == 0f)
        {
            moveX = Input.GetAxisRaw("Horizontal");
            moveY = Input.GetAxisRaw("Vertical");
        }

        Vector3 movement = Vector3.zero;

        if (Camera.main != null)
        {
            Vector3 camForward = Camera.main.transform.forward;
            Vector3 camRight = Camera.main.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector2 input = new Vector2(moveX, moveY);
            float inputMagnitude = Mathf.Clamp01(input.magnitude);
            Vector3 moveDir = (camForward * moveY + camRight * moveX).normalized;
            movement = moveDir * inputMagnitude;
        }
        else
        {
            movement = new Vector3(moveX, 0.0f, moveY);
        }

        rb.AddForce(movement * speed);
    }

    private void GameOver()
    {
        if (winTextObject != null)
        {
            winTextObject.SetActive(true);
            TextMeshProUGUI tmp = winTextObject.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = "You Lose!";
                tmp.color = new Color(1f, 0.2f, 0.2f, 1f);
            }
        }
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckGrounded(collision);

        if (IsHazard(collision.gameObject))
        {
            GameOver();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckGrounded(collision);
    }

    private void OnCollisionExit(Collision collision)
    {
        isGroundedCollision = false;
    }

    private void CheckGrounded(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.4f)
            {
                isGroundedCollision = true;
                return;
            }
        }
    }

    public static bool IsHazard(GameObject go)
    {
        if (go == null) return false;

        if (CompareTagSafe(go, "Enemy") || CompareTagSafe(go, "Trap"))
        {
            return true;
        }

        Transform t = go.transform;
        while (t != null && t.name != "DemoScene")
        {
            string objName = t.name;
            string lower = objName.ToLower();

            // 1. SAFE OBJECTS: Boxes / Crates (Props_6, C_Props_6, DynamicBox) are physical obstacles, NOT deadly traps
            if (objName.StartsWith("Props_6") || objName.StartsWith("C_Props_6") ||
                lower.Contains("box") || lower.Contains("crate"))
            {
                return false;
            }

            // 2. SAFE STRUCTURES: Gates, archways, platforms, seesaw, ramps, scenery
            if (objName.StartsWith("Props_13") || objName.StartsWith("Props_14") || objName.StartsWith("Props_15") ||
                objName.StartsWith("Props_11") || objName.StartsWith("Props_12") || objName.StartsWith("Props_16"))
            {
                return false;
            }

            if (lower.StartsWith("ground") || lower.StartsWith("bridge") || lower.StartsWith("tree") ||
                lower.StartsWith("stone") || lower.StartsWith("tip_board") || lower.StartsWith("ocean") ||
                lower.StartsWith("clone"))
            {
                return false;
            }

            // 3. HAZARDS / TRAPS:
            // - Spiked balls ("berbentuk gerigi"): C_Props_8, Props_8
            // - Swinging spiked maces: C_Props_7, Props_7
            // - Rotating spiked bars: C_Props_9, Props_9
            // - Spinning saws / blades: Props_10
            // - Rolling spiked logs: Props_3
            // - Spiked ring / pit: Props_4
            // - Sharp spikes / crystals: Props_1, Props_5, Props_2 (spike floor), C_Props_1_2_3_5
            if (objName.StartsWith("C_Props_8") || objName.StartsWith("Props_8") ||
                objName.StartsWith("C_Props_7") || objName.StartsWith("Props_7") ||
                objName.StartsWith("C_Props_9") || objName.StartsWith("Props_9") ||
                objName.StartsWith("Props_10") || objName.StartsWith("C_Props_1_2_3_5") ||
                objName.StartsWith("Props_1") || objName.StartsWith("Props_2") ||
                objName.StartsWith("Props_3") || objName.StartsWith("Props_4") ||
                objName.StartsWith("Props_5"))
            {
                return true;
            }

            if (lower.Contains("trap") || lower.Contains("enemy") || lower.Contains("spike") ||
                lower.Contains("gerigi") || lower.Contains("saw") || lower.Contains("hazard"))
            {
                return true;
            }

            t = t.parent;
        }

        return false;
    }

    private static bool CompareTagSafe(GameObject go, string tag)
    {
        if (go == null) return false;
        try
        {
            return go.CompareTag(tag);
        }
        catch
        {
            return false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (CompareTagSafe(other.gameObject, "PickUp"))
        {
            other.gameObject.SetActive(false);
            count = count + 1;

            SetCountText();
        }
        else if (IsHazard(other.gameObject))
        {
            GameOver();
        }
    }
}