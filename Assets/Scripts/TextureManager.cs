using System.IO;
using UnityEngine;

public static class TextureManager
{
    public static Material[,] materials; // Textures for Voxels
    public static Texture2D[,] blockItemTextures; // Textures for BlockItems
    public static Texture2D[] itemTextures; // Textures for Items

    // Static constructor (runs once when the class is first accessed)
    static TextureManager()
    {
        InitializeTextures();
    }

    // Static method to initialize materials
    private static void InitializeTextures()
    {
        int amountOfImages = Directory.GetFiles("Assets/Images", "*.png").Length;

        materials = new Material[amountOfImages, 6];
        blockItemTextures = new Texture2D[amountOfImages, 6];

        for (int id = 0; id < amountOfImages; id++)
        {
            for (int direction = 0; direction < 6; direction++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Textures/{id},{direction}");
                if (texture != null)
                {
                    blockItemTextures[id, direction] = texture;
                    Material material = new(Shader.Find("Standard"))
                    {
                        mainTexture = texture
                    };
                    materials[id, direction] = material;
                }
                else
                {
                    Debug.LogError($"Texture not found: Textures/{id},{direction}");
                }
            }
        }
        amountOfImages = Directory.GetFiles("Assets/Resources/ItemImages", "*.png").Length;

        itemTextures = new Texture2D[amountOfImages];
        for (int id = 0; id < amountOfImages; id++)
        {
            itemTextures[id] = Resources.Load<Texture2D>($"ItemImages/{id}");
        }
    }
}
