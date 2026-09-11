using System;
using System.Collections.Generic;

namespace MaterialEditorAPI
{
    /// <summary>Runtime-only exact key. Lists remain the persistence authority and retain duplicate ordering.</summary>
    internal struct MaterialPropertyRecordKey
    {
        private readonly int _kind;
        private readonly int _coordinate;
        private readonly int _slotOrId;
        private readonly string _material;
        private readonly string _property;

        internal MaterialPropertyRecordKey(int kind, int coordinate, int slotOrId, string material, string property)
        {
            _kind = kind;
            _coordinate = coordinate;
            _slotOrId = slotOrId;
            _material = material;
            _property = property;
        }

        internal bool Matches(MaterialPropertyRecordKey other) =>
            _kind == other._kind && _coordinate == other._coordinate && _slotOrId == other._slotOrId
            && _material == other._material && _property == other._property;
    }

    /// <summary>Stateless typed adapter: no cache to invalidate on external edits, reload, rename or copy.</summary>
    internal sealed class MaterialPropertyRecordQuery<T> where T : class
    {
        private readonly Func<T, MaterialPropertyRecordKey> _keyOf;
        internal MaterialPropertyRecordQuery(Func<T, MaterialPropertyRecordKey> keyOf) { _keyOf = keyOf; }

        internal T First(IList<T> records, MaterialPropertyRecordKey key)
        {
            for (var i = 0; i < records.Count; i++)
                if (_keyOf(records[i]).Matches(key)) return records[i];
            return null;
        }

        internal int RemoveAll(List<T> records, MaterialPropertyRecordKey key, Predicate<T> additional = null) =>
            records.RemoveAll(record => _keyOf(record).Matches(key) && (additional == null || additional(record)));
    }
}
