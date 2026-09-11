using System;
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
    }
}
