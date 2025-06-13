using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

public class Chunk : MonoBehaviour
{
    /// <summary>
    /// The side length of the chunk in blocks.
    /// </summary>
    public const int CHUNK_SIZE = 32; // DO NOT CHANGE
    public Block[,,] Blocks = new Block[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];

    /// <summary>
    /// The chunk coordinates of the chunk.
    /// </summary>
    public Vector3Int position;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    private readonly List<Vector3> vertices = new();
    private readonly List<int> triangles = new();
    private readonly List<Vector2> uvs = new();

    /// <summary>
    /// Flag to indicate if the mesh needs to be updated.
    /// </summary>
    public bool isDirty = true;

    private void Awake()
    {
        meshFilter.sharedMesh = new Mesh();
        meshRenderer.sharedMaterial = TextureManager.material;
    }

    private void Start()
    {
        TerrainGenerator.Instance.GenerateTerrain(Blocks, position);
    }

    private void OnEnable()
    {
        transform.position = position * Chunk.CHUNK_SIZE; // Set the position of the chunk based on its coordinates
    }

    private void Update()
    {
        if (isDirty)
        {
            isDirty = false; // Reset the dirty flag
            CreateMesh();
        }
    }

    private void CreateMesh()
    {
        Mesh mesh = meshFilter.sharedMesh;
        mesh.Clear();
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        int arbitraryCounter = 0;
        // Loop through all the blocks
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    // Process each block in the chunk
                    Block block = Blocks[x, y, z];

                    Vector3Int pos = new(x, y, z);

                    if (block.id == 0)  {
                        arbitraryCounter++;
                        continue; // Skip empty blocks
                    }

                    for (int i = 0; i < 6; i++) // Check all 6 faces of the block
                    {
                        Vector3Int dir = directions[i];
                        if (IsFaceVisible(pos, dir)) AddQuad(pos, dir, block.id, i);
                    }
                }
            }
        }

        if (arbitraryCounter == CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE)
        {
            gameObject.SetActive(false);
        }
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        meshCollider.sharedMesh = mesh;
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
        uvs.Add(new Vector2(tileWidth * (i + 1 ), uvTop));     // top-right
    }

    private bool IsFaceVisible(Vector3Int pos, Vector3Int dir)
    {
        Vector3Int neighborPos = pos + dir;
        // Check if the neighbor position is within bounds of the chunk
        if (neighborPos.x < 0 || neighborPos.x >= CHUNK_SIZE ||
            neighborPos.y < 0 || neighborPos.y >= CHUNK_SIZE ||
            neighborPos.z < 0 || neighborPos.z >= CHUNK_SIZE)
        {
            return true; // If out of bounds, the face is visible
        }

        return Blocks[neighborPos.x, neighborPos.y, neighborPos.z].id == 0; // Check if the neighboring block is not solid
    }

    private readonly Vector3Int[] directions = new Vector3Int[]
    {
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.up,
        Vector3Int.down
    };
}
