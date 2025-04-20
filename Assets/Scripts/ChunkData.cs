using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct ChunkData : IComponentData
{
    public int x;
    public int y;
    public bool isDirty;
}
