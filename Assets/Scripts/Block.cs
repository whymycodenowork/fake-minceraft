public struct Block
{
    /// <summary>
    /// the block id
    /// </summary>
    public int id;

    public Block(int id)
    {
        this.id = id;
    }

    public void Break()
    {
        id = 0;
    }
}
