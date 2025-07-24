using System;
using UnityEngine;

namespace Items
{
    /// <summary>
    /// Base class for items
    /// </summary>
    public abstract class Item
    {
        public virtual string Name => GetType().FullName; // Default to the class name if no name is specified
        public int count;
        public virtual int MaxCount => this is IResource ? 1 : 64;
        public virtual string Description => ""; // No description by default
        public abstract ushort TextureID { get; } // Texture ID for the item
        public bool Favorited = false;
        public Item(int c = 1)
        {
            count = c;
        }
        public override string ToString()
        {
            return $"Item \"{Name}\" with {count} amount";
        }
        public virtual void UseLeft() { }
        public virtual void UseRight() { }
    }

    public class Empty : Item
    {
        public override string Name => "";
        public override ushort TextureID => 0; // No texture for empty item

        public static Empty Instance = new(); // Singleton instance of None item

        public Empty() : base(1) { }
    }

    public interface IResource // Base interface for item values
    {
        int Value { get; set; }
        int MaxValue { get; set; }

        void Decrease(int amount)
        {
            Value -= amount;
            if (Value < 0)
            {
                Value = 0;
            }
        }
        void Increase(int amount)
        {
            Value += amount;
            if (Value > MaxValue)
            {
                Value = MaxValue;
            }
        }
    }

    /// <summary>
    /// Interface for items that place blocks but are not blocks themselves (for example a water bucket)
    /// </summary>
    public interface IPlaceBlock
    {
        Block Block { get; } // The block to place
    }

    /// <summary>
    /// Interface for items that render as a block texture in the inventory.
    /// </summary>
    /// <remarks>Items that render as blocks in the inventory must implement this interface to ensure proper rendering.</remarks>
    public interface IBlockTexture : IPlaceBlock { } // Only to differentiate block items from regular items

    public interface IDamage
    {
        float Damage { get; set; }
        float ArmorPenetration { get; set; }
    }

    namespace Tools
    {
        // No tools yet
    }

    [Serializable]
    public sealed class DebugItem : Item
    {
        public override string Name => "Debug Item";
        public override ushort TextureID => 1; // Example texture ID for debug item
        public override string Description => "This is a debug item for testing purposes.";
        
        public override void UseLeft()
        {
            Debug.Log("Using Debug Item with left click.");
        }
        public override void UseRight()
        {
            Debug.Log("Using Debug Item with right click.");
        }
    }

    public sealed class DebugBlock : Item, IBlockTexture
    {
        public override string Name => "Debug Block";
        public override ushort TextureID => 0; // Example texture ID for debug block
        public Block Block => new(1); // Example block to place
        public DebugBlock() : base(1) { }
        public DebugBlock(int c) : base(c) { }
        public override void UseLeft()
        {
            Debug.Log("Using Debug Block with left click.");
        }
        
        public override void UseRight()
        {
            Debug.Log("Using Debug Block with right click.");
        }
    }
}