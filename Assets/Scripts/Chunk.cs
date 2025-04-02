using System;
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

    public const int HEIGHT = 255;
    public const int WIDTH = 16;
    public const int LENGTH = 16;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        _materials = TextureManager.materials;
        _ = StartLoop();
    }

    private void OnEnable()
    {
        transform.position = new(x * 16, 0, y * 16);

        Voxels ??= terrainGenerator.GenerateTerrain(x, y);
        
        isDirty = true;
    }

    private async Task StartLoop()
    {
        try
        {
            while (Application.isPlaying)
            {
                if (isDirty)
                {
                    isDirty = false;
                    await GenerateMeshThreaded();
                }

                await Task.Yield(); // Prevent CPU overload
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    // ReSharper disable Unity.PerformanceAnalysis
    private async Task GenerateMeshThreaded()
    {
        // Run heavy mesh data generation off the main thread.
        var meshData = await Task.Run(() =>
        {
            var data = new MeshData
            {
                Vertices = new(),
                Uvs = new(),
                TextureToTriangles = new()
            };
    
            // Initialize dictionary for each texture id and face direction.
            for (var id = 0; id < _materials.GetLength(0); id++)
            {
                for (var direction = 0; direction < 6; direction++)
                {
                    data.TextureToTriangles[(id, direction)] = new();
                }
            }
            // Build the mesh data.
            for (var chunkX = 0; chunkX < 16; chunkX++)
            {
                for (var chunkY = 0; chunkY < 255; chunkY++)
                {
                    for (var chunkZ = 0; chunkZ < 16; chunkZ++)
                    {
                        var voxel = Voxels[chunkX, chunkY, chunkZ];
                        if (voxel.Type == 0) continue; // Skip empty voxel
    
                        for (var i = 0; i < 6; i++) // Loop through each face
                        {
                            var direction = indexToDirection[i];
                            if (!ChunkUtils.ShouldAddFace(this, chunkX, chunkY, chunkZ, direction)) continue;
                            var vertexCount = data.Vertices.Count;
                            // Select face vertices based on direction.
                            var faceVertices = direction switch
                            {
                                _ when direction == Vector3.up => faceVerticesUp,
                                _ when direction == Vector3.down => faceVerticesDown,
                                _ when direction == Vector3.right => faceVerticesRight,
                                _ when direction == Vector3.left => faceVerticesLeft,
                                _ when direction == Vector3.forward => faceVerticesForward,
                                _ => faceVerticesBack,
                            };
    
                            for (var j = 0; j < 4; j++)
                            {
                                data.Vertices.Add(new Vector3(chunkX, chunkY, chunkZ) + faceVertices[j]);
                                data.Uvs.Add(faceUVs[j]);
                            }
    
                            // Add two triangles for the face.
                            data.TextureToTriangles[(voxel.ID, i)].Add(vertexCount);
                            data.TextureToTriangles[(voxel.ID, i)].Add(vertexCount + 2);
                            data.TextureToTriangles[(voxel.ID, i)].Add(vertexCount + 1);
                            data.TextureToTriangles[(voxel.ID, i)].Add(vertexCount + 1);
                            data.TextureToTriangles[(voxel.ID, i)].Add(vertexCount + 2);
                            data.TextureToTriangles[(voxel.ID, i)].Add(vertexCount + 3);
                        }
                    }
                }
            }
            return data;
        });
    
        // Back on the main thread: create and assign the mesh.
        var mesh = new Mesh
        {
            vertices = meshData.Vertices.ToArray(),
            uv = meshData.Uvs.ToArray(),
            subMeshCount = meshData.TextureToTriangles.Count
        };
    
        var submeshIndex = 0;
        foreach (var triangles in meshData.TextureToTriangles.Values)
        {
            mesh.SetTriangles(triangles, submeshIndex++);
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    
        // Assign materials (this remains unchanged)
        List<Material> meshMaterials = new();
        for (var type = 0; type < _materials.GetLength(0); type++)
        {
            for (var direction = 0; direction < 6; direction++)
            {
                meshMaterials.Add(_materials[type, direction]);
            }
        }
        meshRenderer.materials = meshMaterials.ToArray();
        meshRenderer.enabled = true;
    }

    public void UpdateNeighborMeshes(int voxelX, int voxelZ)
    {
        switch (voxelX)
        {
            case 0:
                // Update chunk on the left
                chunkPool.ActiveChunks[new(x - 1, y)].GetComponent<Chunk>().isDirty = true;
                break;
            case 15:
                // Update chunk on the right
                chunkPool.ActiveChunks[new(x + 1, y)].GetComponent<Chunk>().isDirty = true;
                break;
        }

        switch (voxelZ)
        {
            case 0:
                // Update chunk below
                chunkPool.ActiveChunks[new(x, y - 1)].GetComponent<Chunk>().isDirty = true;
                break;
            case 15:
                // Update chunk above
                chunkPool.ActiveChunks[new(x, y + 1)].GetComponent<Chunk>().isDirty = true;
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
    
    private struct MeshData
    {
        public List<Vector3> Vertices;
        public List<Vector2> Uvs;
        public Dictionary<(int, int), List<int>> TextureToTriangles;
    }
}