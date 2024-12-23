using System.IO;
using UnityEngine;

public static class TextureManager
{
    public static Material[,] materials;

    // Static constructor (runs once when the class is first accessed)
    static TextureManager()
    {
        InitializeMaterials();
    }

    // Static method to initialize materials
    private static void InitializeMaterials()
    {
        int amountOfImages = Directory.GetFiles("Assets/Images", "*.png").Length;

        materials = new Material[amountOfImages, 6];

        for (int id = 0; id < amountOfImages; id++)
        {
            for (int direction = 0; direction < 6; direction++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Textures/{id},{direction}");
                if (texture != null)
                {
                    Material material = new Material(Shader.Find("Standard"))
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
    }
}
