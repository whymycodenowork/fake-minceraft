using Items;

public sealed class Dirt : Item, IBlockTexture
{
    public override string Name => "Dirt Block"; // The name of the item
    public override ushort TextureID => 1;
    public Block Block => new(1);
    public override string Description => "A block of dirt."; // Description of the item
    public Dirt() : base(1) { } // Default constructor
    public Dirt(int c) : base(c) { } // Constructor with count parameter
}
