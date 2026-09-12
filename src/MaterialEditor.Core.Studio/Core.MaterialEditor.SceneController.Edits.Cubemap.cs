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
            if (material == null) return false;
            var go = GetObjectByID(id);
            var existing = MaterialCubemapPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
            var result = MaterialCubemapImportTransaction.Execute(data, contentKey,
                go, material.NameFormatted(), propertyName, logNormalizationWarning,
                new MaterialCubemapImportStorage
                {
                    Count = () => TextureDictionary.Count,
                    StoreData = SetAndGetTextureID,
                    StoreLease = CubemapLeases.Store,
                    RemoveCreated = RemoveFailedCubemapData,
                    PurgeLeases = PurgeUnusedCubemapLeases
                }, MaterialCubemapPropertyList, existing,
                texId => new MaterialCubemapProperty(id, material.NameFormatted(), propertyName, texId),
                CubemapRecordAccess, candidate => SetCubemapWithProperty(go, candidate));
            if (!result.Succeeded)
                MaterialEditorPluginBase.Logger?.LogWarning("Cubemap import: " + result.Stage + ": " + result.Diagnostic);
            return result.Succeeded;
        }

        private static readonly MaterialCubemapRecordAccess<MaterialCubemapProperty> CubemapRecordAccess =
            new MaterialCubemapRecordAccess<MaterialCubemapProperty>
            {
                GetId = x => x.TexID,
                SetId = (x, id) => x.TexID = id,
                Original = x => x.CubemapOriginalState
            };

        private void RemoveFailedCubemapData(int texId)
        {
            CubemapLeases.Release(texId);
            TextureContainer container;
            if (!TextureDictionary.TryGetValue(texId, out container)) return;
            try { container?.Dispose(); }
            finally { TextureDictionary.Remove(texId); }
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
            var cubemapProperty = MaterialCubemapPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
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
            return MaterialCubemapPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted())?.TexID == null;
        }

        public void RemoveMaterialCubemap(
            int id,
            Material material,
            string propertyName)
        {
            MaterialEditRequestQueue.CancelTarget(GetObjectByID(id), material == null ? null : material.NameFormatted(), propertyName);
            var cubemapProperty = MaterialCubemapPropertyList.FirstOrDefault(x => x.ID == id && x.Property == propertyName && x.MaterialName == material.NameFormatted());
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
