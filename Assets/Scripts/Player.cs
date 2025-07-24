using Items;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float sprintSpeed = 6f;
    public float flySpeed = 8f;
    public float jumpForce = 5f;
    public bool creativeMode = false;

    [Header("Sprint Settings")]
    public bool sprintToggleMode = true; // true = toggle ctrl, false = hold ctrl

    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float pitchMin = -90f;
    public float pitchMax = 90f;

    [Header("Block Interaction")]
    public float interactionDistance = 5f;
    public GameObject blockIndicatorPrefab; // Assign a simple cube prefab in the editor

    [Header("Inventory")]
    public List<Item> items = new();

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

    public Inventory inventory;

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

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.height = normalHeight;
        Cursor.lockState = CursorLockMode.Locked;

        // Initialize block indicator
        blockIndicator = Instantiate(blockIndicatorPrefab);
        blockIndicator.SetActive(false); // Hide initially
        for (int i = 0; i < 27; i++)
        {
            items.Add(Empty.Instance); // Fill inventory with empty items
        }
        GiveItem(new DebugBlock(50));
        GiveItem(new DebugItem());
        GiveItem(new Dirt(10));
        GiveItem(new Bucket() { Value = 5 });
    }

    private void Update()
    {
        HandleMouseLook();
        if (InventoryManager.Instance.openInventories[1] || InventoryManager.Instance.openInventories[2])
        {
            return;
        }

        HandleActions();
        HandleMovement();
        HandleMouse();
    }

    private void HandleMouseLook()
    {
        if (InventoryManager.Instance.openInventories[1] || InventoryManager.Instance.openInventories[2])
        {
            Cursor.lockState = CursorLockMode.None;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void HandleActions()
    {
        // double-tap W for sprint
        if (Input.GetKeyDown(KeyCode.W))
        {
            if (Time.time - lastWPressTime < doubleTapThreshold)
            {
                isDoubleTapSprint = true;
            }

            lastWPressTime = Time.time;
        }

        if (Input.GetKeyUp(KeyCode.W))
        {
            isDoubleTapSprint = false; // double-tap sprint stops on releasing W
        }

        // ctrl sprint toggle or hold
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (sprintToggleMode)
            {
                isSprintToggled = !isSprintToggled;
            }
        }
        if (!sprintToggleMode)
        {
            isSprintToggled = Input.GetKey(KeyCode.LeftControl);
        }

        isSprinting = isDoubleTapSprint || isSprintToggled;

        // fly toggle
        if (creativeMode && Input.GetKeyDown(KeyCode.Space))
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

    private void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        Vector3 input;
        input = new(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));

        // normalize input for consistent diagonal movement
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 move = (transform.right * input.x) + (transform.forward * input.z);

        if (isFlying)
        {
            Vector3 flyMove = move * flySpeed;

            if (Input.GetKey(KeyCode.Space))
            {
                flyMove += Vector3.up * flySpeed;
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                flyMove += Vector3.down * flySpeed;
            }

            _ = controller.Move(flyMove * Time.deltaTime);
        }
        else
        {
            float speed = isSprinting ? sprintSpeed : walkSpeed;
            _ = controller.Move(speed * Time.deltaTime * move);

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            if (Input.GetKey(KeyCode.Space) && isGrounded)
            {
                velocity.y = jumpForce;
            }

            velocity.y += Physics.gravity.y * Time.deltaTime;
            _ = controller.Move(velocity * Time.deltaTime);
        }
    }
    // i should give these more descriptive names
    private void HandleMouse()
    {
        bool rightClicked = Input.GetMouseButtonDown(1);
        Ray ray = new(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            // Compute target world block position (for breaking)
            Vector3Int worldBlockPos = Vector3Int.RoundToInt(hit.point - hit.normal / 2);
            Vector3Int chunkPos = Chunk.GetChunkPosition(worldBlockPos);
            Vector3Int localBlockPos = Chunk.GetLocalPosition(worldBlockPos);

            if (ChunkManager.Instance.ActiveChunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                // Show block indicator at exact position
                blockIndicator.transform.position = worldBlockPos;
                blockIndicator.SetActive(true);

                if (Input.GetMouseButtonDown(0)) // Left click to break
                {
                    if (creativeMode)
                        chunk.RemoveBlock(localBlockPos);
                    else
                        chunk.RemoveBlock(localBlockPos); // TODO: implement survival breaking logic
                }
                else if (rightClicked && InventoryManager.Instance.HeldItem is IPlaceBlock placeBlock)
                {
                    Vector3Int placeWorldPos = Vector3Int.FloorToInt(worldBlockPos + hit.normal);
                    Vector3Int placeChunkPos = Chunk.GetChunkPosition(placeWorldPos);
                    Vector3Int placeLocalPos = Chunk.GetLocalPosition(placeWorldPos);

                    if (ChunkManager.Instance.ActiveChunks.TryGetValue(placeChunkPos, out Chunk neighborChunk))
                    {
                        neighborChunk.PlaceBlock(placeLocalPos, placeBlock.Block);
                        InventoryManager.Instance.HeldItem.count--;
                    }
                }
                else if (Input.GetMouseButtonDown(2))
                {
                    GiveItem(System.Activator.CreateInstance(Block.BlockRegistry.blockIdToItem[chunk.Blocks[localBlockPos.x, localBlockPos.y, localBlockPos.z].ID]) as Item);
                    // gosh this is long
                }
            }
            else
            {
                blockIndicator.SetActive(false);
            }
        }
        else
        {
            blockIndicator.SetActive(false);
        }
    }

    public void GiveItem(Item item)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == Empty.Instance)
            {
                items[i] = item; // Assign the new item to the first empty slot
                return;
            }
            if (items[i].GetType() == item.GetType() && items[i].count < items[i].MaxCount)
            {
                int spaceLeft = items[i].MaxCount - items[i].count;
                if (spaceLeft < 0)
                {

                }
                return;
            }
        }
    }
}