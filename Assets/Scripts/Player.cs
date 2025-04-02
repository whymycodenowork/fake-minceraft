using Items;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Rigidbody myRigidbody;
    public Transform mainCamera;
    public PauseManager pauseManager;
    public InventoryManager inventoryManager;
    public float baseMovementSpeed;
    public float movementSpeed;
    public float jumpHeight;
    private bool _isGrounded;
    private Vector3 _currentVelocity = Vector3.zero;
    private const int CHUNK_SIZE = 16;
    public GameObject indicatorBox;
    public LayerMask groundLayer;
    public float selectDistance = 5;
    public ChunkPool chunkPool;
    public bool canFly;
    private Voxel _selectedBlock = new(6, 1);

    // Start is called before the first frame update
    private void Start()
    {
        // Disable the box at start
        indicatorBox.SetActive(false);
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        var targetVelocity = Vector3.SmoothDamp(myRigidbody.linearVelocity, Vector3.zero, ref _currentVelocity, 0.2f);
        myRigidbody.linearVelocity = new(targetVelocity.x, myRigidbody.linearVelocity.y, targetVelocity.z);
        if (pauseManager.isPaused || inventoryManager.inventoryOpen)
            return; // Disable movement and other interactions if a menu is open
        MovePlayer();

        if (Input.GetKeyDown(KeyCode.R))
        {
            myRigidbody.linearVelocity = Vector3.zero;
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
    }
    
    private void Update()
    {
        var selectedItem = inventoryManager.HotbarItems[(int)Mathf.Round(inventoryManager.hotbarSlot)];
        _selectedBlock = selectedItem switch
        {
            Nothing => new(0),
            BlockItem blockItem => blockItem.BlockToPlace,
            _ => _selectedBlock
        };
        DetectInteractions();
    }

    private void MovePlayer()
    {
        var movementDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            movementDirection += mainCamera.transform.forward;
        }

        if (Input.GetKey(KeyCode.S))
        {
            movementDirection -= mainCamera.transform.forward;
        }

        if (Input.GetKey(KeyCode.A))
        {
            movementDirection -= mainCamera.transform.right;
        }

        if (Input.GetKey(KeyCode.D))
        {
            movementDirection += mainCamera.transform.right;
        }

        if (movementDirection == Vector3.zero)
        {
            return;
        }

        movementDirection.y = 0; // Make movement horizontal
        movementDirection.Normalize(); // No square root of 2 times extra speed when holding 2 keys 

        myRigidbody.linearVelocity += movementDirection * movementSpeed;
    }

    private void HandleJump()
    {
        _isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.01f, groundLayer);
        if (!_isGrounded) return;
        var velocity = myRigidbody.linearVelocity;
        myRigidbody.linearVelocity = new(velocity.x, jumpHeight, velocity.z);
    }

    private void HandleFly(string direction)
    {
        switch (direction)
        {
            case "up":
                myRigidbody.linearVelocity += Vector3.up * jumpHeight;
                break;
            case "down":
                myRigidbody.linearVelocity += Vector3.down * jumpHeight;
                break;
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private void DetectInteractions()
    {
        Ray ray = new(mainCamera.transform.position, mainCamera.forward);
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

    private void Interact(int mouseButton, Vector3Int position)
    {
        var chunkCoords = GetChunkPosition(position, out var localPosition);

        // Ensure the position is within chunk bounds before interacting
        if (!chunkPool.ActiveChunks.TryGetValue(chunkCoords, out var hitChunk)) return;
        var chunkScript = hitChunk.GetComponent<Chunk>();
        var chunkVoxels = chunkScript.Voxels;

        switch (mouseButton)
        {
            case 0: // Left-click: Remove voxel

                var voxel = chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z];
                if (voxel.Type == 0) break;
                // Debug.Log($"Deleted {voxel}");
                chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z].Type = 0;
                chunkScript.isDirty = true;
                chunkScript.UpdateNeighborMeshes((int)localPosition.x, (int)localPosition.z);
                break;
            case 1: // Right-click: Place voxel
                chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z] = _selectedBlock;
                chunkScript.isDirty = true;
                chunkScript.UpdateNeighborMeshes((int)localPosition.x, (int)localPosition.z);
                break;
            case 2:
                _selectedBlock = chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z];
                break;
        }
    }

    private static Vector2Int GetChunkPosition(Vector3 location, out Vector3 localPosition)
    {
        // Calculate chunk coordinates
        var chunkX = Mathf.FloorToInt(location.x / CHUNK_SIZE);
        var chunkZ = Mathf.FloorToInt(location.z / CHUNK_SIZE);

        // Calculate local position within the chunk
        var localX = location.x % CHUNK_SIZE;
        var localZ = location.z % CHUNK_SIZE;

        // Ensure the local position is positive
        if (localX < 0) localX += CHUNK_SIZE;
        if (localZ < 0) localZ += CHUNK_SIZE;

        localPosition = new(localX, location.y, localZ);
        return new(chunkX, chunkZ);
    }
}
