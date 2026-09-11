using MaterialEditorAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class SceneController
    {
        /// <summary>
        /// Import and persist a native Cubemap from an equirectangular PNG or Radiance HDR file.
        /// </summary>
        public void SetMaterialCubemapFromFile(
            int id,
            Material material,
            string propertyName,
            string filePath)
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
                id,
                material,
                propertyName,
                File.ReadAllBytes(filePath),
                null,
                true);
        }

        /// <summary>
        /// Import and persist a native Cubemap from encoded equirectangular PNG or Radiance HDR data.
        /// </summary>
        public void SetMaterialCubemap(
            int id,
            Material material,
            string propertyName,
            byte[] data)
        {
            MaterialEditRequestQueue.CancelTarget(GetObjectByID(id), material == null ? null : material.NameFormatted(), propertyName);
            SetMaterialCubemap(id, material, propertyName, data, null, false);
        }

        internal bool SetMaterialCubemap(
            int id,
            Material material,
            string propertyName,
            byte[] data,
            MaterialEditorCubemapContentKey contentKey)
        {
            return SetMaterialCubemap(
                id,
                material,
                propertyName,
                data,
                contentKey,
                false);
        }

        private bool SetMaterialCubemap(
            int id,
            Material material,
            string propertyName,
            byte[] data,
            MaterialEditorCubemapContentKey contentKey,
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
            GameObject gameObject = null;
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
                lease = null;
                storedLease = true;

                materialName = material.NameFormatted();
                gameObject = GetObjectByID(id);
                previousAppliedValues =
                    MaterialCubemapOriginalSnapshot.SynchronizeByMaterialReference(
                        gameObject,
                        materialName,
                        propertyName,
                        null);
                cubemapProperty = MaterialCubemapPropertyList.FirstOrDefault(x =>
                    x.ID == id
                    && x.Property == propertyName
                    && x.MaterialName == materialName);
                if (cubemapProperty == null)
                {
                    cubemapProperty = new MaterialCubemapProperty(
                        id,
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

                if (SetCubemapWithProperty(gameObject, cubemapProperty))
                    return true;

                RollbackMaterialCubemapSet(
                    gameObject,
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
                    gameObject,
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

        private bool SetCubemapWithProperty(
            GameObject gameObject,
            MaterialCubemapProperty cubemapProperty)
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

            if (!cubemapProperty.SynchronizeCubemapOriginalSnapshot(gameObject))
                return false;
            return SetCubemap(
                gameObject,
                cubemapProperty.MaterialName,
                cubemapProperty.Property,
                cubemap);
        }

        public Cubemap GetMaterialCubemap(
            int id,
            Material material,
            string propertyName)
        {
            var cubemapProperty = MaterialCubemapPropertyList.FirstOrDefault(x =>
                x.ID == id
                && x.MaterialName == material.NameFormatted()
                && x.Property == propertyName);
            if (cubemapProperty == null || !cubemapProperty.TexID.HasValue)
                return null;

            Cubemap cubemap;
            string error;
            return TryGetCubemap(cubemapProperty.TexID.Value, out cubemap, out error)
                ? cubemap
                : null;
        }

        public bool GetMaterialCubemapOriginal(
            int id,
            Material material,
            string propertyName)
        {
            return MaterialCubemapPropertyList.FirstOrDefault(x =>
                x.ID == id
                && x.MaterialName == material.NameFormatted()
                && x.Property == propertyName)?.TexID == null;
        }

        public void RemoveMaterialCubemap(
            int id,
            Material material,
            string propertyName)
        {
            MaterialEditRequestQueue.CancelTarget(GetObjectByID(id), material == null ? null : material.NameFormatted(), propertyName);
            var cubemapProperty = MaterialCubemapPropertyList.FirstOrDefault(x =>
                x.ID == id
                && x.MaterialName == material.NameFormatted()
                && x.Property == propertyName);
            if (cubemapProperty == null)
                return;

            var gameObject = GetObjectByID(id);
            if (!cubemapProperty.RestoreCubemapOriginalSnapshot(gameObject))
                return;
            cubemapProperty.ClearCubemapOriginalSnapshot();
            cubemapProperty.TexID = null;
            if (cubemapProperty.NullCheck())
                MaterialCubemapPropertyList.Remove(cubemapProperty);
            PurgeUnusedTextures();
        }
    }
}
