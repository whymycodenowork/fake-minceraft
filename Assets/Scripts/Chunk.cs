using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    private readonly List<Vector3> vertices = new();
    private readonly List<int> triangles = new();
    private readonly List<Vector2> uvs = new();

    public Voxel[,,] Voxels = null;

    public Vector3Int position;

    public bool IsDirty { get; set; }

    public const int chunkSize = 32;

    private void Awake()
    {
        // Initialize Mesh Components
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        meshCollider = gameObject.GetComponent<MeshCollider>();
    }
    private void OnEnable()
    {
        Voxels ??= TerrainGenerator.Instance.GenerateTerrain(position.x, position.y); // generate terrain if null

        transform.position = position * chunkSize;

        IsDirty = true;
    }
    public async Task CheckMesh()
    {
        try
        {
            if (!IsDirty)
            {
                return;
            }

            IsDirty = false;
            await GenerateMesh();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    public void UpdateMeshLocal(int voxelX, int voxelY, int voxelZ)
    {
        switch (voxelX)
        {
            case 0:
                // Update chunk on the left
                ChunkPool.Instance.ActiveChunks[new(position.x - 1, position.y, position.z)].GetComponent<Chunk>().IsDirty = true;
                break;
            case chunkSize - 1:
                // Update chunk on the right
                ChunkPool.Instance.ActiveChunks[new(position.x + 1, position.y, position.z)].GetComponent<Chunk>().IsDirty = true;
                break;
        }

        switch (voxelY)
        {
            case 0:
                // Update chunk below
                ChunkPool.Instance.ActiveChunks[new(position.x, position.y - 1, position.z)].GetComponent<Chunk>().IsDirty = true;
                break;
            case chunkSize - 1:
                // Update chunk above
                ChunkPool.Instance.ActiveChunks[new(position.x, position.y + 1, position.z)].GetComponent<Chunk>().IsDirty = true;
                break;
        }

        switch (voxelZ)
        {
            case 0:
                // Update chunk behind
                ChunkPool.Instance.ActiveChunks[new(position.x, position.y, position.z - 1)].GetComponent<Chunk>().IsDirty = true;
                break;
            case chunkSize - 1:
                // Update chunk in front
                ChunkPool.Instance.ActiveChunks[new(position.x, position.y, position.z + 1)].GetComponent<Chunk>().IsDirty = true;
                break;
        }

        IsDirty = true;
    }
    private async Task GenerateMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        await Task.Run(IterateVoxels); // Make sure this processes all voxels

        Mesh mesh = new()
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray(),
            uv = uvs.ToArray()
        };

        mesh.RecalculateNormals(); // Important for lighting

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        meshRenderer.material = TextureManager.material;
        Debug.Log("mesh created");
    }
    public void IterateVoxels()
    {
        Debug.Log("started iterating voxels");
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    ProcessVoxel(x, y, z);
                }
            }
        }
        Debug.Log("finished iterating voxels");
    }
    private void ProcessVoxel(int x, int y, int z)
    {
        // Check if the voxels array is initialized and the indices are within bounds
        if (Voxels == null || x < 0 || x >= Voxels.GetLength(0) ||
            y < 0 || y >= Voxels.GetLength(1) || z < 0 || z >= Voxels.GetLength(2))
        {
            return; // Skip processing if the array is not initialized or indices are out of bounds
        }
        Voxel voxel = Voxels[x, y, z];
        if (voxel.Type != Voxel.Types.Air)
        {
            // Check each face of the voxel for visibility
            bool[] facesVisible = new bool[6];

            // Check visibility for each face
            facesVisible[0] = IsFaceVisible(x, y, z, Vector3Int.up); // Top
            facesVisible[1] = IsFaceVisible(x, y, z, Vector3Int.down); // Bottom
            facesVisible[2] = IsFaceVisible(x, y, z, Vector3Int.left); // Left
            facesVisible[3] = IsFaceVisible(x, y, z, Vector3Int.right); // Right
            facesVisible[4] = IsFaceVisible(x, y, z, Vector3Int.forward); // Front
            facesVisible[5] = IsFaceVisible(x, y, z, Vector3Int.back); // Back

            for (int i = 0; i < facesVisible.Length; i++)
            {
                if (facesVisible[i])
                {
                    AddFace(new(x, y, z), indexToDirection[i]); // Method to add mesh data for the visible face
                }
            }
        }
    }
    private bool IsFaceVisible(int x, int y, int z, Vector3Int direction)
    {
        Vector3Int newPos = new Vector3Int(x, y, z) + direction;
        return newPos.x < 0 || newPos.x >= chunkSize || newPos.y < 0 || newPos.y >= chunkSize || newPos.z < 0 || newPos.z >= chunkSize
            ? IsVoxelHiddenInWorld(position + direction, newPos)
            : !Voxels[newPos.x, newPos.y, newPos.z].IsActive;
    }
    private bool IsVoxelHiddenInWorld(Vector3Int chunkPos, Vector3Int localPos)
    {
        // Handle wraparound logic for the local position within the chunk
        localPos.x = (localPos.x + chunkSize) % chunkSize;
        localPos.y = (localPos.y + chunkSize) % chunkSize;
        localPos.z = (localPos.z + chunkSize) % chunkSize;

        // Check if there is a chunk at the global position
        if (!ChunkPool.Instance.ActiveChunks.TryGetValue(chunkPos, out Chunk neighborChunk))
        {
            return true;
        }

        // If the voxel at this local position is inactive, the face should be visible (not hidden)
        return !neighborChunk.IsVoxelActiveAt(localPos);
    }
    public bool IsVoxelActiveAt(Vector3Int localPosition)
    {
        // Round the local position to get the nearest voxel index
        int x = Mathf.RoundToInt(localPosition.x);
        int y = Mathf.RoundToInt(localPosition.y);
        int z = Mathf.RoundToInt(localPosition.z);

        // Check if the indices are within the bounds of the voxel array
        if (x >= 0 && x < chunkSize && y >= 0 && y < chunkSize && z >= 0 && z < chunkSize)
        {
            // Return the active state of the voxel at these indices
            return Voxels[x, y, z].IsActive;
        }

        // If out of bounds, consider the voxel inactive
        return false;
    }
    private void AddFace(Vector3 position, Vector3 direction)
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
        triangles.Add(vertexCount + 1);
        triangles.Add(vertexCount + 2);
        triangles.Add(vertexCount + 2);
        triangles.Add(vertexCount + 3);
        triangles.Add(vertexCount + 1);
    }

    private static readonly Vector3Int[] indexToDirection = new Vector3Int[]
    {
        Vector3Int.up,      // Top
        Vector3Int.down,    // Bottom
        Vector3Int.left,    // Left
        Vector3Int.right,   // Right
        Vector3Int.forward, // Front
        Vector3Int.back     // Back
    };
    public async Task LoadData(Vector3Int coord)
    {
        await Task.Run(() => SaveSystem.LoadChunk(coord, out Voxels));
    }

    public struct MeshData
    {
        public List<Vector3> Vertices;
        public List<int> Triangles;
        public List<Vector2> UVs;

        public MeshData(int capacity)
        {
            Vertices = new List<Vector3>(capacity);
            Triangles = new List<int>(capacity * 3 / 2);
            UVs = new List<Vector2>(capacity);
        }

        public readonly void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            int index = Vertices.Count;

            Vertices.Add(a);
            Vertices.Add(b);
            Vertices.Add(c);
            Vertices.Add(d);

            Triangles.Add(index + 0);
            Triangles.Add(index + 2);
            Triangles.Add(index + 1);

            Triangles.Add(index + 1);
            Triangles.Add(index + 2);
            Triangles.Add(index + 3);

            UVs.Add(uvA);
            UVs.Add(uvB);
            UVs.Add(uvC);
            UVs.Add(uvD);
        }
    }

    public bool HasMesh => meshFilter != null;

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