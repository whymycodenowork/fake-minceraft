#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureSlicer : MonoBehaviour
{
    [MenuItem("Tools/Slice Textures")]
    public static void SliceTextures()
    {
        string imagesFolderPath = "Assets/Images";
        string texturesFolderPath = "Assets/Resources/Textures";

        if (!Directory.Exists(texturesFolderPath))
        {
            Directory.CreateDirectory(texturesFolderPath);
        }

        string[] imagePaths = Directory.GetFiles(imagesFolderPath, "*.png");

        foreach (string imagePath in imagePaths)
        {
            string imageName = Path.GetFileNameWithoutExtension(imagePath);
            Texture2D originalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);

            // Ensure the texture is 128x16
            if (originalTexture.width != 128 || originalTexture.height != 16)
            {
                Debug.LogWarning($"Texture {imageName} is not 128x16. Skipping.");
                continue;
            }

            // Process only the first 6 frames (16x16 each)
            for (int i = 0; i < 6; i++)
            {
                Texture2D slicedTexture = new(16, 16);
                Color[] pixels = originalTexture.GetPixels(i * 16, 0, 16, 16);
                slicedTexture.SetPixels(pixels);
                slicedTexture.Apply();

                byte[] bytes = slicedTexture.EncodeToPNG();
                string outputPath = $"{texturesFolderPath}/{imageName},{i}.png";
                File.WriteAllBytes(outputPath, bytes);
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif