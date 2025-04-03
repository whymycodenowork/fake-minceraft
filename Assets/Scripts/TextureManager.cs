using UnityEngine;

public static class TextureManager
{
    public static readonly Material[,] Materials; // Textures for Voxels
    public static readonly Texture2D[,] BlockItemTextures; // Textures for BlockItems
    public static readonly Texture2D[] ItemTextures; // Textures for Items

    // Static constructor (runs once when the class is first accessed)
    static TextureManager()
    {
        // Load all textures in Resources/Textures
        var textures = Resources.LoadAll<Texture2D>("Textures");
        var amountOfImages = textures.Length / 6; // Divide by 6

        Materials = new Material[amountOfImages, 6];
        BlockItemTextures = new Texture2D[amountOfImages, 6];

        for (var id = 0; id < amountOfImages; id++)
        {
            for (var direction = 0; direction < 6; direction++)
            {
                var textureIndex = id * 6 + direction;
                if (textureIndex < textures.Length)
                {
                    var texture = textures[textureIndex];
                    BlockItemTextures[id, direction] = texture;
                    Material material = new(Shader.Find("Standard"))
                    {
                        mainTexture = texture
                    };
                    Materials[id, direction] = material;
                }
                else
                {
                    Debug.LogError($"Texture missing for direction {direction} of block ID {id}");
                }
            }
        }

        // Load all item textures in Resources/ItemImages
        var itemTextures = Resources.LoadAll<Texture2D>("ItemImages");
        ItemTextures = new Texture2D[itemTextures.Length];

        for (var id = 0; id < itemTextures.Length; id++)
        {
            ItemTextures[id] = itemTextures[id];
        }
    }
}