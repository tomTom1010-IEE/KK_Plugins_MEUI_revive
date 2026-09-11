using MaterialEditorAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorCharaController
    {
        /// <summary>
        /// Import and persist a native Cubemap from an equirectangular PNG or Radiance HDR file.
        /// </summary>
        public void SetMaterialCubemapFromFile(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            string filePath,
            GameObject go)
        {
            if (!File.Exists(filePath))
                return;

            string fileError;
            if (!MaterialEditorCubemapProjection.TryValidateSourceFileLength(
                    new FileInfo(filePath).Length,
                    out fileError))
            {
                MaterialEditorPlugin.Logger.LogMessage(fileError);
                return;
            }

            SetMaterialCubemap(
                slot,
                objectType,
                material,
                propertyName,
                File.ReadAllBytes(filePath),
                null,
                go,
                true);
        }

        /// <summary>
        /// Import and persist a native Cubemap from encoded equirectangular PNG or Radiance HDR data.
        /// </summary>
        public void SetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            byte[] data,
            GameObject go)
        {
            MaterialEditRequestQueue.CancelTarget(go, material == null ? null : material.NameFormatted(), propertyName);
            SetMaterialCubemap(
                slot,
                objectType,
                material,
                propertyName,
                data,
                null,
                go,
                false);
        }

        /// <summary>
        /// Internal warm-cache persistence path. The opaque key belongs to the
        /// exact byte array and avoids a second main-thread SHA-256 pass.
        /// </summary>
        internal bool SetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            byte[] data,
            MaterialEditorCubemapContentKey contentKey,
            GameObject go)
        {
            return SetMaterialCubemap(
                slot,
                objectType,
                material,
                propertyName,
                data,
                contentKey,
                go,
                false);
        }

        private bool SetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            byte[] data,
            MaterialEditorCubemapContentKey contentKey,
            GameObject go,
            bool logNormalizationWarning)
        {
            if (data == null)
                return false;

            MaterialEditorCubemapLease lease = null;
            var storedLease = false;
            var textureEntryCreated = false;
            var texID = 0;
            MaterialCubemapProperty cubemapProperty = null;
            var propertyAdded = false;
            int? previousTexID = null;
            MaterialCubemapOriginalState.Checkpoint previousOriginalState = null;
            Dictionary<Material, Cubemap> previousAppliedValues = null;
            string materialName = null;
            try
            {
                string warning;
                string error;
                var acquired = contentKey == null
                    ? MaterialEditorCubemapCache.TryAcquire(
                        data,
                        out lease,
                        out warning,
                        out error)
                    : MaterialEditorCubemapCache.TryAcquire(
                        data,
                        contentKey,
                        out lease,
                        out warning,
                        out error);
                if (!acquired)
                {
                    MaterialEditorPlugin.Logger.LogMessage(error);
                    return false;
                }
                if (logNormalizationWarning && !string.IsNullOrEmpty(warning))
                    MaterialEditorPlugin.Logger.LogWarning(warning);

                var textureCountBefore = TextureDictionary.Count;
                texID = SetAndGetTextureID(data);
                textureEntryCreated = TextureDictionary.Count > textureCountBefore;
                CubemapLeases.Store(texID, lease);
                // Store consumes ownership both when it adds the lease and when
                // an existing texture ID makes the new reference redundant.
                lease = null;
                storedLease = true;

                materialName = material.NameFormatted();
                previousAppliedValues =
                    MaterialCubemapOriginalSnapshot.SynchronizeByMaterialReference(
                        go,
                        materialName,
                        propertyName,
                        null);
                cubemapProperty = FindMaterialCubemapProperty(
                    slot,
                    objectType,
                    material,
                    propertyName);
                if (cubemapProperty == null)
                {
                    cubemapProperty = new MaterialCubemapProperty(
                        objectType,
                        GetCoordinateIndex(objectType),
                        slot,
                        materialName,
                        propertyName,
                        texID);
                    MaterialCubemapPropertyList.Add(cubemapProperty);
                    propertyAdded = true;
                }
                else
                {
                    previousTexID = cubemapProperty.TexID;
                    previousOriginalState =
                        cubemapProperty.CubemapOriginalState.CaptureCheckpoint();
                    cubemapProperty.TexID = texID;
                }

                if (SetCubemapWithProperty(go, cubemapProperty))
                    return true;

                RollbackMaterialCubemapSet(
                    go,
                    materialName,
                    propertyName,
                    cubemapProperty,
                    propertyAdded,
                    previousTexID,
                    previousOriginalState,
                    previousAppliedValues,
                    texID,
                    textureEntryCreated);
                textureEntryCreated = false;
                MaterialEditorPluginBase.Logger.LogWarning(
                    "Could not apply Cubemap " + materialName + "/" + propertyName
                    + "; the previous override was preserved.");
                return false;
            }
            catch (Exception exception)
            {
                RollbackMaterialCubemapSet(
                    go,
                    materialName,
                    propertyName,
                    cubemapProperty,
                    propertyAdded,
                    previousTexID,
                    previousOriginalState,
                    previousAppliedValues,
                    texID,
                    textureEntryCreated);
                textureEntryCreated = false;
                MaterialEditorPluginBase.Logger.LogWarning(
                    "Could not apply Cubemap; the previous override was preserved. "
                    + exception.Message);
                return false;
            }
            finally
            {
                if (lease != null)
                    lease.Dispose();
                if (storedLease)
                    PurgeUnusedCubemapLeases();
            }
        }

        private void RollbackMaterialCubemapSet(
            GameObject gameObject,
            string materialName,
            string propertyName,
            MaterialCubemapProperty cubemapProperty,
            bool propertyAdded,
            int? previousTexID,
            MaterialCubemapOriginalState.Checkpoint previousOriginalState,
            Dictionary<Material, Cubemap> previousAppliedValues,
            int texID,
            bool textureEntryCreated)
        {
            try
            {
                MaterialCubemapOriginalSnapshot.RestoreByMaterialReference(
                    gameObject,
                    materialName,
                    propertyName,
                    previousAppliedValues);
            }
            catch (Exception exception)
            {
                MaterialEditorPluginBase.Logger.LogWarning(
                    "Could not fully restore the previous Cubemap material value. "
                    + exception.Message);
            }

            if (cubemapProperty != null)
            {
                if (propertyAdded)
                {
                    MaterialCubemapPropertyList.Remove(cubemapProperty);
                    cubemapProperty.ClearCubemapOriginalSnapshot();
                }
                else
                {
                    cubemapProperty.TexID = previousTexID;
                    cubemapProperty.CubemapOriginalState.RestoreCheckpoint(
                        previousOriginalState,
                        true);
                }
            }

            if (textureEntryCreated)
            {
                CubemapLeases.Release(texID);
                TextureContainer container;
                if (TextureDictionary.TryGetValue(texID, out container))
                {
                    try
                    {
                        if (container != null)
                            container.Dispose();
                    }
                    finally
                    {
                        TextureDictionary.Remove(texID);
                    }
                }
            }
            PurgeUnusedCubemapLeases();
        }

        /// <summary>
        /// Get the persisted native Cubemap value, or null when no override exists.
        /// </summary>
        public Cubemap GetMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            GameObject go)
        {
            var cubemapProperty = FindMaterialCubemapProperty(
                slot,
                objectType,
                material,
                propertyName);
            if (cubemapProperty == null || !cubemapProperty.TexID.HasValue)
                return null;

            Cubemap cubemap;
            string error;
            return TryGetCubemap(cubemapProperty.TexID.Value, out cubemap, out error)
                ? cubemap
                : null;
        }

        /// <summary>
        /// Get whether the Cubemap property is still in its original state.
        /// </summary>
        public bool GetMaterialCubemapOriginal(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            GameObject go)
        {
            return FindMaterialCubemapProperty(slot, objectType, material, propertyName)?.TexID == null;
        }

        /// <summary>
        /// Remove a persisted Cubemap override and restore the exact original value.
        /// </summary>
        public void RemoveMaterialCubemap(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName,
            GameObject go,
            bool setProperty = true)
        {
            MaterialEditRequestQueue.CancelTarget(go, material == null ? null : material.NameFormatted(), propertyName);
            var cubemapProperty = FindMaterialCubemapProperty(
                slot,
                objectType,
                material,
                propertyName);
            if (cubemapProperty == null)
                return;

            if (setProperty)
            {
                if (!cubemapProperty.RestoreCubemapOriginalSnapshot(go))
                    return;
            }

            cubemapProperty.ClearCubemapOriginalSnapshot();
            cubemapProperty.TexID = null;
            RemoveCubemapPropertyIfNull(cubemapProperty);
            PurgeUnusedTextures();
        }

        private bool SetCubemapWithProperty(GameObject go, MaterialCubemapProperty cubemapProperty)
        {
            if (cubemapProperty == null
                || !cubemapProperty.TexID.HasValue
                || cubemapProperty.NullCheck())
                return false;

            Cubemap cubemap;
            string error;
            if (!TryGetCubemap(cubemapProperty.TexID.Value, out cubemap, out error))
            {
                MaterialEditorPluginBase.Logger.LogWarning(error);
                return false;
            }

            if (!cubemapProperty.SynchronizeCubemapOriginalSnapshot(go))
                return false;
            return SetCubemap(
                go,
                cubemapProperty.MaterialName,
                cubemapProperty.Property,
                cubemap);
        }

        private MaterialCubemapProperty FindMaterialCubemapProperty(
            int slot,
            ObjectType objectType,
            Material material,
            string propertyName)
        {
            var coordinateIndex = GetCoordinateIndex(objectType);
            var materialName = material.NameFormatted();
            return MaterialCubemapPropertyList.FirstOrDefault(x =>
                x.ObjectType == objectType
                && x.CoordinateIndex == coordinateIndex
                && x.Slot == slot
                && x.Property == propertyName
                && x.MaterialName == materialName);
        }

        private void RemoveCubemapPropertyIfNull(MaterialCubemapProperty cubemapProperty)
        {
            if (!cubemapProperty.NullCheck())
                return;
            MaterialCubemapPropertyList.Remove(cubemapProperty);
        }
    }
}
