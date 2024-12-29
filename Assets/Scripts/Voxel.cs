using System;
using System.IO;

namespace Voxels
{
    // Base interface for all voxels
    public interface IVoxel
    {
        bool Exists { get; }
        byte TextureID { get; }
        void Remove();
    }

    public interface IBreakableVoxel : IVoxel
    {
        float Health { get; }
        float Hardness { get; }
        byte BreakingTool { get; }
        bool NeedsBreakingTool { get; }
        int DropAmount { get; }
        void DealDamage(float amount, byte breakingTool);
    }

    public interface IFluidVoxel : IVoxel
    {
        float Height { get; set; }
        float SpreadFactor { get; }
    }

    public interface IInteractableVoxel : IBreakableVoxel
    {
        void SaveToData(BinaryWriter writer);
        void LoadFromData(BinaryReader reader);
    }

    public struct AirVoxel : IVoxel
    {
        public readonly bool Exists => false;
        public readonly byte TextureID => 0;

        public readonly void Remove() { }
    }

    public struct BreakableVoxel : IBreakableVoxel
    {
        private float health;

        public bool Exists { get; private set; }
        public float Health => health;
        public float Hardness { get; init; }
        public byte BreakingTool { get; init; }
        public bool NeedsBreakingTool { get; init; }
        public int DropAmount { get; init; }
        public byte TextureID { get; init; }

        public BreakableVoxel(byte textureID, float hardness, byte breakingTool, bool needsBreakingTool, int dropAmount = 1)
        {
            health = 100;
            Exists = true;
            TextureID = textureID;
            Hardness = hardness;
            BreakingTool = breakingTool;
            NeedsBreakingTool = needsBreakingTool;
            DropAmount = dropAmount;
        }

        public void Remove() => Exists = false;

        public void DealDamage(float amount, byte breakingTool)
        {
            amount /= Hardness;
            if (!(BreakingTool == 0 || BreakingTool == 3) && BreakingTool != breakingTool)
                amount /= 4;

            health -= amount;

            if (health <= 0)
            {
                Remove();
                if (breakingTool == BreakingTool && NeedsBreakingTool)
                {
                    // DropItem logic goes here
                }
            }
        }
    }

    public struct FluidVoxel : IFluidVoxel
    {
        public bool Exists { get; private set; }
        public byte TextureID { get; init; }
        public float Height { get; set; }
        public float SpreadFactor { get; init; }

        public FluidVoxel(byte textureID, float spreadFactor)
        {
            Exists = true;
            TextureID = textureID;
            Height = 100f;
            SpreadFactor = spreadFactor;
        }

        public void Remove() => Exists = false;
    }

    public static class Air
    {
        public static AirVoxel Empty => new();
    }

    namespace Solid
    {
        public static class SolidVoxels
        {
            public static BreakableVoxel Dirt => new(1, 0.7f, 3, false);
            public static BreakableVoxel Grass => new(2, 1.2f, 3, false);
            public static BreakableVoxel Stone => new(4, 1f, 1, true);
            public static BreakableVoxel WoodLog => new(5, 3f, 2, false);
            public static BreakableVoxel Cobblestone => new(6, 0.85f, 1, true);
        }
    }

    namespace Fluid
    {
        public static class FluidVoxels
        {
            public static FluidVoxel Water => new(3, 2f);
        }
    }
}

namespace System.Runtime.CompilerServices
{
    public sealed class IsExternalInit { }
}
