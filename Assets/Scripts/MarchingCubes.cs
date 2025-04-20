using UnityEngine;
using static Chunk;

public static class MarchingCubes
{
    private const float heightThreshold = 0.5f;

    public static void GenerateMesh(Chunk chunk, MeshData meshData)
    {
        meshData.Vertices.Clear();
        meshData.Triangles.Clear();
        meshData.UVs.Clear();

        for (int x = 0; x < Chunk.chunkSize; x++)
        {
            for (int y = 0; y < Chunk.chunkSize; y++)
            {
                for (int z = 0; z < Chunk.chunkSize; z++)
                {
                    float[] cubeCorners = new float[8];

                    for (int i = 0; i < 8; i++)
                    {
                        _ = ChunkUtils.CheckNextVoxel(chunk, x, y, z, MarchingTable.Corners[i], out Voxel nextVoxel, out _, out _);
                        cubeCorners[i] = nextVoxel.Health; // (nextVoxel.Type != Voxel.Types.Air) ? 0f : 1f;
                    }

                    MarchCube(new Vector3(x, y, z), cubeCorners, meshData);
                }
            }
        }
    }

    private static void MarchCube(Vector3 position, float[] cubeCorners, MeshData meshData)
    {
        int configIndex = GetConfigIndex(cubeCorners);

        if (configIndex is 0 or 255)
        {
            return;
        }

        int edgeIndex = 0;
        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex];

                if (triTableValue == -1)
                {
                    return;
                }

                Vector3 edgeStart = position + MarchingTable.Edges[triTableValue, 0];
                Vector3 edgeEnd = position + MarchingTable.Edges[triTableValue, 1];

                Vector3 vertex = (edgeStart + edgeEnd) / 2;

                meshData.Vertices.Add(vertex);
                meshData.Triangles.Add(meshData.Vertices.Count - 1);

                edgeIndex++;
            }
        }
    }
    private static int GetConfigIndex(float[] cubeCorners)
    {
        int configIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (cubeCorners[i] > heightThreshold)
            {
                configIndex |= 1 << i;
            }
        }
        return configIndex;
    }
}
