#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class TextureAtlasCreator : MonoBehaviour
{
    [MenuItem("Tools/Create Texture Atlas")]
    public static void CreateTextureAtlas()
    {
        var imagesFolderPath = "Assets/Resources/Textures";
        var texturesFolderPath = "Assets/Resources";

        if (!Directory.Exists(texturesFolderPath))
        {
            Directory.CreateDirectory(texturesFolderPath);
        }

        var imagePaths = Directory.GetFiles(imagesFolderPath, "*.png");

        // Determine how many textures we have and the dimensions for the atlas
        int textureCount = imagePaths.Length;
        int atlasHeight = textureCount * 16; // Each texture is 16px tall
        int atlasWidth = 16; // The width will remain 16px (because each texture is 16x16)

        // Create a new texture to hold the atlas
        Texture2D textureAtlas = new(atlasWidth, atlasHeight);

        int yOffset = 0; // Track the Y position in the atlas where the next texture will go

        foreach (var imagePath in imagePaths)
        {
            var imageName = Path.GetFileNameWithoutExtension(imagePath);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);

            if (texture.width != 16 || texture.height != 16)
            {
                Debug.LogWarning($"Texture {imageName} is not 16x16. Skipping.");
                continue;
            }

            // Copy the pixels of the current texture into the atlas at the correct position
            textureAtlas.SetPixels(0, yOffset, texture.width, texture.height, texture.GetPixels());

            // Update the offset to place the next texture beneath the previous one
            yOffset += texture.height;
        }

        // Apply the changes to the atlas
        textureAtlas.Apply();

        // Save the atlas as a PNG file in the Resources/Textures folder
        var atlasPath = $"{texturesFolderPath}/TextureAtlas.png";
        var bytes = textureAtlas.EncodeToPNG();
        File.WriteAllBytes(atlasPath, bytes);

        // Refresh the asset database to make the new atlas visible in the editor
        AssetDatabase.Refresh();

        Debug.Log("Texture Atlas created successfully!");
    }
}
#endif
