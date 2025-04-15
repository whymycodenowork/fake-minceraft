using System.Collections.Generic;
using UnityEngine;
using static Chunk;

public static class MarchingCubes
{
    private const float heightThreshold = 0.5f;
    private const int atlasSizeInTiles = 8;

    public static void GenerateMesh(Chunk chunk, MeshData meshData)
    {
        meshData.Vertices.Clear();
        meshData.Triangles.Clear();
        meshData.UVs.Clear();

        var voxels = chunk.Voxels;

        for (int x = 0; x < Chunk.WIDTH; x++)
        {
            for (int y = 0; y < Chunk.HEIGHT - 1; y++)
            {
                for (int z = 0; z < Chunk.LENGTH - 1; z++)
                {
                    float[] cubeCorners = new float[8];

                    for (int i = 0; i < 8; i++)
                    {
                        // Default to Air if out-of-bounds and no neighbor is available

                        if (ChunkUtils.CheckNextVoxel(chunk, x, y, z, MarchingTable.Corners[i], out Voxel cornerVoxel, out _, out _))
                        {
                            cubeCorners[i] = (cornerVoxel.Type == Voxel.Types.Solid) ? 1f : 0f;
                        }
                        else
                        {
                            cubeCorners[i] = 0f;
                        }
                    }

                    MarchCube(new Vector3(x, y, z), cubeCorners, voxels[x, y, z], meshData);
                }
            }
        }
    }

    private static void MarchCube(Vector3 position, float[] cubeCorners, Voxel centerVoxel, MeshData meshData)
    {
        int configIndex = GetConfigIndex(cubeCorners);
        if (configIndex == 0 || configIndex == 255) return;

        // Get texture ID (row index in atlas)
        int textureIndex = (int)centerVoxel.ID % 16;
        float uvHeight = 1f / atlasSizeInTiles;
        float uvYMin = textureIndex * uvHeight;
        float uvYMax = uvYMin + uvHeight;

        List<int> tempVerts = new();

        int edgeIndex = 0;
        for (int t = 0; t < 5; t++)
        {
            for (int v = 0; v < 3; v++)
            {
                int triTableValue = MarchingTable.Triangles[configIndex, edgeIndex];
                if (triTableValue == -1) return;

                Vector3 edgeStart = position + MarchingTable.Edges[triTableValue, 0];
                Vector3 edgeEnd = position + MarchingTable.Edges[triTableValue, 1];
                Vector3 vertex = (edgeStart + edgeEnd) / 2f;

                meshData.Vertices.Add(vertex);
                int vertIndex = meshData.Vertices.Count - 1;
                tempVerts.Add(vertIndex);

                Vector2 uv = new(0.5f, (uvYMin + uvYMax) / 2f);
                meshData.UVs.Add(uv);

                edgeIndex++;
            }

            // Add triangle with corrected winding (flip order: 0, 2, 1)
            if (tempVerts.Count >= 3)
            {
                meshData.Triangles.Add(tempVerts[0]);
                meshData.Triangles.Add(tempVerts[2]);
                meshData.Triangles.Add(tempVerts[1]);
            }

            tempVerts.Clear();
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
