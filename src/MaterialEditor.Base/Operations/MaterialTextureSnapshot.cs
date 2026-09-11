using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>Rollback values for every runtime material touched by SetTexture, including projectors.</summary>
    internal sealed class MaterialTextureSnapshot
    {
        private readonly Dictionary<Material, Texture> _values = new Dictionary<Material, Texture>();
        private readonly string _property;

        internal MaterialTextureSnapshot(GameObject root, string name, string property)
        {
            _property = "_" + property;
            foreach (var material in MaterialAPI.GetObjectMaterials(root, name))
                if (material != null && material.HasProperty(_property))
                    _values[material] = material.GetTexture(_property);
        }

        internal void Restore()
        {
            foreach (var entry in _values)
            {
                try
                {
                    if (entry.Key != null && entry.Key.HasProperty(_property))
                        entry.Key.SetTexture(_property, entry.Value);
                }
                catch (Exception ex) { MaterialEditorPluginBase.Logger?.LogWarning("Texture rollback: " + ex.Message); }
            }
        }
    }
}
