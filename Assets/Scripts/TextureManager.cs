using System.IO;
using UnityEngine;

public class TextureManager : MonoBehaviour
{
    public static Material[,] materials;
    void Awake()
    {
        int amountOfImages = Directory.GetFiles("Assets/Images", "*.png").Length;
        materials = new Material[amountOfImages, 6];

        for (int id = 0; id < amountOfImages; id++)
        {
            for (int direction = 0; direction < 6; direction++)
            {
                Texture2D texture = Resources.Load<Texture2D>($"Textures/{id},{direction}");
                Material material = new Material(Shader.Find("Standard"));
                material.mainTexture = texture;
                materials[id, direction] = material;
            }
        }
    }
}
