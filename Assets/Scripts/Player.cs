using System;
using Items;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

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
    public GameObject indicatorBox;
    public LayerMask groundLayer;
    public float selectDistance = 5;
    public ChunkPool chunkPool;
    public bool canFly;
    private Voxel _selectedBlock = new(Voxel.IDs.Cobblestone, Voxel.Types.Solid);

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
        var currentChunkPos = GetChunkPosition(new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z), out var _);
        if (!chunkPool.ActiveChunks.ContainsKey(currentChunkPos))
        {
            myRigidbody.linearVelocity = Vector3.zero;
        }
        else if (!chunkPool.ActiveChunks[new(currentChunkPos.x, currentChunkPos.y)].HasMesh)
        {
            myRigidbody.linearVelocity = Vector3.zero;
        }
        else
        {
            myRigidbody.WakeUp();
        }
    }
    
    private void Update()
    {
        var selectedItem = inventoryManager.HotbarItems[(int)Mathf.Round(inventoryManager.hotbarSlot)];
        _selectedBlock = selectedItem switch
        {
            Air => new(0),
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
        movementDirection.Normalize(); // No square root of 2 times extra speed when holding 2 keys at once

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
            Vector3 hitPointAdjusted = hit.point - hit.normal * 0.5f;

            // Round the hit point adjusted to the nearest integer
            Vector3Int roundedHitPoint = new(
                Mathf.RoundToInt(hitPointAdjusted.x),
                Mathf.RoundToInt(hitPointAdjusted.y),
                Mathf.RoundToInt(hitPointAdjusted.z)
            );

            indicatorBox.transform.position = roundedHitPoint;

            // Enable the box if it was disabled
            if (!indicatorBox.activeSelf)
            {
                indicatorBox.SetActive(true);
            }

            if (Input.GetMouseButton(0))
                Interact(0, roundedHitPoint);
            else if (Input.GetMouseButtonDown(1))
                Interact(1, roundedHitPoint + new Vector3Int((int)hit.normal.x, (int)hit.normal.y, (int)hit.normal.z));
            else if (Input.GetMouseButtonDown(2))
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
        if (!chunkPool.ActiveChunks.TryGetValue(chunkCoords, out var chunkScript)) return;
        var chunkVoxels = chunkScript.Voxels;

        switch (mouseButton)
        {
            case 0: // Left-click: Remove voxel

                var voxel = chunkVoxels[localPosition.x, localPosition.y, localPosition.z];
                if (voxel.Type == 0) break;
                // Debug.Log($"Deleted {voxel}");
                chunkVoxels[localPosition.x, localPosition.y, localPosition.z].DealDamage(0.1f);
                chunkScript.UpdateMeshLocal(localPosition.x, localPosition.y, localPosition.z);
                break;
            case 1: // Right-click: Place voxel
                chunkVoxels[localPosition.x, localPosition.y, localPosition.z] = _selectedBlock;
                chunkScript.UpdateMeshLocal(localPosition.x, localPosition.y, localPosition.z);
                break;
            case 2:
                _selectedBlock = chunkVoxels[localPosition.x, localPosition.y, localPosition.z];
                break;
        }
    }

    private static Vector3Int GetChunkPosition(Vector3Int location, out Vector3Int localPosition)
    {
        // Calculate chunk coordinates
        var chunkX = Mathf.FloorToInt(location.x / Chunk.chunkSize);
        var chunkY = Mathf.FloorToInt(location.y / Chunk.chunkSize);
        var chunkZ = Mathf.FloorToInt(location.z / Chunk.chunkSize);

        // Calculate local position within the chunk
        var localX = location.x % Chunk.chunkSize;
        var localY = location.y % Chunk.chunkSize;
        var localZ = location.z % Chunk.chunkSize;

        // Ensure the local position is positive
        if (localX < 0) localX += Chunk.chunkSize;
        if (localY < 0) localY += Chunk.chunkSize;
        if (localZ < 0) localZ += Chunk.chunkSize;

        localPosition = new(localX, localY, localZ);
        return new(chunkX, chunkY, chunkZ);
    }
}
