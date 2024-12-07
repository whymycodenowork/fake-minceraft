using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using UnityEngine.Rendering;
using Voxels;

public class Chunk : MonoBehaviour
{
    public Voxel[,,] voxels = null;
    public TerrainGenerator terrainGenerator;

    public static Material[,] materials; // Stores materials in a 2D array

    public int x;
    public int y;
    public bool isDirty;

    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public MeshRenderer meshRenderer;

    private static readonly Vector3Int[] directions = {
        Vector3Int.back, Vector3Int.right, Vector3Int.forward, Vector3Int.left, Vector3Int.up, Vector3Int.down
    };

    private static readonly Vector3[] faceVertices = {
        new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f),
        new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f),
        new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
        new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f),
        new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f),
        new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f)
    };

    private static readonly Vector2[] faceUVs = {
        new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 0), new Vector2(1, 0)
    };

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void OnEnable()
    {
        transform.position = new Vector3(x * 16, 0, y * 16);
        voxels ??= terrainGenerator.GenerateTerrain(x, y);
        CreateMesh();
    }

    public void CreateMesh() => isDirty = true;

    public void UpdateMeshLocal(int x, int y, int z) => CreateMesh();

    void Update()
    {
        if (isDirty)
        {
            StartCoroutine(GenerateMeshDataOverTime());
            isDirty = false;
        }
    }

    IEnumerator GenerateMeshDataOverTime()
    {
        NativeArray<byte> textureIDs = FlattenArray(voxels);

        // Using TempJob allocator for the NativeArrays
        NativeArray<Vector3Int> directionsArray = new NativeArray<Vector3Int>(directions, Allocator.TempJob);
        NativeArray<Vector3> faceVerticesArray = new NativeArray<Vector3>(faceVertices, Allocator.TempJob);
        NativeArray<Vector2> faceUVsArray = new NativeArray<Vector2>(faceUVs, Allocator.TempJob);

        // Mesh generation job setup
        MeshGenerationJob meshGenerationJob = new MeshGenerationJob
        {
            textureIDs = textureIDs,
            directions = directionsArray,
            faceVertices = faceVerticesArray,
            faceUVs = faceUVsArray,
            vertices = new NativeList<Vector3>(Allocator.TempJob),
            triangles = new NativeList<int>(Allocator.TempJob),
            uvs = new NativeList<Vector2>(Allocator.TempJob),
            chunkSize = 16,
            chunkHeight = 255
        };

        JobHandle jobHandle = meshGenerationJob.Schedule();
        jobHandle.Complete();

        // Create the mesh with the generated data
        Mesh mesh = new Mesh();

        // Convert NativeList to arrays and set mesh data
        var verticesArray = meshGenerationJob.vertices.ToArray(Allocator.TempJob);
        mesh.SetVertices(verticesArray);
        verticesArray.Dispose();

        var trianglesArray = meshGenerationJob.triangles.ToArray(Allocator.TempJob);
        mesh.SetTriangles(trianglesArray.ToArray(), 0);
        trianglesArray.Dispose();

        var uvsArray = meshGenerationJob.uvs.AsArray();
        mesh.SetUVs(0, uvsArray);
        uvsArray.Dispose();

        // Apply the mesh to the mesh filter and collider
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

        // Clean up all temporary allocations
        textureIDs.Dispose();
        directionsArray.Dispose();
        faceVerticesArray.Dispose();
        faceUVsArray.Dispose();

        // Ensure NativeList allocations are disposed of in the MeshGenerationJob
        meshGenerationJob.vertices.Dispose();
        meshGenerationJob.triangles.Dispose();
        meshGenerationJob.uvs.Dispose();

        yield return null;
    }

    public NativeArray<byte> FlattenArray(Voxel[,,] voxelArray)
    {
        const int width = 16, height = 255, depth = 16;
        NativeArray<byte> nativeArray = new NativeArray<byte>(width * height * depth, Allocator.TempJob);

        int index = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    nativeArray[index++] = voxelArray[x, y, z].TextureID;
                }
            }
        }
        return nativeArray;
    }

    public void LoadData(string path) => SaveSystem.LoadChunk(path, ref x, ref y, ref voxels);
}

[BurstCompile]
public struct MeshGenerationJob : IJob
{
    [ReadOnly] public NativeArray<byte> textureIDs;
    [ReadOnly] public NativeArray<Vector3Int> directions;
    [ReadOnly] public NativeArray<Vector3> faceVertices;
    [ReadOnly] public NativeArray<Vector2> faceUVs;

    public NativeList<Vector3> vertices;
    public NativeList<int> triangles;
    public NativeList<Vector2> uvs;

    public int chunkSize;
    public int chunkHeight;

    public void Execute()
    {
        NativeArray<int> addTriangles = new NativeArray<int>(6, Allocator.TempJob);
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkHeight; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int index = x + y * chunkSize + z * chunkSize * chunkHeight;
                    byte textureID = textureIDs[index];

                    if (textureID == 0) continue;

                    for (int i = 0; i < 6; i++)
                    {
                        int startIndex = vertices.Length;

                        for (int j = 0; j < 4; j++)
                        {
                            vertices.Add(new Vector3(x, y, z) + faceVertices[i * 4 + j]);
                        }

                        uvs.AddRange(faceUVs);
                        addTriangles[0] = startIndex;
                        addTriangles[1] = startIndex + 2;
                        addTriangles[2] = startIndex + 1;
                        addTriangles[3] = startIndex + 1;
                        addTriangles[4] = startIndex + 2;
                        addTriangles[5] = startIndex + 3;
                        triangles.AddRange(addTriangles);
                    }
                }
            }
        }
        addTriangles.Dispose();
    }

    public void Dispose()
    {
        vertices.Dispose();
        triangles.Dispose();
        uvs.Dispose();
    }
}
