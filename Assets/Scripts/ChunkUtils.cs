using UnityEngine;

public static class ChunkUtils
{
    public static bool ShouldAddFace(Chunk chunk, int x, int y, int z, Vector3 direction)
    {
        if (chunk == null)
        {
            //Debug.LogWarning("chunk is null");
            return false;
        }

        var voxel = chunk.Voxels[x, y, z];
        if (voxel.Type == 0) return true;

        var newX = x + (int)direction.x;
        var newY = y + (int)direction.y;
        var newZ = z + (int)direction.z;

        const int maxHeight = 255;
        const int chunkSize = 16;

        switch (newY)
        {
            case >= maxHeight:
                return true;
            case < 0:
                return false;
        }

        var isOutOfBounds = newX < 0 || newX >= chunkSize || newZ < 0 || newZ >= chunkSize;

        if (isOutOfBounds)
        {
            Vector2Int targetChunkCoord = new(chunk.x + (int)direction.x, chunk.y + (int)direction.z);

            if (!chunk.chunkPool.ActiveChunks.TryGetValue(targetChunkCoord, out var nextChunk)) return false;
            newX = (newX + chunkSize) % chunkSize;
            newZ = (newZ + chunkSize) % chunkSize;
            var nextChunkVoxels = nextChunk.Voxels;

            if (nextChunkVoxels == null)
            {
                return false;
            }

            var nextChunkVoxel = nextChunkVoxels[newX, newY, newZ];
            return nextChunkVoxel.Type != 1 || voxel.Type != 1;
        }

        var neighborVoxel = chunk.Voxels[newX, newY, newZ];
        return neighborVoxel.Type != 1 || voxel.Type != 1;
    }

    public static bool CheckNextVoxel(Chunk chunk, int x, int y, int z, Vector3 direction, out Voxel nextVoxel, out Vector3Int nextPosition, out Vector2Int nextChunkPos)
    {

        var newX = x + (int)direction.x;
        var newY = y + (int)direction.y;
        var newZ = z + (int)direction.z;

        switch (newY)
        {
            case >= 255:
            case < 0:
                nextPosition = Vector3Int.zero;
                nextVoxel = new();
                nextChunkPos = Vector2Int.zero;
                return false;
        }

        var isOutOfBounds = newX < 0 || newX >= 16 || newZ < 0 || newZ >= 16;

        if (isOutOfBounds)
        {
            if (chunk.chunkPool.ActiveChunks.TryGetValue(new(chunk.x + (int)direction.x, chunk.y + (int)direction.z), out var nextChunk))
            {
                var nextChunkScript = nextChunk.GetComponent<Chunk>();

                newX = (newX + 16) % 16;
                newZ = (newZ + 16) % 16;
                nextPosition = new(newX, y, newZ);
                var nextChunkVoxels = nextChunkScript.Voxels;
                if (nextChunkVoxels == null)
                {
                    nextPosition = Vector3Int.zero;
                    nextVoxel = new();
                    nextChunkPos = Vector2Int.zero;
                    return false;
                }
                nextVoxel = nextChunkVoxels[newX, y, newZ];
                var nextChunkVoxel = nextChunkVoxels[newX, y, newZ];
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
}
