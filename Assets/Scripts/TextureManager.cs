using UnityEngine;

public static class TextureManager
{
    public static Material material; // Texture atlas for blocks
    public static Texture2D[] ItemTextures; // Textures for Items
    public static Texture2D[] BlockItemTextures; // Textures for Items that place blocks
    public static int atlasSize;

    public static void CreateTextures()
    {
        // Load all textures in Resources/Textures
        Texture2D texture = Resources.Load<Texture2D>("TextureAtlas");
        Texture2D[] textures = Resources.LoadAll<Texture2D>("Images");

        texture.mipMapBias = -1f;

        atlasSize = textures.Length;

        material = new(Shader.Find("Standard"))
        {
            mainTexture = texture
        };


        // Load all item textures in Resources/ItemImages
        Texture2D[] itemTextures = Resources.LoadAll<Texture2D>("ItemImages");
        ItemTextures = new Texture2D[itemTextures.Length];

        for (int id = 0; id < itemTextures.Length; id++)
        {
            ItemTextures[id] = itemTextures[id];
        }

        // Load all item textures in Resources/ItemImages
        Texture2D[] blockItemTextures = Resources.LoadAll<Texture2D>("Textures");
        BlockItemTextures = new Texture2D[blockItemTextures.Length];

        for (int i = 0; i < blockItemTextures.Length; i++)
        {
            BlockItemTextures[i] = blockItemTextures[i];
        }
    }
}