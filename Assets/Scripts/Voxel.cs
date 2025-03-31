using System.Collections.Generic;

public struct Voxel
{
    public byte id;
    public byte type;
    public float health;
    public float defence;

    public Voxel(byte id = 0, byte type = 0, float defence = 0f)
    {
        this.id = id;
        this.type = type;
        this.health = 100;
        this.defence = defence;
    }
    public void Destroy()
    {
        // Set itself to air
        id = 0;
        type = 0;
    }

    public void DealDamage(float amount)
    {
        var actualAmount = amount;
        actualAmount /= defence;
        health -= actualAmount;
        if (health <= 0) Destroy();
    }

    public readonly override string ToString() => $"{id}, {type}, {health}";
}