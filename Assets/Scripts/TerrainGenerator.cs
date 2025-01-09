using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int heightOffset;
    public Vector2 offset;
    public float zoom;

    public int heightOffset2;
    public Vector2 offset2;
    public float zoom2;
    public float heightMultiplier;

    private const int width = 16;
    private const int height = 255;
    private const int length = 16;

    public Voxel[,,] GenerateTerrain(int offsetX, int offsetY)
    {
        Voxel[,,] terrain = new Voxel[width, height, length];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < length; z++)
            {
                float noiseValue = Mathf.PerlinNoise((offsetX * width + x + offset.x) * zoom, (offsetY * length + z + offset.y) * zoom);
                noiseValue *= Mathf.PerlinNoise((offsetX * width + x + offset2.x) * zoom2, (offsetY * length + z + offset2.y) * zoom2);
                noiseValue *= Mathf.PerlinNoise((offsetX * width + x) * 0.001f, (offsetY * length + z) * 0.0001f);

                // Scale the noiseValue to fit within the height range
                int heightAtPoint = Mathf.RoundToInt((noiseValue * heightMultiplier) + heightOffset);

                // Set voxels based on height
                for (int y = 0; y < height; y++)
                {
                    if (y == heightAtPoint)
                    {
                        if (heightAtPoint >= 60)
                        {
                            // Create grass at top and if not submerged
                            terrain[x, y, z] = new Voxel(2, 1);
                        }
                        else
                        {
                            // If submerged, place dirt instead
                            terrain[x, y, z] = new Voxel(1, 1);
                        }
                    }
                    else if (y < heightAtPoint && y > heightAtPoint - 6)
                    {
                        // Dirt
                        terrain[x, y, z] = new Voxel(1, 1);
                    }
                    else if (y < heightAtPoint - 5)
                    {
                        // Stone
                        terrain[x, y, z] = new Voxel(4, 1);
                    }

                    else if (y > heightAtPoint && y <= 60)
                    {
                        // Water
                        terrain[x, y, z] = new Voxel(3, 2);
                    }

                    else
                    {
                        // Air
                        terrain[x, y, z] = new Voxel();
                    }
                }
            }
        }

        return terrain;
    }
}
