using System;
using Items;
public partial struct Block
{
    // Packed 64-bit metadata
    public ulong data;

    public Block(ushort id)
    {
        data = id; // sets lower 16 bits
    }

    public Block(ulong data)
    {
        this.data = data;
    }

    public void Break()
    {
        data = 0u;
    }

    // --- Properties to extract each field ---
    public ushort ID
    {
        readonly get => (ushort)(data & 0xFFFF);
        set => data = (data & ~0xFFFFUL) | value;
    }

    public byte Facing
    {
        readonly get => (byte)((data >> 16) & 0b111);
        set => data = (data & ~(0b111UL << 16)) | ((ulong)(value & 0b111) << 16);
    }

    public byte IOFlags
    {
        readonly get => (byte)((data >> 19) & 0b11_1111);
        set => data = (data & ~(0b11_1111UL << 19)) | ((ulong)(value & 0b11_1111) << 19);
    }

    public byte Substance
    {
        readonly get => (byte)((data >> 25) & 0b111);
        set => data = (data & ~(0b111UL << 25)) | ((ulong)(value & 0b111) << 25);
    }

    public float Amount
    {
        readonly get => BitConverter.Int32BitsToSingle((int)((data >> 28) & 0xFFFF_FFFF));
        set
        {
            uint fbits = (uint)BitConverter.SingleToInt32Bits(value);
            data = (data & ~(0xFFFF_FFFFUL << 28)) | ((ulong)fbits << 28);
        }
    }

    // Optionally: Flags field if needed from upper bits (bit 60–63)
    public byte Flags
    {
        readonly get => (byte)((data >> 60) & 0b1111);
        set => data = (data & ~(0b1111UL << 60)) | ((ulong)(value & 0b1111) << 60);
    }

    public static class BlockRegistry
    {
        public static Type[] blockIdToItem =
        {
            typeof(Empty),
            typeof(Dirt),
            typeof(Grass),
        };
    }
}
