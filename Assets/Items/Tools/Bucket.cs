using Items;
using UnityEngine;

public sealed class Bucket : Item, IResource, IPlaceBlock
{
    public override string Name => "Bucket"; // The name of the item
    public override ushort TextureID => 2; // The texture ID for the bucket
    public override string Description => $"A bucket for carrying liquids.\nContains {Value}/{MaxValue} blocks of water"; // Description of the item
    public int Value { get; set; } = 0; // The current value of the bucket
    public int MaxValue { get; set; } = 5; // The maximum value of the bucket
    public Block Block
    {
        get
        {
            Value--;
            return new(3);
        }
    }
}
