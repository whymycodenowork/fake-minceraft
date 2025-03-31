using Items;
using UnityEngine;
public class Player : MonoBehaviour
{
    public Rigidbody MyRigidbody;
    public Transform MainCamera;
    public PauseManager pauseManager;
    public InventoryManager inventoryManager;
    public float baseMovementSpeed;
    public float movementSpeed;
    public float jumpHeight;
    private bool isGrounded;
    private Vector3 currentVelocity = Vector3.zero;
    private const int chunkSize = 16;
    public GameObject indicatorBox;
    public LayerMask groundLayer;
    public float selectDistance = 5;
    public ChunkPool chunkPool;
    public bool canFly;
    public Voxel selectedBlock = new(6, 1);

    // Start is called before the first frame update
    void Start()
    {
        // Disable the box at start
        indicatorBox.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        var targetVelocity = Vector3.SmoothDamp(MyRigidbody.linearVelocity, Vector3.zero, ref currentVelocity, 0.2f);
        MyRigidbody.linearVelocity = new Vector3(targetVelocity.x, MyRigidbody.linearVelocity.y, targetVelocity.z);
        if (pauseManager.isPaused || inventoryManager.inventoryOpen) return; // Disable movement and other interactions if a menu is open
        var selectedItem = inventoryManager.hotbarItems[(int)Mathf.Round(inventoryManager.hotbarSlot)];
        if (selectedItem is Nothing) selectedBlock = new Voxel(0, 0);
        if (selectedItem is BlockItem blockItem)
        {
            selectedBlock = blockItem.BlockToPlace;
        }
        MovePlayer();
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            MyRigidbody.linearVelocity = Vector3.zero;
            transform.position = Vector3.zero;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            if (canFly) HandleFly("up");
            else HandleJump();
        }

        if (Input.GetKey(KeyCode.LeftShift) && canFly)
        {
            HandleFly("down");
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            movementSpeed = baseMovementSpeed * 2;
        }
        else
        {
            movementSpeed = baseMovementSpeed;
        }
        /*
        Vector2Int currentChunkPos = GetChunkPosition(transform.position, out Vector3 n);
        if (!chunkPool.activeChunks.ContainsKey(currentChunkPos))
        { 
            MyRigidbody.velocity = Vector3.zero;
            Debug.LogWarning("out of bounds!");
        }
        else if (chunkPool.activeChunks[new Vector2Int(currentChunkPos.x, currentChunkPos.y)].GetComponent<Chunk>().HasMesh)
        {
            MyRigidbody.velocity = Vector3.zero;
            Debug.LogWarning("out of bounds!");
        }
        */
        DetectInteractions();
    }

    void MovePlayer()
    {
        var movementDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            movementDirection += MainCamera.transform.forward;
        }

        if (Input.GetKey(KeyCode.S))
        {
            movementDirection -= MainCamera.transform.forward;
        }

        if (Input.GetKey(KeyCode.A))
        {
            movementDirection -= MainCamera.transform.right;
        }

        if (Input.GetKey(KeyCode.D))
        {
            movementDirection += MainCamera.transform.right;
        }

        if (movementDirection == Vector3.zero)
        {
            return;
        }

        movementDirection.y = 0; // Make movement horizontal
        movementDirection.Normalize(); // No square root of 2 times extra speed when holding 2 keys 

        MyRigidbody.linearVelocity += movementDirection * movementSpeed;
    }

    void HandleJump()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.01f, groundLayer);
        if (isGrounded)
        {
            var velocity = MyRigidbody.linearVelocity;
            MyRigidbody.linearVelocity = new Vector3(velocity.x, jumpHeight, velocity.z);
        }
    }

    void HandleFly(string direction)
    {
        if (direction == "up")
        {
            MyRigidbody.linearVelocity += Vector3.up * jumpHeight;
        }
        else if (direction == "down")
        {
            MyRigidbody.linearVelocity += Vector3.down * jumpHeight;
        }
    }

    void DetectInteractions()
    {
        Ray ray = new(MainCamera.transform.position, MainCamera.forward);
        if (Physics.Raycast(ray, out var hit, selectDistance, groundLayer))
        {
            // Get the hit location and adjust based on hit normal to avoid incorrect rounding
            Vector3 hitPointAdjusted;
            if (Input.GetMouseButton(1))
            {
                hitPointAdjusted = hit.point + hit.normal * 0.5f;
            }
            else
            {
                hitPointAdjusted = hit.point + -hit.normal * 0.5f;
            }

            // Round the hit point adjusted to the nearest integer
            Vector3Int roundedHitPoint = new(
                Mathf.RoundToInt(hitPointAdjusted.x),
                Mathf.RoundToInt(hitPointAdjusted.y),
                Mathf.RoundToInt(hitPointAdjusted.z)
            );

            // Move the indicator box to the adjusted hit location
            indicatorBox.transform.position = roundedHitPoint;

            // Enable the box if it was disabled
            if (!indicatorBox.activeSelf)
            {
                indicatorBox.SetActive(true);
            }

            if (Input.GetMouseButton(0))
                Interact(0, roundedHitPoint);
            else if (Input.GetMouseButton(1))
                Interact(1, roundedHitPoint);
            else if (Input.GetMouseButton(2))
                Interact(2, roundedHitPoint);
        }
        else
        {
            // Disable the indicator box if nothing is hit
            if (indicatorBox.activeSelf)
            {
                indicatorBox.SetActive(false);
            }
        }
    }

    void Interact(int mouseButton, Vector3Int position)
    {
        var chunkCoords = GetChunkPosition(position, out var localPosition);

        // Ensure the position is within chunk bounds before interacting
        if (chunkPool.activeChunks.TryGetValue(chunkCoords, out var hitChunk))
        {
            var chunkScript = hitChunk.GetComponent<Chunk>();
            var chunkVoxels = chunkScript.voxels;

            switch (mouseButton)
            {
                case 0: // Left-click: Remove voxel

                    var voxel = chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z];
                    if (voxel.type == 0) break;
                    // Debug.Log($"Deleted {voxel}");
                    chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z].type = 0;
                    chunkScript.UpdateMeshLocal((int)localPosition.x, (int)localPosition.z);
                    break;
                case 1: // Right-click: Place voxel
                    chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z] = selectedBlock;
                    chunkScript.UpdateMeshLocal((int)localPosition.x, (int)localPosition.z);
                    break;
                case 2:
                    selectedBlock = chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z];
                    break;
                default:
                    break;
            }
        }
    }

    Vector2Int GetChunkPosition(Vector3 location, out Vector3 localPosition)
    {
        // Calculate chunk coordinates
        var chunkX = Mathf.FloorToInt(location.x / chunkSize);
        var chunkZ = Mathf.FloorToInt(location.z / chunkSize);

        // Calculate local position within the chunk
        var localX = location.x % chunkSize;
        var localZ = location.z % chunkSize;

        // Ensure the local position is positive
        if (localX < 0) localX += chunkSize;
        if (localZ < 0) localZ += chunkSize;

        localPosition = new Vector3(localX, location.y, localZ);
        return new Vector2Int(chunkX, chunkZ);
    }
}
