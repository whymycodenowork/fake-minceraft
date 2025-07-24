using Items;

public sealed class Grass : Item, IBlockTexture
{
    public override string Name => "Grass Block"; // The name of the item
    public override ushort TextureID => 2;
    public Block Block => new(2);
    public override string Description => "A block of grass"; // Description of the item
    public Grass() : base(1) { } // Default constructor
    public Grass(int c) : base(c) { } // Constructor with count parameter
}
