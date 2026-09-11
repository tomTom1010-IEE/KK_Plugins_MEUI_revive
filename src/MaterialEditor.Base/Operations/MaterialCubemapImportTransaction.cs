using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>Controller-owned byte storage and leases; no serialization types cross this boundary.</summary>
    internal sealed class MaterialCubemapImportStorage
    {
        internal Func<int> Count;
        internal Func<byte[], int> StoreData;
        internal Action<int, MaterialEditorCubemapLease> StoreLease;
        internal Action<int> RemoveCreated;
        internal Action PurgeLeases;
    }

    internal sealed class MaterialCubemapRecordAccess<T>
    {
        internal Func<T, int?> GetId;
        internal Action<T, int?> SetId;
        internal Func<T, MaterialCubemapOriginalState> Original;
    }

    /// <summary>One transaction for Chara and Studio; candidates stay detached until application succeeds.</summary>
    internal static class MaterialCubemapImportTransaction
    {
        private static void Cleanup(Action action)
        {
            try { action(); }
            catch (Exception ex) { MaterialEditorPluginBase.Logger?.LogWarning("Cubemap cleanup: " + ex.Message); }
        }

        internal static MaterialEditResult Execute<T>(byte[] data, MaterialEditorCubemapContentKey key,
            GameObject root, string materialName, string property, bool logNormalization,
            MaterialCubemapImportStorage storage, IList<T> records, T existing,
            Func<int, T> create, MaterialCubemapRecordAccess<T> access, Func<T, bool> apply) where T : class
        {
            if (data == null || root == null)
                return new MaterialEditResult(MaterialEditStatus.Failed, "Validate");

            MaterialEditorCubemapLease lease = null;
            MaterialTextureSnapshot runtime = null;
            T candidate = null;
            var created = false;
            var committed = false;
            var texId = 0;
            var stage = "Acquire";
            var previousId = existing == null ? null : access.GetId(existing);
            var previousState = existing == null ? null : access.Original(existing).CaptureCheckpoint();
            try
            {
                string warning;
                string error;
                var acquired = key == null
                    ? MaterialEditorCubemapCache.TryAcquire(data, out lease, out warning, out error)
                    : MaterialEditorCubemapCache.TryAcquire(data, key, out lease, out warning, out error);
                if (!acquired) return new MaterialEditResult(MaterialEditStatus.Failed, stage, error);
                if (logNormalization && !string.IsNullOrEmpty(warning))
                    MaterialEditorPluginBase.Logger?.LogWarning(warning);
                stage = "Store";
                var count = storage.Count();
                texId = storage.StoreData(data);
                created = storage.Count() > count;
                storage.StoreLease(texId, lease);
                lease = null;

                stage = "Snapshot";
                runtime = new MaterialTextureSnapshot(root, materialName, property);
                candidate = create(texId);
                // Checkpoints are copy-on-replace state; Synchronize builds new collections.
                if (existing != null) access.Original(candidate).RestoreCheckpoint(previousState, false);
                stage = "Apply";
                if (!apply(candidate))
                    return new MaterialEditResult(MaterialEditStatus.Failed, stage, "Previous override preserved.");
                stage = "Commit";
                if (existing == null) records.Add(candidate);
                else
                {
                    access.SetId(existing, texId);
                    access.Original(existing).RestoreCheckpoint(access.Original(candidate).CaptureCheckpoint(), false);
                }
                committed = true;
                return new MaterialEditResult(MaterialEditStatus.Succeeded, stage);
            }
            catch (Exception ex) { return new MaterialEditResult(MaterialEditStatus.Failed, stage, ex.Message); }
            finally
            {
                if (!committed)
                {
                    runtime?.Restore();
                    if (existing != null)
                    {
                        access.SetId(existing, previousId);
                        access.Original(existing).RestoreCheckpoint(previousState, true);
                    }
                    else if (candidate != null) records.Remove(candidate);
                    if (candidate != null) access.Original(candidate).Clear();
                    if (created) Cleanup(() => storage.RemoveCreated(texId));
                }
                if (lease != null) Cleanup(lease.Dispose);
                Cleanup(storage.PurgeLeases);
            }
        }
    }
}
