#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class ClearSave
{
    // Path of the folder to be cleared (relative to the Assets directory)
    private static readonly string folderPath = "Assets/SaveData/SaveFile1";

    [MenuItem("Tools/Clear Save Data")]
    private static void ClearFolder()
    {
        var fullPath = folderPath;

        if (Directory.Exists(fullPath))
        {
            DirectoryInfo directory = new(fullPath);

            // Delete all files in the directory
            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }

            // Delete all subdirectories
            foreach (var subDirectory in directory.GetDirectories())
            {
                subDirectory.Delete(true);
            }

            Debug.Log("Save data folder cleared.");
        }
        else
        {
            Debug.LogWarning("Directory does not exist: " + fullPath);
        }
        AssetDatabase.Refresh();
    }
}
#endif
