using UnityEngine;

public static class ChunkUtils
{
    /// <summary>
    ///      Gets the voxel and it's relevant information in a certain direction of an element of a chunk's voxel array
    /// </summary>
    public static bool CheckNextVoxel(Chunk chunk, int x, int y, int z, Vector3 direction, out Voxel nextVoxel, out Vector3Int nextPosition, out Vector2Int nextChunkPos)
    {
        int newX = x + (int)direction.x;
        int newY = y + (int)direction.y;
        int newZ = z + (int)direction.z;

        switch (newY)
        {
            case >= 255:
            case < 0:
                nextPosition = Vector3Int.zero;
                nextVoxel = new();
                nextChunkPos = Vector2Int.zero;
                return false;
        }

        bool isOutOfBounds = newX < 0 || newX >= 16 || newZ < 0 || newZ >= 16;

        if (isOutOfBounds)
        {
            if (chunk.chunkPool.ActiveChunks.TryGetValue(new(chunk.x + (int)direction.x, chunk.y + (int)direction.z), out Chunk nextChunkScript))
            {
                newX = (newX + 16) % 16;
                newZ = (newZ + 16) % 16;
                nextPosition = new(newX, y, newZ);
                Voxel[,,] nextChunkVoxels = nextChunkScript.Voxels;
                if (nextChunkVoxels == null)
                {
                    nextPosition = Vector3Int.zero;
                    nextVoxel = new();
                    nextChunkPos = Vector2Int.zero;
                    return false;
                }
                nextVoxel = nextChunkVoxels[newX, y, newZ];
                nextChunkPos = new(nextChunkScript.x, nextChunkScript.y);
                return true;
            }
            nextPosition = Vector3Int.zero;
            nextVoxel = new();
            nextChunkPos = Vector2Int.zero;
            return false;
        }

        nextPosition = new(newX, newY, newZ);
        nextVoxel = chunk.Voxels[newX, newY, newZ];
        nextChunkPos = new(chunk.x, chunk.y);
        return true;
    }

    // Converts world position to local voxel coordinates within a chunk.
    public static Vector3Int WorldToVoxel(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x) % 16;
        int y = Mathf.FloorToInt(worldPosition.y);
        int z = Mathf.FloorToInt(worldPosition.z) % 16;

        if (x < 0)
        {
            x += 16;
        }

        if (z < 0)
        {
            z += 16;
        }

        return new Vector3Int(x, y, z);
    }

    // Converts world position to chunk coordinates.
    public static Vector2Int WorldToChunk(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / 16);
        int y = Mathf.FloorToInt(worldPosition.z / 16); // Using z for chunk grid instead of y to align with world space
        return new Vector2Int(x, y);
    }
}
