using System;

public struct Voxel
{
    public byte id;
    public byte type;
    public float health;

    public Voxel(byte id = 0, byte type = 0)
    {
        this.id = id;
        this.type = type;
        this.health = 100;
    }
    public void Destroy()
    {
        id = 0;
        type = 0;
    }

    public void DealDamage(float amount)
    {
        health -= amount;
        if (health < 0) Destroy();
    }

    public readonly override string ToString() => $"{id}, {type}, {health}";
}