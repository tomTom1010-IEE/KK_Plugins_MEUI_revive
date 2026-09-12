using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>Texture2D prepare/apply/commit boundary; animation-specific rollback stays with its controller.</summary>
    internal static class MaterialTextureImportTransaction
    {
        internal static MaterialEditResult Execute<T>(GameObject root, string materialName, string property,
            Func<T> prepare, Func<T, bool> apply, Action<T> commit, Action<T> discard) where T : class
        {
            T candidate = null;
            MaterialTextureSnapshot snapshot = null;
            var committed = false;
            var stage = "Snapshot";
            try
            {
                snapshot = new MaterialTextureSnapshot(root, materialName, property);
                stage = "Prepare texture/animation";
                candidate = prepare();
                stage = "Apply";
                if (!apply(candidate))
                    return new MaterialEditResult(MaterialEditStatus.Failed, stage);
                stage = "Commit";
                commit(candidate);
                committed = true;
                return new MaterialEditResult(MaterialEditStatus.Succeeded, stage);
            }
            catch (Exception ex)
            {
                return new MaterialEditResult(MaterialEditStatus.Failed, stage, ex.Message);
            }
            finally
            {
                if (!committed)
                {
                    snapshot?.Restore();
                    try { discard(candidate); }
                    catch (Exception ex) { MaterialEditorPluginBase.Logger?.LogWarning("Texture cleanup: " + ex.Message); }
                }
            }
        }

        // Keep the existing DTO identity: animation bindings may still reference it.
        internal static void Commit<T, TController, TAnimation>(IList<T> records, IDictionary<T, TController> animations,
            T existing, T candidate, Func<T, int?> getId, Action<T, int?> setId,
            Func<T, TAnimation> getAnimation, Action<T, TAnimation> setAnimation) where T : class
        {
            if (existing == null)
            {
                records.Add(candidate);
                return;
            }
            var previousId = getId(existing);
            var previousAnimation = getAnimation(existing);
            TController previousController;
            TController candidateController;
            var hadPrevious = animations.TryGetValue(existing, out previousController);
            var hasCandidate = animations.TryGetValue(candidate, out candidateController);
            try
            {
                setId(existing, getId(candidate));
                setAnimation(existing, getAnimation(candidate));
                animations.Remove(candidate);
                if (hasCandidate) animations[existing] = candidateController;
                else animations.Remove(existing);
            }
            catch
            {
                setId(existing, previousId);
                setAnimation(existing, previousAnimation);
                animations.Remove(candidate);
                if (hadPrevious) animations[existing] = previousController;
                else animations.Remove(existing);
                throw;
            }
        }
    }

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
