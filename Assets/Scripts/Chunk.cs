using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public Voxel[,,] Voxels;
    public TerrainGenerator terrainGenerator;
    public ChunkPool chunkPool;

    public int x;
    public int y;
    public bool isDirty = false;

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
    }

    private void OnEnable()
    {
        transform.position = new(x * 16, 0, y * 16);

        Voxels ??= terrainGenerator.GenerateTerrain(x, y);
        isDirty = true;
        if (Voxels == null) Debug.LogWarning("ahhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh");
    }

    public async Task CheckMesh()
    {
        try
        {
            if (isDirty)
            {
                if (Voxels == null) return;

                isDirty = false;
                await GenerateMeshThreaded();
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
        MeshData meshData = await Task.Run(() =>
        {
            MeshData data = new()
            {
                Vertices = new(),
                UVs = new(),
                Triangles = new()
            };

            MarchingCubes.GenerateMesh(this, data);

            return data;
        });

        // Back on the main thread: create and assign the mesh.
        Mesh mesh = new()
        {
            vertices = meshData.Vertices.ToArray(),
            uv = meshData.UVs.ToArray(),
            triangles = meshData.Triangles.ToArray()
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        meshRenderer.material = TextureManager.material;
        meshRenderer.enabled = true;
    }

    public void UpdateMeshLocal(int voxelX, int voxelZ)
    {
        Vector2Int neighbor = new();

        switch (voxelX)
        {
            case 0: neighbor.x = -1; break;
            case 15: neighbor.x = 1; break;
        }

        switch (voxelZ)
        {
            case 0: neighbor.y = -1; break;
            case 15: neighbor.y = 1; break;
        }

        if (neighbor != Vector2Int.zero)
        {
            var neighborChunk = chunkPool.ActiveChunks[new(x + neighbor.x, y + neighbor.y)];
            if (neighborChunk.Voxels == null) Debug.LogWarning("ahhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh");
            neighborChunk.isDirty = true;
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

    public bool HasMesh => meshFilter != null;

    public class MeshData
    {
        public List<Vector3> Vertices { get; set; } = new List<Vector3>();
        public List<int> Triangles { get; set; } = new List<int>();
        public List<Vector2> UVs { get; set; } = new List<Vector2>();
    }
}