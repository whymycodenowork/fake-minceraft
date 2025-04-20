namespace Items
{
    // Base class for items
    public abstract class Item
    {
        public abstract string Name { get; }
        public abstract string ToolTip { get; }
        public virtual int MaxStackSize { get => 64; }
        public abstract byte TextureID { get; }
        public int Amount;

        // Method for combining two stacks
        public virtual Item Add(Item item)
        {
            var newAmount = Amount + item.Amount;

            if (newAmount > MaxStackSize)
            {
                var excess = newAmount - MaxStackSize;
                Amount = MaxStackSize;
                item.Amount = excess;
                return item; // Return the excess
            }
            Amount = newAmount;
            return new Air();
        }

        // Must return itself or another item
        public virtual Item UseLeft()
        {
            return this;
        }
        public virtual Item UseRight()
        {
            return this;
        }
    }

    // Class for no items
    public sealed class Air : Item
    {
        public override string Name => "";
        public override string ToolTip => "";
        public override int MaxStackSize => 1;
        public override byte TextureID => 0;
    }

    // Item used to place blocks
    public abstract class BlockItem : Item
    {
        public abstract Voxel BlockToPlace { get; }
        public override byte TextureID => 0; // Uses the associated block's texture

        public override Item UseRight()
        {
            Amount--;
            if (Amount == 0) return new Air();
            return this;
        }
    }

    // Tools
    public abstract class ToolItem : Item
    {
        public override int MaxStackSize => 1; // Tools are not stackable
        public abstract float Damage { get; }
        public abstract float Knockback { get; }
        public abstract float UseSpeed { get; }
        public abstract float MiningSpeed { get; }
        public abstract byte ToolType { get; }
        public abstract int MaxDurability { get; }

        public int Durability;

        public override Item UseLeft()
        {
            Durability--;
            return this;
        }
    }

    // The different types of tools
    public enum ToolTypes
    {
        None,
        Pickaxe,
        Shovel,
        Axe,
    }
    public abstract class PickaxeItem : ToolItem
    {
        public override byte ToolType => (byte)ToolTypes.Pickaxe;
    }

    namespace MiscItems
    {
        public sealed class WaterBucket : Item
        {
            public override string Name => "Water Bucket";
            public override string ToolTip => "A bucket of water";
            public override int MaxStackSize => 1;
            public override byte TextureID => 2;
        }
    }

    namespace BlockItems
    {
        public sealed class Dirt : BlockItem
        {
            public override string Name => "Dirt Block";
            public override string ToolTip => "A block of dirt";
            public override Voxel BlockToPlace => new(Voxel.IDs.Dirt, Voxel.Types.Solid);
        }
        public sealed class Grass : BlockItem
        {
            public override string Name => "Grass Block";
            public override string ToolTip => "A grass block";
            public override Voxel BlockToPlace => new(Voxel.IDs.Grass, Voxel.Types.Solid);
        }
        public sealed class Stone : BlockItem
        {
            public override string Name => "Stone Block";
            public override string ToolTip => "A block of stone";
            public override Voxel BlockToPlace => new(Voxel.IDs.Stone, Voxel.Types.Solid);
        }
        public sealed class WoodLog : BlockItem
        {
            public override string Name => "Wood Log";
            public override string ToolTip => "A wood log";
            public override Voxel BlockToPlace => new(Voxel.IDs.WoodLog, Voxel.Types.Solid);
        }
        public sealed class Cobblestone : BlockItem
        {
            public override string Name => "Cobblestone";
            public override string ToolTip => "A collection of small stones packed tightly";
            public override Voxel BlockToPlace => new(Voxel.IDs.Cobblestone, Voxel.Types.Solid);
        }
        public sealed class WoodPlanks : BlockItem
        {
            public override string Name => "Wood Planks";
            public override string ToolTip => "A stack of wooden planks";
            public override Voxel BlockToPlace => new(Voxel.IDs.WoodPlanks, Voxel.Types.Solid);
        }
    }

    namespace ToolItems
    {
        public sealed class WoodPickaxe : PickaxeItem
        {
            public override string Name => "Wooden Pickaxe";
            public override string ToolTip => "A basic pickaxe made of wood";
            public override float Damage => 2.5f;
            public override float Knockback => 0.75f;
            public override float UseSpeed => 1f;
            public override float MiningSpeed => 1f;
            public override int MaxDurability => 64;
            public override byte TextureID => 1;
        }
    }
}