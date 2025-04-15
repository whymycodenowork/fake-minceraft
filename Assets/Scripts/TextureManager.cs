using UnityEngine;

public static class TextureManager
{
    public static readonly Material material; // Textures for Voxels
    public static readonly Texture2D[] BlockItemTextures; // Textures for BlockItems
    public static readonly Texture2D[] ItemTextures; // Textures for Items

    // Static constructor (runs once when the class is first accessed)
    static TextureManager()
    {
        // Load all textures in Resources/Textures
        var texture = Resources.Load<Texture2D>("TextureAtlas");
        BlockItemTextures = Resources.LoadAll<Texture2D>("Textures");

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