#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureSlicer : EditorWindow
{
    private string sourceFolder = "Assets/Resources/Images";
    private string targetFolder = "Assets/Resources/Textures";

    [MenuItem("Tools/Slice Textures 16x16")]
    public static void ShowWindow()
    {
        GetWindow<TextureSlicer>("Texture Slicer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Slice all textures in a folder (first 16x16 section)", EditorStyles.boldLabel);
        sourceFolder = EditorGUILayout.TextField("Source Folder", sourceFolder);
        targetFolder = EditorGUILayout.TextField("Target Folder", targetFolder);

        if (GUILayout.Button("Slice Textures"))
        {
            SliceTextures();
        }
    }

    private void SliceTextures()
    {
        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogError($"Source folder does not exist: {sourceFolder}");
            return;
        }
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        string[] files = Directory.GetFiles(sourceFolder, "*.png", SearchOption.TopDirectoryOnly);
        foreach (string file in files)
        {
            string assetPath = file.Replace(Application.dataPath, "Assets");
            Texture2D sourceTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (sourceTexture == null)
            {
                Debug.LogWarning($"Could not load texture at {assetPath}");
                continue;
            }

            // Readable check
            string texPath = AssetDatabase.GetAssetPath(sourceTexture);
            TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(texPath);
            if (!importer.isReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            // Slice 16x16 from top-left using SetPixels32
            Color32[] pixels = sourceTexture.GetPixels32();
            Color32[] slicedPixels = new Color32[16 * 16];
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int sourceIndex = (sourceTexture.height - 16 + y) * sourceTexture.width + x;
                    int targetIndex = y * 16 + x;
                    slicedPixels[targetIndex] = pixels[sourceIndex];
                }
            }
            Texture2D sliced = new(16, 16, sourceTexture.format, false);
            sliced.SetPixels32(slicedPixels);
            sliced.Apply();

            // Save as PNG
            byte[] pngData = sliced.EncodeToPNG();
            string fileName = Path.GetFileName(file);
            string savePath = Path.Combine(targetFolder, fileName);
            File.WriteAllBytes(savePath, pngData);
            Debug.Log($"Sliced texture saved: {savePath}");

            // Import the new asset
            AssetDatabase.ImportAsset(savePath.Replace(Application.dataPath, "Assets"));
        }
        AssetDatabase.Refresh();
        Debug.Log("Texture slicing complete.");
    }
}
#endif