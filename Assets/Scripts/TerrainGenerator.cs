using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public int heightOffset;
    public Vector2 offset;
    public float zoom;

    public Vector2 offset2;
    public float zoom2;
    public float heightMultiplier;

    private const int WIDTH = 16;
    private const int HEIGHT = 255;
    private const int LENGTH = 16;

    public Voxel[,,] GenerateTerrain(int offsetX, int offsetY)
    {
        var terrain = new Voxel[WIDTH, HEIGHT, LENGTH];

        for (var x = 0; x < WIDTH; x++)
        {
            for (var z = 0; z < LENGTH; z++)
            {
                var noiseValue = Mathf.PerlinNoise((offsetX * WIDTH + x + offset.x) * zoom, (offsetY * LENGTH + z + offset.y) * zoom);
                noiseValue *= Mathf.PerlinNoise((offsetX * WIDTH + x + offset2.x) * zoom2, (offsetY * LENGTH + z + offset2.y) * zoom2);
                noiseValue *= Mathf.PerlinNoise((offsetX * WIDTH + x) * 0.001f, (offsetY * LENGTH + z) * 0.0001f);

                // Scale the noiseValue to fit within the height range
                var heightAtPoint = Mathf.RoundToInt((noiseValue * heightMultiplier) + heightOffset);

                // Set voxels based on height
                for (var y = 0; y < HEIGHT; y++)
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
