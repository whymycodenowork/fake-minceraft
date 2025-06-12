using UnityEngine;
using System.Collections.Generic;
using UnityEditorInternal;

public class Chunk : MonoBehaviour
{
    public const int CHUNK_SIZE = 16;
    public Block[,,] Blocks = new Block[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    private List<Vector3> vertices = new();
    private List<int> triangles = new();
    private List<Vector2> uvs = new();

    /// <summary>
    /// Flag to indicate if the mesh needs to be updated
    /// </summary>
    public bool isDirty = true;

    private void Awake()
    {
        meshFilter.sharedMesh = new Mesh();
        meshRenderer.sharedMaterial = TextureManager.material;
        meshCollider.sharedMesh = meshFilter.sharedMesh;
    }

    private void Start()
    {
        for (int x = 0; x < CHUNK_SIZE; x++)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    Blocks[x, y, z] = new Block { id = 1 }; // Placeholder for actual terrain generation logic
                }
            }
        }
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
                        if (IsFaceVisible(pos, dir)) AddQuad(pos, dir, block.id);
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
    }

    private void AddQuad(Vector3Int pos, Vector3Int dir, int id)
    {
        if (dir == Vector3Int.up)
        {
            // +y face
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, 0.5f)); // bl
            vertices.Add(pos + new Vector3(0.5f, 0.5f, 0.5f)); // br
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, -0.5f)); // tl
            vertices.Add(pos + new Vector3(0.5f, 0.5f, -0.5f)); // tr
        }
        else if (dir == Vector3Int.down)
        {
            // -y face
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, -0.5f)); // bl
            vertices.Add(pos + new Vector3(0.5f, -0.5f, -0.5f)); // br
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, 0.5f)); // tl
            vertices.Add(pos + new Vector3(0.5f, -0.5f, 0.5f)); // tr
        }
        else if (dir == Vector3Int.forward)  // +z
        {
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, 0.5f)); // bl
            vertices.Add(pos + new Vector3(0.5f, -0.5f, 0.5f)); // br
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, 0.5f)); // tl
            vertices.Add(pos + new Vector3(0.5f, 0.5f, 0.5f)); // tr
        }
        else if (dir == Vector3Int.back)     // -z
        {
            vertices.Add(pos + new Vector3(0.5f, -0.5f, -0.5f)); // bl
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, -0.5f)); // br
            vertices.Add(pos + new Vector3(0.5f, 0.5f, -0.5f)); // tl
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, -0.5f)); // tr
        }
        else if (dir == Vector3Int.left)     // -x
        {
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, -0.5f)); // bl
            vertices.Add(pos + new Vector3(-0.5f, -0.5f, 0.5f)); // br
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, -0.5f)); // tl
            vertices.Add(pos + new Vector3(-0.5f, 0.5f, 0.5f)); // tr
        }
        else if (dir == Vector3Int.right)    // +x
        {
            vertices.Add(pos + new Vector3(0.5f, -0.5f, 0.5f)); // bl
            vertices.Add(pos + new Vector3(0.5f, -0.5f, -0.5f)); // br
            vertices.Add(pos + new Vector3(0.5f, 0.5f, 0.5f)); // tl
            vertices.Add(pos + new Vector3(0.5f, 0.5f, -0.5f)); // tr
        }

        int startIndex = vertices.Count - 4;
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 3);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 0);

        float tileSize = 1f / TextureManager.atlasSize; // fraction size of one tile in uv space

        // calculate uv rectangle for this id tile in the atlas
        float uvBottom = id * tileSize;
        float uvTop = uvBottom + tileSize;

        // assign uvs for quad vertices (bottom-left, bottom-right, top-left, top-right)
        uvs.Add(new Vector2(0, uvBottom));  // bottom-left
        uvs.Add(new Vector2(1, uvBottom));  // bottom-right
        uvs.Add(new Vector2(0, uvTop));     // top-left
        uvs.Add(new Vector2(1, uvTop));     // top-right
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

        return Blocks[neighborPos.x, neighborPos.y, neighborPos.z].id != 1; // Check if the neighboring block is not solid
    }

    private readonly Vector3Int[] directions = new Vector3Int[]
    {
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.forward,
        Vector3Int.back
    };
}
