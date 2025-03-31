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

        var voxel = chunk.voxels[x, y, z];
        if (voxel.type == 0) return true;

        var newX = x + (int)direction.x;
        var newY = y + (int)direction.y;
        var newZ = z + (int)direction.z;

        const int MaxHeight = 255;
        const int ChunkSize = 16;

        if (newY >= MaxHeight) return true;
        if (newY < 0) return false;

        var isOutOfBounds = newX < 0 || newX >= ChunkSize || newZ < 0 || newZ >= ChunkSize;

        if (isOutOfBounds)
        {
            Vector2Int targetChunkCoord = new(chunk.x + (int)direction.x, chunk.y + (int)direction.z);

            if (chunk.chunkPool.activeChunks.TryGetValue(targetChunkCoord, out var nextChunk))
            {
                newX = (newX + ChunkSize) % ChunkSize;
                newZ = (newZ + ChunkSize) % ChunkSize;
                var nextChunkVoxels = nextChunk.GetComponent<Chunk>().voxels;

                if (nextChunkVoxels == null)
                {
                    //Debug.LogWarning("Next chunk voxels are null");
                    return false;
                }

                var nextChunkVoxel = nextChunkVoxels[newX, newY, newZ];
                if (nextChunkVoxel.type == 1 && voxel.type == 1) return false;

                return true;
            }
            return false;
        }

        var neighborVoxel = chunk.voxels[newX, newY, newZ];
        if (neighborVoxel.type == 1 && voxel.type == 1) return false;

        return true;
    }

    public static bool CheckNextVoxel(Chunk chunk, int x, int y, int z, Vector3 direction, out Voxel nextVoxel, out Vector3Int nextPosition, out Vector2Int nextChunkPos)
    {

        var newX = x + (int)direction.x;
        var newY = y + (int)direction.y;
        var newZ = z + (int)direction.z;

        if (newY >= 255)
        {
            nextPosition = Vector3Int.zero;
            nextVoxel = new Voxel();
            nextChunkPos = Vector2Int.zero;
            return false;
        }
        if (newY < 0)
        {
            nextPosition = Vector3Int.zero;
            nextVoxel = new Voxel();
            nextChunkPos = Vector2Int.zero;
            return false;
        }

        var isOutOfBounds = newX < 0 || newX >= 16 || newZ < 0 || newZ >= 16;

        if (isOutOfBounds)
        {
            GameObject nextChunk;
            if (chunk.chunkPool.activeChunks.TryGetValue(new Vector2Int(chunk.x + (int)direction.x, chunk.y + (int)direction.z), out nextChunk))
            {
                var nextChunkScript = nextChunk.GetComponent<Chunk>();

                newX = (newX + 16) % 16;
                newZ = (newZ + 16) % 16;
                nextPosition = new Vector3Int(newX, y, newZ);
                var nextChunkVoxels = nextChunkScript.voxels;
                if (nextChunkVoxels == null)
                {
                    nextPosition = Vector3Int.zero;
                    nextVoxel = new Voxel();
                    nextChunkPos = Vector2Int.zero;
                    return false;
                }
                nextVoxel = nextChunkVoxels[newX, y, newZ];
                var nextChunkVoxel = nextChunkVoxels[newX, y, newZ];
                nextChunkPos = new Vector2Int(nextChunkScript.x, nextChunkScript.y);
                return true;
            }
            nextPosition = Vector3Int.zero;
            nextVoxel = new Voxel();
            nextChunkPos = Vector2Int.zero;
            return false;
        }
        nextPosition = new Vector3Int(newX, newY, newZ);
        nextVoxel = chunk.voxels[newX, newY, newZ];
        nextChunkPos = new Vector2Int(chunk.x, chunk.y);
        return true;
    }
}
