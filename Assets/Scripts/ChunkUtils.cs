using UnityEngine;

public static class ChunkUtils
{
    /// <summary>
    /// Gets the voxel and it's relevant information in a certain direction of an element of a chunk's voxel array
    /// </summary>
    /// <param name="chunk"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="direction"></param>
    /// <param name="nextVoxel"></param>
    /// <param name="nextPosition"></param>
    /// <param name="nextChunkPos"></param>
    /// <returns></returns>
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
            if (ChunkPool.Instance.ActiveChunks.TryGetValue(new(chunk.position.x + (int)direction.x, chunk.position.y + (int)direction.z), out Chunk nextChunkScript))
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
                nextChunkPos = new(nextChunkScript.position.x, nextChunkScript.position.y);
                return true;
            }
            nextPosition = Vector3Int.zero;
            nextVoxel = new();
            nextChunkPos = Vector2Int.zero;
            return false;
        }
        if (chunk.Voxels == null)
        {
            Debug.LogWarning("as87dyhoaleghq9o8shfolpashfoaeuhalsxjfghao");
        }
        nextPosition = new(newX, newY, newZ);
        nextVoxel = chunk.Voxels[newX, newY, newZ];
        nextChunkPos = new(chunk.position.x, chunk.position.y);
        return true;
    }
}
