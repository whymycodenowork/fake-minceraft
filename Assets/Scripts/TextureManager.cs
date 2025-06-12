using UnityEditorInternal;
using UnityEngine;

public static class TextureManager
{
    public static readonly Material material; // Texture atlas for blocks
    public static readonly Texture2D[] BlockItemTextures; // Textures for BlockItems (does not exist yet)
    public static readonly Texture2D[] ItemTextures; // Textures for Items
    public static readonly int atlasSize;

    static TextureManager()
    {
        // Load all textures in Resources/Textures
        var texture = Resources.Load<Texture2D>("TextureAtlas");
        BlockItemTextures = Resources.LoadAll<Texture2D>("Textures");

        atlasSize = BlockItemTextures.Length;

        material = new(Shader.Find("Standard"))
        {
            mainTexture = texture
        };


        // Load all item textures in Resources/ItemImages
        var itemTextures = Resources.LoadAll<Texture2D>("ItemImages");
        ItemTextures = new Texture2D[itemTextures.Length];

        for (var id = 0; id < itemTextures.Length; id++)
        {
            ItemTextures[id] = itemTextures[id];
        }
    }
}