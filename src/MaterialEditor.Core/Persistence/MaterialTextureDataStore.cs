using System.Collections.Generic;

namespace KK_Plugins.MaterialEditor
{
    /// <summary>Shared byte deduplication. Preserves first equal payload and max-existing-ID + 1 allocation.</summary>
    internal static class MaterialTextureDataStore
    {
        internal static int GetOrAdd(Dictionary<int, TextureContainer> textures, byte[] data)
        {
            int highestId = 0;
            foreach (var texture in textures)
                if (texture.Value.Data.SequenceEqualFast(data))
                    return texture.Key;
                else if (texture.Key > highestId)
                    highestId = texture.Key;

            highestId++;
            textures.Add(highestId, TextureSaveHandler.CreateTextureContainer(data));
            return highestId;
        }
    }
}
