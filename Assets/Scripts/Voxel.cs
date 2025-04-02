public struct Voxel
{
    public byte ID;
    public byte Type;
    public float Health;
    public float Defence;

    public Voxel(byte id = 0, byte type = 0, float defence = 0f)
    {
        this.ID = id;
        this.Type = type;
        Health = 100;
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

    public readonly override string ToString() => $"{ID}, {Type}, {Health}";
}