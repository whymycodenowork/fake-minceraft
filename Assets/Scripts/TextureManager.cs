using UnityEditorInternal;
using UnityEngine;

public static class TextureManager
{
    public static Material material; // Texture atlas for blocks
    public static Texture2D[] ItemTextures; // Textures for Items
    public static int atlasSize;

    public static void CreateTextures()
    {
        // Load all textures in Resources/Textures
        var texture = Resources.Load<Texture2D>("TextureAtlas");
        var textures = Resources.LoadAll<Texture2D>("Images");

        texture.mipMapBias = -1f;

        atlasSize = textures.Length;

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