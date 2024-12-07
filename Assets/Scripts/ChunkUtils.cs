using System.Collections;
using System.Collections.Generic;
using Voxels;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;

public static class ChunkUtils
{
    [BurstCompile]
    public static bool ShouldAddFace(
    NativeArray<byte> voxels,  // Current chunk voxel data
    int x,
    int y,
    int z,
    Vector3Int direction,
    NativeArray<byte> neighborChunkVoxels  // Neighbor chunk data
    )
    {
        // Calculate the neighbor coordinates
        int newX = x + direction.x;
        int newY = y + direction.y;
        int newZ = z + direction.z;

        // Check Y bounds (vertical limits)
        if (newY >= 255) return true;  // Face at the top
        if (newY < 0) return false;            // Face at the bottom

        // Check if the neighbor is out of bounds in X/Z
        bool isOutOfBounds = newX < 0 || newX >= 16 || newZ < 0 || newZ >= 16;

        if (isOutOfBounds)
        {
            // Adjust to neighbor chunk coordinates
            int neighborX = (newX + 16) % 16;
            int neighborZ = (newZ + 16) % 16;

            // Compute the index for the neighboring voxel
            int neighborIndex = neighborX + neighborZ * 16 + newY * 255 * 16;

            // Check if the neighbor voxel is empty
            return neighborChunkVoxels[neighborIndex] == (byte)0;
        }
        else
        {
            // Compute the index for the local voxel
            int localIndex = newX + newZ * 16 + newY * 255 * 16;

            // Check if the neighbor voxel is empty
            return voxels[localIndex] == (byte)0;
        }
    }

    public static bool CheckNextVoxel(Chunk chunk, int x, int y, int z, Vector3 direction, out Voxel nextVoxel, out Vector3Int nextPosition, out Vector2Int nextChunkPos)
    {

        int newX = x + (int)direction.x;
        int newY = y + (int)direction.y;
        int newZ = z + (int)direction.z;

        if (newY >= 255) {
            nextPosition = Vector3Int.zero;
            nextVoxel = Voxel.Empty;
            nextChunkPos = Vector2Int.zero;
            return false;
        }
        if (newY < 0) {
            nextPosition = Vector3Int.zero;
            nextVoxel = Voxel.Empty;
            nextChunkPos = Vector2Int.zero;
            return false;
        }

        bool isOutOfBounds = newX < 0 || newX >= 16 || newZ < 0 || newZ >= 16;

        if (isOutOfBounds)
        {
            GameObject nextChunk;
            if (ChunkPool.activeChunks.TryGetValue(new Vector2Int(chunk.x + (int)direction.x, chunk.y + (int)direction.z), out nextChunk))
            {
                Chunk nextChunkScript = nextChunk.GetComponent<Chunk>();

                newX = (newX + 16) % 16;
                newZ = (newZ + 16) % 16;
                nextPosition = new Vector3Int(newX, y, newZ);
                Voxel[,,] nextChunkVoxels = nextChunkScript.voxels;
                if (nextChunkVoxels == null)
                {
                    nextPosition = Vector3Int.zero;
                    nextVoxel = Voxel.Empty;
                    nextChunkPos = Vector2Int.zero;
                    return false;
                }
                nextVoxel = nextChunkVoxels[newX, y, newZ];
                Voxel nextChunkVoxel = nextChunkVoxels[newX, y, newZ];
                nextChunkPos = new Vector2Int(nextChunkScript.x, nextChunkScript.y);
                return true;
            }
            nextPosition = Vector3Int.zero;
            nextVoxel = Voxel.Empty;
            nextChunkPos = Vector2Int.zero;
            return false;
        }
        nextPosition = new Vector3Int(newX, newY, newZ);
        nextVoxel = chunk.voxels[newX, newY, newZ];
        nextChunkPos = new Vector2Int(chunk.x, chunk.y);
        return true;
    }
}
