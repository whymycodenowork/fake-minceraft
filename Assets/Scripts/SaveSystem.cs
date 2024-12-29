using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Voxels;

public static class SaveSystem
{
    private static List<Type> voxelTypes = new List<Type>();

    // Static constructor to initialize the voxel types
    static SaveSystem()
    {
        CacheVoxelTypes();
    }

    private static void CacheVoxelTypes()
    {
        voxelTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IVoxel).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
    }

    public static void SaveChunkToDisk(string path, int x, int y, IVoxel[,,] voxels)
    {
        if (voxels == null)
        {
            Debug.LogWarning("voxels is null!");
            return;
        }

        using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write x and y
                writer.Write(x);
                writer.Write(y);

                // Write voxel data
                for (int i = 0; i < 16; i++)
                {
                    for (int j = 0; j < 255; j++)
                    {
                        for (int k = 0; k < 16; k++)
                        {
                            IVoxel voxel = voxels[i, j, k];

                            // Save type name
                            writer.Write(voxel.GetType().AssemblyQualifiedName);

                            // Save voxel data
                            if (voxel is  IInteractableVoxel interactableVoxel)
                            {
                                interactableVoxel.SaveToData(writer);
                            }
                        }
                    }
                }
            }
        }
    }

    public static void LoadChunkFromDisk(string path, ref int x, ref int y, ref IVoxel[,,] voxels)
    {
        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
        using (BinaryReader reader = new BinaryReader(stream))
        {
            // Read x and y
            x = reader.ReadInt32();
            y = reader.ReadInt32();

            // Initialize voxel array
            voxels = new IVoxel[16, 255, 16];

            // Read voxel data
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 255; j++)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        // Read the voxel type name
                        string typeName = reader.ReadString();
                        Type voxelType = voxelTypes.FirstOrDefault(t => t.AssemblyQualifiedName == typeName);

                        if (voxelType == null)
                        {
                            // Handle error or default case (e.g., AirVoxel)
                            voxels[i, j, k] = Air.Empty;
                            continue;
                        }

                        IVoxel voxel = (IVoxel)Activator.CreateInstance(voxelType);
                        if (voxel is IInteractableVoxel interactableVoxel)
                        {
                            interactableVoxel.LoadFromData(reader);
                        }
                        voxels[i, j, k] = voxel;
                    }
                }
            }
        }
    }

    public static void SaveChunk(Vector2Int coord, IVoxel[,,] voxels)
    {

    }
}
