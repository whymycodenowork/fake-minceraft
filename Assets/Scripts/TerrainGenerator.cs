using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public static TerrainGenerator Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [System.Serializable]
    public struct NoiseLayer
    {
        public float scale;
        public float offsetX;
        public float offsetZ;
        public float offsetY;
        public float weight;
        public float heightMultiplier;
    }

    public NoiseLayer[] noiseLayers;

    public void GenerateTerrain(Block[,,] blocks, Vector3Int pos)
    {
        for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
        {
            for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
            {
                float noiseValue = 0f;
                float totalWeight = 0f;
                foreach (var layer in noiseLayers)
                {
                    float noise = Mathf.PerlinNoise(
                        (x + pos.x + layer.offsetX) * layer.scale,
                        (z + pos.z + layer.offsetZ) * layer.scale
                    );
                    noise *= layer.heightMultiplier; // Scale the noise value
                    noiseValue += noise * layer.weight;
                    noiseValue += layer.offsetY; // Add vertical offset
                    totalWeight += layer.weight;
                }
                noiseValue /= totalWeight;
                // Simple height-based terrain generation
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    var worldY = y + (pos.y * Chunk.CHUNK_SIZE);
                    if (worldY < noiseValue)
                    {
                        blocks[x, y, z] = new Block { id = 1 }; // Solid block
                    }
                    else
                    {
                        blocks[x, y, z] = new Block { id = 0 }; // Air block
                    }
                }
            }
        }
    }
}
