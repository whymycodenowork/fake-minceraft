using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    /// <summary>
    /// The side length of the chunk in blocks.
    /// </summary>
    public const int CHUNK_SIZE = 32;
    public Block[,,] Blocks = new Block[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];

    /// <summary>
    /// The chunk coordinates of the chunk.
    /// </summary>
    public Vector3Int position;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    private readonly List<Vector3> vertices = new(12 * CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE);
    private readonly List<int> triangles = new(20 * CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE);
    private readonly List<Vector2> uvs = new(12 * CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE);
    private readonly List<Vector3> normals = new(12 * CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE);

    /// <summary>
    /// Flag to indicate if the mesh needs to be updated.
    /// </summary>
    public bool isDirty = false;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = new();
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = TextureManager.material;
        meshCollider = GetComponent<MeshCollider>();
    }
    private void Update()
    {
        if (isDirty)
        {
            isDirty = false; // Reset the dirty flag
            CreateMesh();
        }
    }

    /// <summary>
    /// Places a block at the specified coordinates in the chunk and updates the chunk's mesh and nearby chunks' meshes.
    /// </summary>
    /// <param name="x">The X-coordinate of the block to place.</param>
    /// <param name="y">The Y-coordinate of the block to place.</param>
    /// <param name="z">The Z-coordinate of the block to place.</param>
    /// <param name="block">The block to place.></param>
    public void PlaceBlock(int x, int y, int z, Block block)
    {
        Blocks[x, y, z] = block;
        isDirty = true; // Mark the chunk as dirty to update the mesh
        UpdateNeighbors(x, y, z); // Update neighboring chunks
    }

    /// <summary>
    /// Places a block at the specified coordinates in the chunk and updates the chunk's mesh and nearby chunks' meshes.
    /// </summary>
    /// <param name="pos">The coordinates to place the block.</param>
    /// <param name="block">The block to place.</param>
    public void PlaceBlock(Vector3Int pos, Block block)
    {
        PlaceBlock(pos.x, pos.y, pos.z, block);
    }

    /// <summary>
    /// Removes a block at the specified coordinates in the chunk and updates the chunk's mesh and nearby chunks' meshes.
    /// </summary>
    /// <param name="x">The X-coordinate of the block to remove.</param>
    /// <param name="y">The Y-coordinate of the block to remove.</param>
    /// <param name="z">The Z-coordinate of the block to remove.</param>
    public void RemoveBlock(int x, int y, int z)
    {
        Blocks[x, y, z].Break(); // Break the block
        isDirty = true; // Mark the chunk as dirty to update the mesh
        UpdateNeighbors(x, y, z); // Update neighboring chunks
    }

    /// <summary>
    /// Removes a block at the specified coordinates in the chunk and updates the chunk's mesh and nearby chunks' meshes.
    /// </summary>
    /// <param name="pos">The coordinates of the block to remove.</param>
    public void RemoveBlock(Vector3Int pos)
    {
        RemoveBlock(pos.x, pos.y, pos.z);
    }

    // Method to update the neighbors of the chunk
    public void UpdateNeighbors(int x, int y, int z)
    {
        switch (x)
        {
            case 0:
                if (ChunkManager.Instance.ActiveChunks.TryGetValue(new Vector3Int(position.x - 1, position.y, position.z), out Chunk leftChunk))
                {
                    leftChunk.isDirty = true; // Mark the left chunk as dirty
                }

                break;
            case CHUNK_SIZE - 1:
                if (ChunkManager.Instance.ActiveChunks.TryGetValue(new Vector3Int(position.x + 1, position.y, position.z), out Chunk rightChunk))
                {
                    rightChunk.isDirty = true; // Mark the right chunk as dirty
                }

                break;
        }

        switch (y)
        {
            case 0:
                if (ChunkManager.Instance.ActiveChunks.TryGetValue(new Vector3Int(position.x, position.y - 1, position.z), out Chunk downChunk))
                {
                    downChunk.isDirty = true; // Mark the down chunk as dirty
                }

                break;
            case CHUNK_SIZE - 1:
                if (ChunkManager.Instance.ActiveChunks.TryGetValue(new Vector3Int(position.x, position.y + 1, position.z), out Chunk upChunk))
                {
                    upChunk.isDirty = true; // Mark the up chunk as dirty
                }

                break;
        }

        switch (z)
        {
            case 0:
                if (ChunkManager.Instance.ActiveChunks.TryGetValue(new Vector3Int(position.x, position.y, position.z - 1), out Chunk backChunk))
                {
                    backChunk.isDirty = true; // Mark the back chunk as dirty
                }

                break;
            case CHUNK_SIZE - 1:
                if (ChunkManager.Instance.ActiveChunks.TryGetValue(new Vector3Int(position.x, position.y, position.z + 1), out Chunk frontChunk))
                {
                    frontChunk.isDirty = true; // Mark the front chunk as dirty
                }

                break;
        }
    }

    // Overloaded method to update neighbors using Vector3Int
    public void UpdateNeighbors(Vector3Int pos)
    {
        UpdateNeighbors(pos.x, pos.y, pos.z);
    }

    private async void CreateMesh()
    {
        Mesh mesh = meshFilter.sharedMesh;
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        normals.Clear();
        await Task.Run(() =>
        {
            // Loop through all the blocks
            Vector3Int pos = new();
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < CHUNK_SIZE; z++)
                    {
                        // Process each block in the chunk
                        pos.Set(x, y, z);
                        Block block = Blocks[x, y, z];

                        if (block.ID == 0)
                        {
                            continue; // Skip empty blocks
                        }

                        for (int i = 0; i < 6; i++) // Check all 6 faces of the block
                        {
                            Vector3Int dir = directions[i];
                            if (IsFaceVisible(pos, dir))
                            {
                                AddQuad(pos, dir, block.ID, i);
                            }
                        }
                    }
                }
            }
        });

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetNormals(normals);
        mesh.bounds = new Bounds(Vector3.one * (CHUNK_SIZE / 2), Vector3.one * CHUNK_SIZE);
        mesh.UploadMeshData(false); // Upload the mesh data to the GPU

        if (mesh.vertexCount == 0)
        {
            meshCollider.sharedMesh = null; // Disable the mesh collider if no vertices are present
            meshCollider.enabled = false;
        }
        else
        {
            meshCollider.sharedMesh = mesh;
            meshCollider.enabled = true;
        }
    }

    private void AddQuad(Vector3Int pos, Vector3Int dir, int id, int i)
    {
        if (dir == Vector3Int.up)
        {
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, 0.5f));
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, -0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, -0.5f));
        }
        else if (dir == Vector3Int.down)
        {
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, -0.5f, 0.5f));
        }
        else if (dir == Vector3Int.forward)
        {
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, 0.5f));
        }
        else if (dir == Vector3Int.back)
        {
            vertices.Add(pos + new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, -0.5f));
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, -0.5f));
        }
        else if (dir == Vector3Int.left)
        {
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, 0.5f));
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, -0.5f));
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, 0.5f));
        }
        else if (dir == Vector3Int.right)
        {
            vertices.Add(pos + new Vector3(0.5f, -0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, -0.5f, -0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, 0.5f));
            vertices.Add(pos + new Vector3(0.5f, 0.5f, -0.5f));
        }

        int startIndex = vertices.Count - 4;
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 3);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 0);

        float tileHeight = 1f / TextureManager.atlasSize; // fraction size of one tile in uv space
        float tileWidth = 1f / 8;

        // calculate uv rectangle for this id tile in the atlas
        float uvBottom = id * tileHeight;
        float uvTop = uvBottom + tileHeight;

        // assign uvs for quad vertices (bottom-left, bottom-right, top-left, top-right)
        uvs.Add(new Vector2(tileWidth * i, uvBottom));  // bottom-left
        uvs.Add(new Vector2(tileWidth * (i + 1), uvBottom));  // bottom-right
        uvs.Add(new Vector2(tileWidth * i, uvTop));     // top-left
        uvs.Add(new Vector2(tileWidth * (i + 1), uvTop));     // top-right

        normals.Add(dir);
        normals.Add(dir);
        normals.Add(dir);
        normals.Add(dir);
    }

    private bool IsFaceVisible(Vector3Int pos, Vector3Int dir)
    {
        Vector3Int neighborPos = pos + dir;

        if (neighborPos.x >= 0 && neighborPos.x < CHUNK_SIZE &&
            neighborPos.y >= 0 && neighborPos.y < CHUNK_SIZE &&
            neighborPos.z >= 0 && neighborPos.z < CHUNK_SIZE)
        {
            return Blocks[neighborPos.x, neighborPos.y, neighborPos.z].ID == 0;
        }

        Vector3Int worldNeighborPos = (position * CHUNK_SIZE) + neighborPos;
        Vector3Int neighborChunkPos = new(
            worldNeighborPos.x >> 5, // Divide by 32
            worldNeighborPos.y >> 5,
            worldNeighborPos.z >> 5
        );
        Vector3Int localNeighborPos = new(
            worldNeighborPos.x & (CHUNK_SIZE - 1),
            worldNeighborPos.y & (CHUNK_SIZE - 1),
            worldNeighborPos.z & (CHUNK_SIZE - 1)
        );

        return ChunkManager.Instance.ActiveChunks.TryGetValue(neighborChunkPos, out Chunk neighborChunk) && neighborChunk.Blocks[localNeighborPos.x, localNeighborPos.y, localNeighborPos.z].ID == 0;
    }

    private static readonly Vector3Int[] directions = new Vector3Int[]
    {
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.up,
        Vector3Int.down
    };

    /// <summary>
    /// Util method to get the chunk position from a world position.
    /// </summary>
    /// <param name="worldPos">The position in the world.</param>
    /// <returns>The chunk position.</returns>
    public static Vector3Int GetChunkPosition(Vector3Int worldPos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x / (float)CHUNK_SIZE),
            Mathf.FloorToInt(worldPos.y / (float)CHUNK_SIZE),
            Mathf.FloorToInt(worldPos.z / (float)CHUNK_SIZE)
        );
    }

    /// <summary>
    /// Util method to get the local position inside of a chunk from a world position.
    /// </summary>
    /// <param name="worldPos">The position in the world</param>
    /// <returns>The local position inside of a chunk.</returns>
    public static Vector3Int GetLocalPosition(Vector3Int worldPos)
    {
        return new Vector3Int(
            Mod(worldPos.x, CHUNK_SIZE),
            Mod(worldPos.y, CHUNK_SIZE),
            Mod(worldPos.z, CHUNK_SIZE)
        );
    }

    private static int Mod(int a, int b)
    {
        int r = a % b;
        return r < 0 ? r + b : r;
    }
}
