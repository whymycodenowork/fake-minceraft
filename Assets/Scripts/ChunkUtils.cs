using System.Collections;
using System.Collections.Generic;
using Voxels;
using UnityEngine;

public static class ChunkUtils
{
    public static bool ShouldAddFace(Chunk chunk, int x, int y, int z, Vector3 direction)
    {
        if (chunk == null)
        {
            Debug.LogWarning("chunk is null");
            return false;
        }
        Voxel voxel = chunk.voxels[x, y, z];
        if (voxel == null)
        {
            Debug.LogWarning("voxel is null");
            return false;
        }
        if (voxel == Voxel.Empty) return false;

        int newX = x + (int)direction.x;
        int newY = y + (int)direction.y;
        int newZ = z + (int)direction.z;

        if (newY >= 255) return true;
        if (newY < 0) return false;

        bool isOutOfBounds = newX < 0 || newX >= 16 || newZ < 0 || newZ >= 16;

        if (isOutOfBounds)
        {
            GameObject nextChunk;
            if (chunk.chunkPool.activeChunks.TryGetValue(new Vector2Int(chunk.x + (int)direction.x, chunk.y + (int)direction.z), out nextChunk))
            {
                if (nextChunk == null)
                {
                    Debug.LogWarning("other chunk is null");
                    return false;
                }
                if (newX < 0) newX = 15;
                if (newX >= 16) newX = 0;
                if (newZ < 0) newZ = 15;
                if (newZ >= 16) newZ = 0;

                Voxel nextChunkVoxel = nextChunk.GetComponent<Chunk>().voxels[newX, y, newZ];
                if (nextChunkVoxel == null)
                {
                    Debug.LogWarning("voxel in the other chunk is null");
                    return false;
                }
                //bug
                if (nextChunkVoxel is SolidVoxel && voxel is SolidVoxel) return false;
                return true;
            }
            return false;
        }
        if (chunk.voxels[newX, newY, newZ] == null)
        {
            Debug.Log("other voxel is null");
            return false;
        }
        if (chunk.voxels[newX, newY, newZ].GetType() == voxel.GetType()) return false;
        return true;
    }

    public static bool CheckNextVoxel(Chunk chunk, int x, int y, int z, Vector3 direction, out Voxel nextVoxel, out Vector3Int nextPosition, out Vector2Int nextChunkPos)
    {

        int newX = x + (int)direction.x;
        int newY = y + (int)direction.y;
        int newZ = z + (int)direction.z;

        if (newY >= 255) {
            nextPosition = new Vector3Int(0, 0, 0);
            nextVoxel = Voxel.Empty;
            nextChunkPos = new Vector2Int(0, 0);
            return false;
        }
        if (newY < 0) {
            nextPosition = new Vector3Int(0, 0, 0);
            nextVoxel = Voxel.Empty;
            nextChunkPos = new Vector2Int(0, 0);
            return false;
        }

        bool isOutOfBounds = newX < 0 || newX >= 16 || newZ < 0 || newZ >= 16;

        if (isOutOfBounds)
        {
            GameObject nextChunk;
            if (chunk.chunkPool.activeChunks.TryGetValue(new Vector2Int(chunk.x + (int)direction.x, chunk.y + (int)direction.z), out nextChunk))
            {
                Chunk nextChunkScript = nextChunk.GetComponent<Chunk>();

                if (newX < 0) newX = 15;
                if (newX >= 16) newX = 0;
                if (newZ < 0) newZ = 15;
                if (newZ >= 16) newZ = 0;
                nextPosition = new Vector3Int(newX, newY, newZ);
                nextVoxel = nextChunkScript.voxels[newX, y, newZ];
                nextChunkPos = new Vector2Int(nextChunkScript.x, nextChunkScript.y);
                return true;
            }
            nextPosition = new Vector3Int(0, 0, 0);
            nextVoxel = Voxel.Empty;
            nextChunkPos = new Vector2Int(0, 0);
            return false;
        }
        nextPosition = new Vector3Int(newX, newY, newZ);
        nextVoxel = chunk.voxels[newX, newY, newZ];
        nextChunkPos = new Vector2Int(chunk.x, chunk.y);
        return true;
    }
}
