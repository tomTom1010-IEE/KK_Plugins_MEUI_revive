using System.Collections.Generic;
using ExtensibleSaveFormat;
using MessagePack;

namespace KK_Plugins.MaterialEditor
{
    /// <summary>Preserves explicit legacy keys, concrete List serialization and present-but-null empty families.</summary>
    internal static class MaterialEditRecordSerialization
    {
        internal static void Write<T>(PluginData data, string key, List<T> records) =>
            data.data.Add(key, records.Count > 0 ? MessagePackSerializer.Serialize(records) : null);
    }
}
