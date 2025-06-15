using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float sprintSpeed = 4f;
    public float flySpeed = 8f;
    public float jumpForce = 5f;
    public bool canFly = false;

    [Header("Sprint Settings")]
    public bool sprintToggleMode = true; // true = toggle ctrl, false = hold ctrl

    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Block Interaction")]
    public float interactionDistance = 5f;
    public int blockToPlace = 1; // Default block ID to place
    public GameObject blockIndicatorPrefab; // Assign a simple cube prefab in the editor

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isFlying = false;
    private bool isSprinting = false;
    private bool isSprintToggled = false;
    private bool isDoubleTapSprint = false;
    private bool isCrouching = false;

    private readonly float crouchHeight = 1.35f;
    private readonly float normalHeight = 1.8f;

    private float yaw = 0f;
    private float pitch = 0f;

    private float lastWPressTime = 0f;
    private float lastSpacePressTime = 0f;
    private readonly float doubleTapThreshold = 0.3f;

    private GameObject blockIndicator;

    public static Player Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.height = normalHeight;
        Cursor.lockState = CursorLockMode.Locked;

        // Initialize block indicator
        blockIndicator = Instantiate(blockIndicatorPrefab);
        blockIndicator.SetActive(false); // Hide initially
    }

    void Update()
    {
        HandleMouseLook();
        HandleActions();
        HandleMovement();
        HandleMouse();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void HandleActions()
    {
        // double-tap W for sprint
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (Time.time - lastWPressTime < doubleTapThreshold)
                isDoubleTapSprint = true;

            lastWPressTime = Time.time;
        }

        if (Input.GetKeyUp(KeyCode.W))
            isDoubleTapSprint = false; // double-tap sprint stops on releasing W

        // ctrl sprint toggle or hold
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (sprintToggleMode)
                isSprintToggled = !isSprintToggled;
        }
        if (!sprintToggleMode)
            isSprintToggled = Input.GetKey(KeyCode.LeftControl);

        isSprinting = isDoubleTapSprint || isSprintToggled;

        // fly toggle
        if (canFly && Input.GetKeyDown(KeyCode.Space))
        {
            if (Time.time - lastSpacePressTime < doubleTapThreshold)
            {
                isFlying = !isFlying;
                velocity = Vector3.zero;
                controller.height = normalHeight; // ensure proper collider
            }
            lastSpacePressTime = Time.time;
        }

        // crouching (only on ground, not flying)
        if (!isFlying)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (!isCrouching)
                {
                    controller.height = crouchHeight;
                    isCrouching = true;
                }
            }
            else
            {
                if (isCrouching)
                {
                    controller.height = normalHeight;
                    isCrouching = false;
                }
            }
        }
        else
        {
            controller.height = normalHeight;
            isCrouching = false;
        }
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        Vector3 input = new(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        // normalize input for consistent diagonal movement
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 move = transform.right * input.x + transform.forward * input.z;

        if (isFlying)
        {
            Vector3 flyMove = move * flySpeed;

            if (Input.GetKey(KeyCode.Space))
                flyMove += Vector3.up * flySpeed;
            if (Input.GetKey(KeyCode.LeftShift))
                flyMove += Vector3.down * flySpeed;

            controller.Move(flyMove * Time.deltaTime);
        }
        else
        {
            float speed = isSprinting ? sprintSpeed : walkSpeed;
            controller.Move(speed * Time.deltaTime * move);

            if (isGrounded && velocity.y < 0)
                velocity.y = -2f;

            if (Input.GetKey(KeyCode.Space) && isGrounded)
                velocity.y = jumpForce;

            velocity.y += Physics.gravity.y * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }
    }

    void HandleMouse()
    {
        Ray ray = new(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            if (hit.collider.TryGetComponent<Chunk>(out var chunk))
            {
                // Calculate the block position relative to the chunk
                Vector3 hitPosition = hit.point - chunk.transform.position;
                Vector3Int blockPos = Vector3Int.RoundToInt(hitPosition - (hit.normal / 2));

                // Check if the block position is within the chunk bounds
                if (blockPos.x >= 0 && blockPos.x < Chunk.CHUNK_SIZE &&
                    blockPos.y >= 0 && blockPos.y < Chunk.CHUNK_SIZE &&
                    blockPos.z >= 0 && blockPos.z < Chunk.CHUNK_SIZE)
                {
                    // Activate and position the block indicator
                    blockIndicator.SetActive(true);
                    blockIndicator.transform.position = chunk.transform.position + blockPos;

                    if (Input.GetMouseButtonDown(0)) // Left click to break block
                    {
                        // Break the block
                        chunk.Blocks[blockPos.x, blockPos.y, blockPos.z] = new Block();
                        chunk.isDirty = true;
                    }
                    else if (Input.GetMouseButtonDown(1)) // Right click to place block
                    {
                        Vector3Int placeBlockPos = Vector3Int.RoundToInt(blockPos + hit.normal);

                        // Check if the placement position is within the chunk bounds
                        if (placeBlockPos.x >= 0 && placeBlockPos.x < Chunk.CHUNK_SIZE &&
                            placeBlockPos.y >= 0 && placeBlockPos.y < Chunk.CHUNK_SIZE &&
                            placeBlockPos.z >= 0 && placeBlockPos.z < Chunk.CHUNK_SIZE)
                        {
                            // Place the block
                            chunk.Blocks[placeBlockPos.x, placeBlockPos.y, placeBlockPos.z] = new Block(blockToPlace);
                            chunk.isDirty = true;
                        }
                    }
                }
                else
                {
                    // If the block position is outside the chunk, hide the indicator
                    blockIndicator.SetActive(false);
                }
                return; // Exit to prevent deactivating the indicator when a valid block is hit
            }
        }

        // If no block is hit, hide the indicator
        blockIndicator.SetActive(false);
    }
}
