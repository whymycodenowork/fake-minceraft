using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Voxel[,,] voxels = null;
    public TerrainGenerator terrainGenerator;
    public ChunkPool chunkPool;

    public Material[,] materials;

    public int x;
    public int y;
    public bool isDirty;

    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public MeshRenderer meshRenderer;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        materials = TextureManager.materials;
    }

    void OnEnable()
    {
        transform.position = new Vector3(x * 16, 0, y * 16);

        voxels ??= terrainGenerator.GenerateTerrain(x, y);

        CreateMesh();
    }

    public void CreateMesh()
    {
        isDirty = true;
        meshFilter.sharedMesh = null;
    }

    void Update()
    {
        if (isDirty)
        {
            StartCoroutine(GenerateMesh());
            isDirty = false;
        }
    }

    IEnumerator GenerateMesh()
    {
        Mesh mesh = new();
        List<Vector3> vertices = new();
        List<Vector2> uvs = new();

        // Dictionary to store triangle lists for each texture on each face
        Dictionary<(int, int), List<int>> textureToTriangles = new();
        List<Material> meshMaterials = new();

        // Initialize the triangle list for each texture and face direction
        for (int id = 0; id < this.materials.GetLength(0); id++)
        {
            for (int direction = 0; direction < 6; direction++) // 6 faces per voxel
            {
                textureToTriangles[(id, direction)] = new List<int>();
            }
        }

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 255; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    Voxel voxel = voxels[x, y, z];
                    if (voxel.type == 0) continue; // Skip empty voxel
                    // Assign faces to corresponding texture groups

                    for (int i = 0; i < 6; i++) // Loop through each face direction
                    {
                        Vector3 direction = indexToDirection[i];
                        AddFaceIfNeeded(vertices, textureToTriangles[(voxel.id, i)], uvs, new Vector3(x, y, z), direction);
                    }
                }
            }
            if (x % 2 == 0) yield return null;
        }

        // Finalize the mesh data
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.subMeshCount = textureToTriangles.Count;

        // Assign triangles for each face texture
        int submeshIndex = 0;
        foreach (var triangles in textureToTriangles.Values)
        {
            mesh.SetTriangles(triangles, submeshIndex++);
        }

        // Finalize the mesh
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // Assign the mesh to the mesh filter and collider
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        // Assign materials based on the face textures
        for (int type = 0; type < materials.GetLength(0); type++)
        {
            for (int direction = 0; direction < 6; direction++) // 6 faces per voxel
            {
                meshMaterials.Add(materials[type, direction]);
            }
        }
        // Apply the materials to the mesh renderer
        meshRenderer.materials = meshMaterials.ToArray();
    }

    public void UpdateMeshLocal(int voxelX, int voxelZ)
    {
        if (voxelX == 0)
        {
            // Update chunk on the left
            chunkPool.activeChunks[new Vector2Int(x - 1, y)].GetComponent<Chunk>().isDirty = true;
        }
        if (voxelX == 15)
        {
            // Update chunk on the right
            chunkPool.activeChunks[new Vector2Int(x + 1, y)].GetComponent<Chunk>().isDirty = true;
        }
        if (voxelZ == 0)
        {
            // Update chunk below
            chunkPool.activeChunks[new Vector2Int(x, y - 1)].GetComponent<Chunk>().isDirty = true;
        }
        if (voxelZ == 15)
        {
            // Update chunk above
            chunkPool.activeChunks[new Vector2Int(x, y + 1)].GetComponent<Chunk>().isDirty = true;
        }
        isDirty = true; // Add local mesh logic later
    }

    public void LoadData(Vector2Int coord)
    {
        SaveSystem.LoadChunk(coord, out voxels);
        x = coord.x;
        y = coord.y;
        // Once data is loaded, mark the mesh as dirty
        CreateMesh();
    }

    void AddFaceIfNeeded(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 position, Vector3 direction)
    {
        if (ChunkUtils.ShouldAddFace(this, (int)position.x, (int)position.y, (int)position.z, direction))
        {
            AddFace(vertices, triangles, uvs, position, direction);
        }
    }

    void AddFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 position, Vector3 direction)
    {
        Vector3[] faceVertices = direction switch
        {
            { } d when d == Vector3.up => faceVerticesUp,
            { } d when d == Vector3.down => faceVerticesDown,
            { } d when d == Vector3.right => faceVerticesRight,
            { } d when d == Vector3.left => faceVerticesLeft,
            { } d when d == Vector3.forward => faceVerticesForward,
            _ => faceVerticesBack,
        };

        int vertexCount = vertices.Count;

        for (int i = 0; i < 4; i++)
        {
            vertices.Add(position + faceVertices[i]);
            uvs.Add(faceUVs[i]);
        }

        triangles.Add(vertexCount);
        triangles.Add(vertexCount + 2);
        triangles.Add(vertexCount + 1);
        triangles.Add(vertexCount + 1);
        triangles.Add(vertexCount + 2);
        triangles.Add(vertexCount + 3);
    }

    private static readonly Vector3[] indexToDirection = new Vector3[]
    {
        Vector3.back,
        Vector3.right,
        Vector3.forward,
        Vector3.left,
        Vector3.up,
        Vector3.down
    };

    public bool HasMesh => meshFilter != null;

    // Define face vertices depending on the direction
    private static readonly Vector3[] faceVerticesUp = new Vector3[]
    {
        new(-0.5f, 0.5f, -0.5f),
        new(0.5f, 0.5f, -0.5f),
        new(-0.5f, 0.5f, 0.5f),
        new(0.5f, 0.5f, 0.5f)
    };

    private static readonly Vector3[] faceVerticesDown = new Vector3[]
    {
        new(-0.5f, -0.5f, 0.5f),
        new(0.5f, -0.5f, 0.5f),
        new(-0.5f, -0.5f, -0.5f),
        new(0.5f, -0.5f, -0.5f)
    };

    private static readonly Vector3[] faceVerticesRight = new Vector3[]
    {
        new(0.5f, 0.5f, 0.5f),
        new(0.5f, 0.5f, -0.5f),
        new(0.5f, -0.5f, 0.5f),
        new(0.5f, -0.5f, -0.5f)
    };

    private static readonly Vector3[] faceVerticesLeft = new Vector3[]
    {
        new(-0.5f, 0.5f, -0.5f),
        new(-0.5f, 0.5f, 0.5f),
        new(-0.5f, -0.5f, -0.5f),
        new(-0.5f, -0.5f, 0.5f)
    };

    private static readonly Vector3[] faceVerticesForward = new Vector3[]
    {
        new(-0.5f, 0.5f, 0.5f),
        new(0.5f, 0.5f, 0.5f),
        new(-0.5f, -0.5f, 0.5f),
        new(0.5f, -0.5f, 0.5f)
    };

    private static readonly Vector3[] faceVerticesBack = new Vector3[]
    {
        new(0.5f, 0.5f, -0.5f),
        new(-0.5f, 0.5f, -0.5f),
        new(0.5f, -0.5f, -0.5f),
        new(-0.5f, -0.5f, -0.5f)
    };

    private static readonly Vector2[] faceUVs = new Vector2[]
    {
        new(0, 1),
        new(1, 1),
        new(0, 0),
        new(1, 0)
    };
}