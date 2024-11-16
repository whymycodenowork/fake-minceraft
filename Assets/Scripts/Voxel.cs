using System;
using System.IO;

namespace Voxels
{
    public abstract class Voxel
    {
        // Temporary flag for setting to AirVoxel
        public bool exists = true;

        public virtual void Remove()
        {
            exists = false;
        }

        public static readonly AirVoxel Empty = new AirVoxel();

        public abstract byte TextureID { get; }

        public override string ToString()
        {
            return $"Voxel exists: {exists}, TextureID: {TextureID}, Type: {this.GetType()}";
        }
    }

    public class AirVoxel : Voxel
    {
        public override void Remove() { } // Override remove to prevent tomfoolery
        public override byte TextureID => 0;
        public AirVoxel() { }
    }

    public abstract class BreakableVoxel : Voxel
    {
        protected float health;
        public float Health => health;
        public abstract float Hardness { get; }
        public abstract byte BreakingTool { get; }
        public abstract bool NeedsBreakingTool { get; }
        public virtual int DropAmount { get; } = 1;
        // public virtual Item DroppedItem { get; } = Items.Air;

        public BreakableVoxel()
        {
            health = 100;
        }

        public void DealDamage(float amount, byte breakingTool)
        {
            amount /= Hardness;
            if (!(BreakingTool == 0 || BreakingTool == 3) && BreakingTool != breakingTool) amount /= 4;
            health -= amount;
            if (health <= 0)
            {
                Remove(); 
                if (breakingTool == BreakingTool && NeedsBreakingTool) { /* DropItem(DroppedItem, DropAmount) add later*/ }
            }
        }
    }

    public abstract class SolidVoxel : BreakableVoxel
    {
        
    }

    public abstract class TransparentVoxel : BreakableVoxel
    {

    }

    public abstract class FluidVoxel : Voxel
    {
        public float height = 100f;
        public abstract float SpreadFactor { get; }
    }

    public abstract class InteractableVoxel : BreakableVoxel
    {
        public abstract void SaveToData(BinaryWriter writer);

        public abstract void LoadFromData(BinaryReader reader);
    }

    namespace Solid
    {
        public class Dirt : SolidVoxel
        {
            public override byte TextureID => 1;
            public override float Hardness => 0.7f;
            public override byte BreakingTool => 3;
            public override bool NeedsBreakingTool => false;
        }

        public class Grass: SolidVoxel
        {
            public override byte TextureID => 2;
            public override float Hardness => 1.2f;
            public override byte BreakingTool => 3;
            public override bool NeedsBreakingTool => false;
        }

        public class Stone : SolidVoxel
        {
            public override byte TextureID => 4; // Skip one because id 3 is water 
            public override float Hardness => 1f;
            public override byte BreakingTool => 1;
            public override bool NeedsBreakingTool => true;
        }

        public class WoodLog : SolidVoxel
        {
            public override byte TextureID => 5;
            public override float Hardness => 3;
            public override byte BreakingTool => 2;
            public override bool NeedsBreakingTool => false;
        }

        public class Cobblestone : SolidVoxel
        {
            public override byte TextureID => 6;
            public override float Hardness => 0.85f;
            public override byte BreakingTool => 1;
            public override bool NeedsBreakingTool => true;
        }
    }

    namespace Fluid
    {
        public class Water : FluidVoxel
        {
            public override byte TextureID => 3;
            public override float SpreadFactor => 2f;
        }
    }
}