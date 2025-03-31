#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureSlicer : MonoBehaviour
{
    [MenuItem("Tools/Slice Textures")]
    public static void SliceTextures()
    {
        var imagesFolderPath = "Assets/Images";
        var texturesFolderPath = "Assets/Resources/Textures";

        if (!Directory.Exists(texturesFolderPath))
        {
            Directory.CreateDirectory(texturesFolderPath);
        }

        var imagePaths = Directory.GetFiles(imagesFolderPath, "*.png");

        foreach (var imagePath in imagePaths)
        {
            var imageName = Path.GetFileNameWithoutExtension(imagePath);
            var originalTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);

            // Ensure the texture is 128x16
            if (originalTexture.width != 128 || originalTexture.height != 16)
            {
                Debug.LogWarning($"Texture {imageName} is not 128x16. Skipping.");
                continue;
            }

            // Process only the first 6 frames (16x16 each)
            for (var i = 0; i < 6; i++)
            {
                Texture2D slicedTexture = new(16, 16);
                var pixels = originalTexture.GetPixels(i * 16, 0, 16, 16);
                slicedTexture.SetPixels(pixels);
                slicedTexture.Apply();

                var bytes = slicedTexture.EncodeToPNG();
                var outputPath = $"{texturesFolderPath}/{imageName},{i}.png";
                File.WriteAllBytes(outputPath, bytes);
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif