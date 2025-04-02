using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Voxel[,,] Voxels;
    public TerrainGenerator terrainGenerator;
    public ChunkPool chunkPool;

    private Material[,] _materials;

    public int x;
    public int y;
    public bool isDirty;

    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public MeshRenderer meshRenderer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        _materials = TextureManager.materials;
    }

    private void OnEnable()
    {
        transform.position = new Vector3(x * 16, 0, y * 16);

        Voxels ??= terrainGenerator.GenerateTerrain(x, y);
        
        isDirty = true;
    }

    private void Update()
    {
        if (Voxels == null) Debug.LogWarning("ahhhhhhhhhhhhhhhhhhhhhhhh 2");
        if (!isDirty) return;
        StartCoroutine(GenerateMesh());
        isDirty = false;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private IEnumerator GenerateMesh()
    {
        Mesh mesh = new();
        List<Vector3> vertices = new();
        List<Vector2> uvs = new();

        // Dictionary to store triangle lists for each texture on each face
        Dictionary<(int, int), List<int>> textureToTriangles = new();
        List<Material> meshMaterials = new();

        // Initialize the triangle list for each texture and face direction
        for (var id = 0; id < _materials.GetLength(0); id++)
        {
            for (var direction = 0; direction < 6; direction++) // 6 faces per voxel
            {
                textureToTriangles[(id, direction)] = new List<int>();
            }
        }

        for (var chunkX = 0; chunkX < 16; chunkX++)
        {
            for (var chunkY = 0; chunkY < 255; chunkY++)
            {
                for (var chunkZ = 0; chunkZ < 16; chunkZ++)
                {
                    var voxel = Voxels[chunkX, chunkY, chunkZ];
                    if (voxel.Type == 0) continue; // Skip empty voxel
                    // Assign faces to corresponding texture groups

                    for (var i = 0; i < 6; i++) // Loop through each face direction
                    {
                        var direction = indexToDirection[i];
                        AddFaceIfNeeded(vertices, textureToTriangles[(voxel.ID, i)], uvs,
                            new Vector3(chunkX, chunkY, chunkZ), direction);
                    }
                }
            }
            if (chunkX % 2 == 0) yield return null;
        }

        // Finalize the mesh data
        mesh.vertices = vertices.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.subMeshCount = textureToTriangles.Count;

        // Assign triangles for each face texture
        var submeshIndex = 0;
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
        for (var type = 0; type < _materials.GetLength(0); type++)
        {
            for (var direction = 0; direction < 6; direction++) // 6 faces per voxel
            {
                meshMaterials.Add(_materials[type, direction]);
            }
        }
        // Apply the materials to the mesh renderer
        meshRenderer.materials = meshMaterials.ToArray();
        // Enable the mesh
        meshRenderer.enabled = true;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public void UpdateNeighborMeshes(int voxelX, int voxelZ)
    {
        switch (voxelX)
        {
            case 0:
                // Update chunk on the left
                chunkPool.ActiveChunks[new Vector2Int(x - 1, y)].GetComponent<Chunk>().isDirty = true;
                break;
            case 15:
                // Update chunk on the right
                chunkPool.ActiveChunks[new Vector2Int(x + 1, y)].GetComponent<Chunk>().isDirty = true;
                break;
        }

        switch (voxelZ)
        {
            case 0:
                // Update chunk below
                chunkPool.ActiveChunks[new Vector2Int(x, y - 1)].GetComponent<Chunk>().isDirty = true;
                break;
            case 15:
                // Update chunk above
                chunkPool.ActiveChunks[new Vector2Int(x, y + 1)].GetComponent<Chunk>().isDirty = true;
                break;
        }

        isDirty = true;
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public async Task LoadData(Vector2Int coord)
    {
        await Task.Run(() => SaveSystem.LoadChunk(coord, out Voxels));
        x = coord.x;
        y = coord.y;
    }

    private void AddFaceIfNeeded(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 position, Vector3 direction)
    {
        if (ChunkUtils.ShouldAddFace(this, (int)position.x, (int)position.y, (int)position.z, direction))
        {
            AddFace(vertices, triangles, uvs, position, direction);
        }
    }

    private static void AddFace(List<Vector3> vertices, List<int> triangles, List<Vector2> uvs, Vector3 position, Vector3 direction)
    {
        var faceVertices = direction switch
        {
            { } when direction == Vector3.up => faceVerticesUp,
            { } when direction == Vector3.down => faceVerticesDown,
            { } when direction == Vector3.right => faceVerticesRight,
            { } when direction == Vector3.left => faceVerticesLeft,
            { } when direction == Vector3.forward => faceVerticesForward,
            _ => faceVerticesBack,
        };

        var vertexCount = vertices.Count;

        for (var i = 0; i < 4; i++)
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

    private static readonly Vector3[] indexToDirection = {
        Vector3.back,
        Vector3.right,
        Vector3.forward,
        Vector3.left,
        Vector3.up,
        Vector3.down
    };

    public bool HasMesh => meshFilter != null;

    // Define face vertices depending on the direction
    private static readonly Vector3[] faceVerticesUp = {
        new(-0.5f, 0.5f, -0.5f),
        new(0.5f, 0.5f, -0.5f),
        new(-0.5f, 0.5f, 0.5f),
        new(0.5f, 0.5f, 0.5f)
    };

    private static readonly Vector3[] faceVerticesDown = {
        new(-0.5f, -0.5f, 0.5f),
        new(0.5f, -0.5f, 0.5f),
        new(-0.5f, -0.5f, -0.5f),
        new(0.5f, -0.5f, -0.5f)
    };

    private static readonly Vector3[] faceVerticesRight = {
        new(0.5f, 0.5f, 0.5f),
        new(0.5f, 0.5f, -0.5f),
        new(0.5f, -0.5f, 0.5f),
        new(0.5f, -0.5f, -0.5f)
    };

    private static readonly Vector3[] faceVerticesLeft = {
        new(-0.5f, 0.5f, -0.5f),
        new(-0.5f, 0.5f, 0.5f),
        new(-0.5f, -0.5f, -0.5f),
        new(-0.5f, -0.5f, 0.5f)
    };

    private static readonly Vector3[] faceVerticesForward = {
        new(-0.5f, 0.5f, 0.5f),
        new(0.5f, 0.5f, 0.5f),
        new(-0.5f, -0.5f, 0.5f),
        new(0.5f, -0.5f, 0.5f)
    };

    private static readonly Vector3[] faceVerticesBack = {
        new(0.5f, 0.5f, -0.5f),
        new(-0.5f, 0.5f, -0.5f),
        new(0.5f, -0.5f, -0.5f),
        new(-0.5f, -0.5f, -0.5f)
    };

    private static readonly Vector2[] faceUVs = {
        new(0, 1),
        new(1, 1),
        new(0, 0),
        new(1, 0)
    };
}