using Voxels;
using UnityEngine;
public class Player : MonoBehaviour
{
    public Rigidbody MyRigidbody;
    public Transform MainCamera;
    public float baseMovementSpeed;
    public float movementSpeed;
    public float jumpHeight;
    private bool isGrounded;
    private Vector3 currentVelocity = Vector3.zero;
    private const int chunkSize = 16;
    private GameObject indicatorBox;
    public LayerMask groundLayer;
    public float selectDistance = 5;
    public GameObject boxPrefab;
    public ChunkPool chunkPool;
    public bool canFly;
    public Voxel selectedBlock = new Voxels.Solid.Cobblestone();

    // Start is called before the first frame update
    void Start()
    {
        // Instantiate the indicator box once and disable it initially
        indicatorBox = Instantiate(boxPrefab);
        indicatorBox.transform.localScale = new Vector3(1.05f, 1.05f, 1.05f);

        // Set the box's material color to translucent white
        Renderer boxRenderer = indicatorBox.GetComponent<Renderer>();
        if (boxRenderer != null)
        {
            Color translucentWhite = new Color(1f, 1f, 1f, 0.5f);
            boxRenderer.material.color = translucentWhite;
        }

        // Disable the box at start
        indicatorBox.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        MovePlayer();
        Vector3 targetVelocity = Vector3.SmoothDamp(MyRigidbody.velocity, Vector3.zero, ref currentVelocity, 0.2f);
        MyRigidbody.velocity = new Vector3(targetVelocity.x, MyRigidbody.velocity.y, targetVelocity.z);

        if (Input.GetKeyDown(KeyCode.R))
        {
            MyRigidbody.velocity = Vector3.zero;
            transform.position = Vector3.zero;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            if (canFly) HandleFly("up");
            else HandleJump();
        }

        if (Input.GetKey(KeyCode.LeftShift))
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

        if (!chunkPool.activeChunks.ContainsKey(GetChunkPosition(transform.position, out Vector3 n)))
        {
            MyRigidbody.velocity = Vector3.zero;
        }
        DetectInteractions();
    }

    void MovePlayer()
    {
        Vector3 movementDirection = Vector3.zero;

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
        movementDirection.Normalize(); // Maintain consistent speed

        MyRigidbody.velocity += movementDirection * movementSpeed;
    }

    void HandleJump()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.01f, groundLayer);
        if (isGrounded)
        {
            Vector3 velocity = MyRigidbody.velocity;
            MyRigidbody.velocity = new Vector3(velocity.x, jumpHeight, velocity.z);
        }
    }

    void HandleFly(string direction)
    {
        if (direction == "up")
        {
            MyRigidbody.velocity += Vector3.up * jumpHeight;
        }
        else if (direction == "down")
        {
            MyRigidbody.velocity += Vector3.down * jumpHeight;
        }
    }

    void DetectInteractions()
    {
        Ray ray = new Ray(transform.position + (Vector3.up / 2), MainCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, selectDistance, groundLayer))
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
            Vector3Int roundedHitPoint = new Vector3Int(
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
                Interact(0, roundedHitPoint, hit.normal);
            else if (Input.GetMouseButton(1))
                Interact(1, roundedHitPoint, hit.normal);
            else if (Input.GetMouseButton(2))
                Interact(2, roundedHitPoint, hit.normal);
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

    void Interact(int mouseButton, Vector3Int position, Vector3 direction)
    {
        Vector2Int chunkCoords = GetChunkPosition(position, out Vector3 localPosition);

        // Ensure the position is within chunk bounds before interacting
        if (chunkPool.activeChunks.TryGetValue(chunkCoords, out GameObject hitChunk))
        {
            Chunk chunkScript = hitChunk.GetComponent<Chunk>();
            Voxel[,,] chunkVoxels = chunkScript.voxels;

            switch (mouseButton)
            {
                case 0: // Left-click: Remove voxel

                    Voxel voxel = chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z];
                    if (voxel == Voxel.Empty) break;
                    Debug.Log($"Deleted {voxel}");
                    chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z] = Voxel.Empty;
                    chunkScript.UpdateMeshLocal((int)localPosition.x, (int)localPosition.y, (int)localPosition.z);
                    break;
                case 1: // Right-click: Place voxel
                    chunkVoxels[(int)localPosition.x, (int)localPosition.y, (int)localPosition.z] = selectedBlock;
                    chunkScript.UpdateMeshLocal((int)localPosition.x, (int)localPosition.y, (int)localPosition.z);
                    break;
                default:
                    break;
            }
        }
    }

    Vector2Int GetChunkPosition(Vector3 location, out Vector3 localPosition)
    {
        // Calculate chunk coordinates
        int chunkX = Mathf.FloorToInt(location.x / chunkSize);
        int chunkZ = Mathf.FloorToInt(location.z / chunkSize);

        // Calculate local position within the chunk
        float localX = location.x % chunkSize;
        float localZ = location.z % chunkSize;

        // Ensure the local position is positive
        if (localX < 0) localX += chunkSize;
        if (localZ < 0) localZ += chunkSize;

        localPosition = new Vector3(localX, location.y, localZ);
        return new Vector2Int(chunkX, chunkZ);
    }
}
