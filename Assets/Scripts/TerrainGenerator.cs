using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int heightOffset;
    public Vector2 offset;
    public float zoom;

    public Vector2 offset2;
    public float zoom2;
    public float heightMultiplier;

    // singleton
    public static TerrainGenerator Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    public Voxel[,,] GenerateTerrain(int offsetX, int offsetY)
    {
        Voxel[,,] terrain = new Voxel[Chunk.chunkSize, Chunk.chunkSize, Chunk.chunkSize];

        for (int x = 0; x < Chunk.chunkSize; x++)
        {
            for (int z = 0; z < Chunk.chunkSize; z++)
            {

                float noiseValue = Mathf.PerlinNoise(((offsetX * Chunk.chunkSize) + x + offset.x) * zoom, ((offsetY * Chunk.chunkSize) + z + offset.y) * zoom);
                noiseValue *= Mathf.PerlinNoise(((offsetX * Chunk.chunkSize) + x + offset2.x) * zoom2, ((offsetY * Chunk.chunkSize) + z + offset2.y) * zoom2);
                noiseValue *= Mathf.PerlinNoise(((offsetX * Chunk.chunkSize) + x) * 0.001f, ((offsetY * Chunk.chunkSize) + z) * 0.0001f);


                // Scale the noiseValue to fit within the height range
                int heightAtPoint = Mathf.RoundToInt((noiseValue * heightMultiplier) + heightOffset);

                // Set voxels based on height

                for (int y = 0; y < Chunk.chunkSize; y++)
                {

                    if (y == heightAtPoint)
                    {
                        if (heightAtPoint >= 60)
                        {
                            // Create grass at top and if not submerged
                            terrain[x, y, z] = new(Voxel.IDs.Grass, Voxel.Types.Solid);
                        }
                        else
                        {
                            // If submerged, place dirt instead
                            terrain[x, y, z] = new(Voxel.IDs.Dirt, Voxel.Types.Solid);
                        }
                    }
                    else if (y < heightAtPoint && y > heightAtPoint - 6)
                    {
                        // Dirt
                        terrain[x, y, z] = new(Voxel.IDs.Dirt, Voxel.Types.Solid);
                    }
                    else if (y < heightAtPoint - 5)
                    {
                        // Stone
                        terrain[x, y, z] = new(Voxel.IDs.Stone, Voxel.Types.Solid);
                    }

                    else if (y > heightAtPoint && y <= 60)
                    {
                        // Water
                        terrain[x, y, z] = new(Voxel.IDs.Water, Voxel.Types.Liquid);
                    }

                    else
                    {
                        // Air
                        terrain[x, y, z] = new();
                    }
                }
            }
        }

        return terrain;
    }
}
