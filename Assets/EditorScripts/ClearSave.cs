#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

public class ClearSave : MonoBehaviour
{
    [MenuItem("Tools/Clear Saved Chunk Data")]
    public static void ClearSaveData()
    {
        string saveFolderPath = Path.Combine(Application.persistentDataPath, "SaveFile1");
        if (Directory.Exists(saveFolderPath))
        {
            Directory.Delete(saveFolderPath, true);
            Debug.Log("Save data cleared successfully.");
        }
    }
}
#endif