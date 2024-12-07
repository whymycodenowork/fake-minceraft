using Voxels;
using System.Threading.Tasks;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Rigidbody MyRigidbody;
    public Transform MainCamera;
    public float baseMovementSpeed;
    public float movementSpeed;
    public float jumpHeight;
    public LayerMask groundLayer;
    public GameObject boxPrefab;
    public bool canFly;
    public Voxel selectedBlock = new Voxels.Solid.Cobblestone();
    public float selectDistance = 5f;

    private bool isGrounded;
    private bool isFlying;
    private bool isWaiting = false;
    private Vector3 currentVelocity = Vector3.zero;
    private const int chunkSize = 16;
    private GameObject indicatorBox;

    private const int doublePressDelay = 400; // change later

    void Start()
    {
        InitializeIndicatorBox();
    }

    void Update()
    {
        HandleInput();
        MovePlayer();
        ApplyHorizontalDrag();
        HandleChunkBounds();
        DetectInteractions();
    }

    void InitializeIndicatorBox()
    {
        indicatorBox = Instantiate(boxPrefab);
        indicatorBox.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);

        Renderer boxRenderer = indicatorBox.GetComponent<Renderer>();
        if (boxRenderer != null)
        {
            boxRenderer.material.color = new Color(1f, 1f, 1f, 0.5f); // white
        }

        indicatorBox.SetActive(false);
    }

    // player inputs
    void HandleInput()
    {
        bool isPressingSpace = Input.GetKey(KeyCode.Space);
        if (Input.GetKeyDown(KeyCode.R)) ResetPlayer();
        if (Input.GetKeyDown(KeyCode.Space)) HandleSpacePress();
        if (isPressingSpace) HandleJumpOrFly();
        if (Input.GetKey(KeyCode.LeftShift) && isFlying) HandleFly("down");
        else if (isFlying && !isPressingSpace) MyRigidbody.velocity = new Vector3(MyRigidbody.velocity.x, 0, MyRigidbody.velocity.z);
        movementSpeed = Input.GetKey(KeyCode.LeftControl) ? baseMovementSpeed * 2 : baseMovementSpeed;
    }

    // Resets the player's position and velocity
    void ResetPlayer()
    {
        MyRigidbody.velocity = Vector3.zero;
        transform.position = new Vector3(0, 100, 0);
    }

    // Handles movement input for the player
    void MovePlayer()
    {
        Vector3 movementDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) movementDirection += MainCamera.transform.forward;
        if (Input.GetKey(KeyCode.S)) movementDirection -= MainCamera.transform.forward;
        if (Input.GetKey(KeyCode.A)) movementDirection -= MainCamera.transform.right;
        if (Input.GetKey(KeyCode.D)) movementDirection += MainCamera.transform.right;

        if (movementDirection == Vector3.zero) return;

        movementDirection.y = 0; // Ensure horizontal movement
        movementDirection.Normalize(); // Maintain consistent speed

        MyRigidbody.velocity += movementDirection * movementSpeed;
    }

    // Applies smooth deceleration to horizontal movement
    void ApplyHorizontalDrag()
    {
        Vector3 targetVelocity = Vector3.SmoothDamp(MyRigidbody.velocity, Vector3.zero, ref currentVelocity, 0.2f);
        MyRigidbody.velocity = new Vector3(targetVelocity.x, MyRigidbody.velocity.y, targetVelocity.z);
    }

    // Handles interactions with chunk bounds
    void HandleChunkBounds()
    {
        Vector2Int currentChunkPos = GetChunkPosition(transform.position, out _);

        if (ChunkPool.activeChunks.TryGetValue(currentChunkPos, out GameObject chunkObject))
        {
            Chunk currentChunk = chunkObject.GetComponent<Chunk>();
            if (currentChunk.meshFilter == null || currentChunk.meshFilter.sharedMesh == null)
            {
                MyRigidbody.velocity = Vector3.zero;
            }
        }
        else
        {
            MyRigidbody.velocity = Vector3.zero;
        }
    }

    // Handles jump or flying logic based on player state
    void HandleJumpOrFly()
    {
        if (isFlying) HandleFly("up");
        else HandleJump();
    }

    // Handles jumping when grounded
    void HandleJump()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.01f, groundLayer);
        if (isGrounded)
        {
            Vector3 velocity = MyRigidbody.velocity;
            MyRigidbody.velocity = new Vector3(velocity.x, jumpHeight, velocity.z);
        }
    }

    // Handles flying logic
    void HandleFly(string direction)
    {
        if (direction == "up")
        {
            MyRigidbody.velocity = Vector3.up * jumpHeight;
        }
        else if (direction == "down")
        {
            MyRigidbody.velocity = Vector3.down * jumpHeight;
        }
    }

    // Handles asynchronous space press detection for flying
    private async void HandleSpacePress()
    {
        if (isWaiting)
        {
            isFlying = !isFlying;
            MyRigidbody.useGravity = !MyRigidbody.useGravity;
            isWaiting = false;
        }
        else
        {
            isWaiting = true;
            await Task.Delay(doublePressDelay);
            if (isWaiting) isWaiting = false;
        }
    }

    // Detects and handles voxel interactions
    void DetectInteractions()
    {
        Ray ray = new Ray(transform.position + (Vector3.up / 2), MainCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, selectDistance, groundLayer))
        {
            Vector3 hitPointAdjusted = Input.GetMouseButton(1)
                ? hit.point + hit.normal * 0.5f
                : hit.point - hit.normal * 0.5f;

            Vector3Int roundedHitPoint = new Vector3Int(
                Mathf.RoundToInt(hitPointAdjusted.x),
                Mathf.RoundToInt(hitPointAdjusted.y),
                Mathf.RoundToInt(hitPointAdjusted.z)
            );

            indicatorBox.transform.position = roundedHitPoint;
            if (!indicatorBox.activeSelf) indicatorBox.SetActive(true);

            if (Input.GetMouseButton(0)) Interact(0, roundedHitPoint, hit.normal);
            else if (Input.GetMouseButton(1)) Interact(1, roundedHitPoint, hit.normal);
        }
        else if (indicatorBox.activeSelf)
        {
            indicatorBox.SetActive(false);
        }
    }

    // Handles voxel interactions
    void Interact(int mouseButton, Vector3Int position, Vector3 direction)
    {
        Vector2Int chunkCoords = GetChunkPosition(position, out Vector3 localPosition);

        if (ChunkPool.activeChunks.TryGetValue(chunkCoords, out GameObject hitChunk))
        {
            Chunk chunkScript = hitChunk.GetComponent<Chunk>();
            Voxel[,,] chunkVoxels = chunkScript.voxels;

            if (localPosition.x < 0 || localPosition.x >= chunkSize ||
                localPosition.y < 0 || localPosition.y >= chunkSize ||
                localPosition.z < 0 || localPosition.z >= chunkSize) return;

            switch (mouseButton)
            {
                case 0: // Remove voxel
                    chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z] = Voxel.Empty;
                    chunkScript.UpdateMeshLocal((int)localPosition.x, (int)localPosition.y, (int)localPosition.z);
                    break;

                case 1: // Place voxel
                    chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z] = selectedBlock;
                    chunkScript.UpdateMeshLocal((int)localPosition.x, (int)localPosition.y, (int)localPosition.z);
                    break;
            }
        }
    }

    // Calculates chunk position and local voxel position
    Vector2Int GetChunkPosition(Vector3 location, out Vector3 localPosition)
    {
        int chunkX = Mathf.FloorToInt(location.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(location.z / chunkSize);

        float localX = location.x % chunkSize;
        float localZ = location.z % chunkSize;

        if (localX < 0) localX += chunkSize;
        if (localZ < 0) localZ += chunkSize;

        localPosition = new Vector3(localX, location.y, localZ);
        return new Vector2Int(chunkX, chunkZ);
    }
}
