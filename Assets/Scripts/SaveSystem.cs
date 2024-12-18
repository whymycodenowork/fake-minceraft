using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Voxels;
using UnityEngine;

public static class SaveSystem
{
    private static List<Type> voxelTypes = new List<Type>();
    static SaveSystem()
    {
        CacheVoxelTypes();
    }

    private static void CacheVoxelTypes()
    {
        voxelTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(Voxel).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
    }

    public static void SaveChunk(string path, int x, int y, Voxel[,,] voxels)
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
                            Voxel voxel = voxels[i, j, k];

                            // Save type name
                            writer.Write(voxel.GetType().AssemblyQualifiedName);

                            // Save voxel data
                            if (voxel is InteractableVoxel interactableVoxel)
                            {
                                interactableVoxel.SaveToData(writer);
                            }
                        }
                    }
                }
            }
        }
    }

    public static void LoadChunk(string path, ref int x, ref int y, ref Voxel[,,] voxels)
    {
        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            // Read x and y
            x = reader.ReadInt32();
            y = reader.ReadInt32();

            // Initialize voxel array
            voxels = new Voxel[16, 255, 16];

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
                            voxels[i, j, k] = Voxel.Empty;
                            continue;
                        }

                        Voxel voxel = (Voxel)Activator.CreateInstance(voxelType);
                        if (voxel is InteractableVoxel interactableVoxel)
                        {
                            interactableVoxel.LoadFromData(reader);
                        }
                        voxels[i, j, k] = voxel;
                    }
                }
            }
        }
    }
}
