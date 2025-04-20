public struct Voxel
{
    public IDs ID { get; set; }
    public Types Type { get; set; }
    public float Health { get; set; }
    public float Defence { get; set; }

    public readonly bool IsActive => Type != Types.Air;

    public Voxel(IDs id = IDs.Air, Types type = Types.Air, float defence = 0f, float health = 1f)
    {
        this.ID = id;
        this.Type = type;
        Health = health;
        this.Defence = defence;
    }
    public void Destroy()
    {
        // Set itself to air
        ID = 0;
        Type = 0;
    }

    public void DealDamage(float amount)
    {
        var actualAmount = amount;
        actualAmount /= Defence;
        Health -= actualAmount;
        if (Health <= 0) Destroy();
    }

    public enum Types
    {
        Air,
        Solid,
        Liquid,
    }

    public enum IDs
    {
        Air,
        Dirt,
        Grass,
        Water,
        Stone,
        WoodLog,
        Cobblestone,
        WoodPlanks
    }

    public readonly override string ToString() => $"{ID}, {Type}, {Health}";
}