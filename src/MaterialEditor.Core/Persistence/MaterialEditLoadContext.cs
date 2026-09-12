using System;
using System.Collections.Generic;
using ExtensibleSaveFormat;
using MessagePack;

namespace MaterialEditorAPI
{
    /// <summary>One game persistence scope. Owns source, ID mapping and per-record diagnostics.</summary>
    internal sealed class MaterialEditLoadContext
    {
        internal readonly PluginData Source;
        internal readonly Dictionary<int, int> TextureIds;

        internal MaterialEditLoadContext(PluginData source, Dictionary<int, int> textureIds)
        {
            Source = source;
            TextureIds = textureIds;
        }

        internal static Dictionary<int, int> ImportTextures<T>(IEnumerable<KeyValuePair<int, T>> source,
            Func<T, byte[]> readBytes, Func<byte[], int> store)
        {
            var ids = new Dictionary<int, int>();
            ApplyRecords("TextureDictionary", source, entry => ids[entry.Key] = store(readBytes(entry.Value)));
            return ids;
        }

        internal int? RemapTexture(int? id)
        {
            if (!id.HasValue) return null;
            int mapped;
            if (TextureIds.TryGetValue(id.Value, out mapped)) return mapped;
            throw new InvalidOperationException("Missing imported texture ID " + id.Value);
        }

        internal void Read<T>(string family, Action<T> accept)
        {
            object bytes;
            if (Source?.data == null || !Source.data.TryGetValue(family, out bytes) || bytes == null) return;
            List<T> records;
            try { records = MessagePackSerializer.Deserialize<List<T>>((byte[])bytes); }
            catch (Exception ex) { Report(family, ex); return; }
            ApplyRecords(family, records, accept);
        }

        internal static void ApplyRecords<T>(string family, IEnumerable<T> records, Action<T> accept)
        {
            if (records == null) return;
            foreach (var record in records)
            {
                try { accept(record); }
                catch (Exception ex) { Report(family, ex); }
            }
        }

        private static void Report(string family, Exception error) =>
            MaterialEditorPluginBase.Logger?.LogWarning("Skipped invalid " + family + " record: " + error.Message);
    }
}
