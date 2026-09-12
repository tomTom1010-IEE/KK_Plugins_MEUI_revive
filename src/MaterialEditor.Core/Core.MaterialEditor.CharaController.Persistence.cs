using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using MaterialEditorAPI;
using MessagePack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;
#if !EC
using KKAPI.Studio;
#endif
#if AI || HS2
using AIChara;
#endif
#if PH
using ChaFileCoordinate = Character.CustomParameter;
using ChaControl = Human;
#endif
namespace KK_Plugins.MaterialEditor
{
    using MEAnimationController = MEAnimationController<MaterialEditorCharaController, MaterialEditorCharaController.MaterialTextureProperty>;

    public partial class MaterialEditorCharaController
    {
        /// <summary>
        /// Handles saving data to coordinate cards
        /// </summary>
        /// <param name="coordinate"></param>
        protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
        {
            RemoveLegacyMaterialVectorDuplicates();
            var coordinateRendererPropertyList = RendererPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateProjectorPropertyList = ProjectorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialNamePropertyList = MaterialNamePropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialFloatPropertyList = MaterialFloatPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialKeywordPropertyList = MaterialKeywordPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialColorPropertyList = MaterialColorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialVectorPropertyList = MaterialVectorPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialTexturePropertyList = MaterialTexturePropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialCubemapPropertyList = MaterialCubemapPropertyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialShaderList = MaterialShaderList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateMaterialCopyList = MaterialCopyList.Where(x => x.CoordinateIndex == CurrentCoordinateIndex && x.ObjectType != ObjectType.Hair && x.ObjectType != ObjectType.Character).ToList();
            var coordinateTextureDictionary = new Dictionary<int, byte[]>();

            var usedTexIDMap = MEAnimationController.GetUsedTexIDSet(AnimationControllerMap, coordinateMaterialTexturePropertyList);
            foreach (var cubemapProperty in coordinateMaterialCubemapPropertyList)
                if (cubemapProperty.TexID.HasValue)
                    usedTexIDMap.Add(cubemapProperty.TexID.Value);

            foreach (var tex in TextureDictionary)
            {
                if (usedTexIDMap.Contains(tex.Key))
                    coordinateTextureDictionary.Add(tex.Key, tex.Value.Data);
            }

            if (coordinateRendererPropertyList.Count == 0 && coordinateMaterialNamePropertyList.Count == 0 && coordinateMaterialFloatPropertyList.Count == 0 && coordinateMaterialKeywordPropertyList.Count == 0 && coordinateMaterialColorPropertyList.Count == 0 && coordinateMaterialVectorPropertyList.Count == 0 && coordinateMaterialTexturePropertyList.Count == 0 && coordinateMaterialCubemapPropertyList.Count == 0 && coordinateMaterialShaderList.Count == 0 && coordinateMaterialCopyList.Count == 0)
            {
                SetCoordinateExtendedData(coordinate, null);
            }
            else
            {
                var data = new PluginData();
                if (coordinateTextureDictionary.Count > 0)
                    data.data.Add(TexDicSaveKey, MessagePackSerializer.Serialize(coordinateTextureDictionary));
                else
                    data.data.Add(TexDicSaveKey, null);

                WriteRecords(nameof(RendererPropertyList), coordinateRendererPropertyList);
                WriteRecords(nameof(ProjectorPropertyList), coordinateProjectorPropertyList);
                WriteRecords(nameof(MaterialNamePropertyList), coordinateMaterialNamePropertyList);
                WriteRecords(nameof(MaterialFloatPropertyList), coordinateMaterialFloatPropertyList);
                WriteRecords(nameof(MaterialKeywordPropertyList), coordinateMaterialKeywordPropertyList);
                WriteRecords(nameof(MaterialColorPropertyList), coordinateMaterialColorPropertyList);
                WriteRecords(nameof(MaterialVectorPropertyList), coordinateMaterialVectorPropertyList);
                WriteRecords(nameof(MaterialTexturePropertyList), coordinateMaterialTexturePropertyList);
                WriteRecords(nameof(MaterialCubemapPropertyList), coordinateMaterialCubemapPropertyList);
                WriteRecords(nameof(MaterialShaderList), coordinateMaterialShaderList);
                WriteRecords(nameof(MaterialCopyList), coordinateMaterialCopyList);

                SetCoordinateExtendedData(coordinate, data);

                void WriteRecords<T>(string key, List<T> records)
                {
                    // Keep empty fields present as null in the saved data.
                    data.data.Add(key,
                        records.Count > 0 ? MessagePackSerializer.Serialize(records) : null);
                }
            }

            base.OnCoordinateBeingSaved(coordinate);
        }

        /// <summary>
        /// Handles loading data from coordinate cards
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="maintainState"></param>
        protected override void OnCoordinateBeingLoaded(ChaFileCoordinate coordinate, bool maintainState)
        {
            LoadCoordinateExtSaveData(coordinate);

            CoordinateChanging = true;

            if (MakerAPI.InsideAndLoaded)
                MaterialEditorUI.Visible = false;
            ReleaseUiForCharacterContentsReplacement();

            ChaControl.StartCoroutine(LoadData(true, true, false));
            base.OnCoordinateBeingLoaded(coordinate, maintainState);
        }

        private void LoadCharacterExtSaveData()
        {
            RemoveMaterialCopies(ChaControl.gameObject);

            var objectTypesToLoad = PrepareCharacterLoad();

            //Don't destroy the textures in H mode because they will still be needed
            if (KoikatuAPI.GetCurrentGameMode() != GameMode.MainGame)
            {
                PurgeUnusedTextures();
            }

            CharacterLoading = true;

            var data = GetExtendedData();
            if (data != null)
            {
                var importDictionary = new Dictionary<int, int>();
                MaterialEditorCharaController duplicateSourceController = null;

#if !EC
                if (DuplicatingFrom.HasValue)
                {
                    var chaCtrl = (Studio.Studio.Instance.dicObjectCtrl[DuplicatingFrom.Value] as Studio.OCIChar).charInfo
#if PH
                        .human
#endif
                        ;
                    duplicateSourceController = MaterialEditorPlugin.GetCharaController(chaCtrl);
                    importDictionary = MaterialEditLoadContext.ImportTextures(duplicateSourceController.TextureDictionary, x => x.Data, SetAndGetTextureID);
                    DuplicatingFrom = null;
                }
                else
#endif
                {
                    var importDictionaryTemp = TextureSaveHandler.Instance.Load<Dictionary<int, TextureContainer>>(data, TexDicSaveKey, true);
                    try
                    {
                        importDictionary = MaterialEditLoadContext.ImportTextures(importDictionaryTemp, x => x.Data, SetAndGetTextureID);
                    }
                    finally
                    {
                        TextureSaveHandler.DisposeTextureContainers(importDictionaryTemp);
                    }
                }

                var context = new CharacterLoadContext
                {
                    Data = new MaterialEditLoadContext(data, importDictionary),
                    ObjectTypes = objectTypesToLoad,
                    DuplicateSource = duplicateSourceController
                };
                LoadMaterialShaderList(context);

                LoadRendererPropertyList(context);

                LoadProjectorPropertyList(context);

                LoadMaterialNamePropertyList(context);

                LoadMaterialFloatPropertyList(context);

                LoadMaterialKeywordPropertyList(context);

                LoadMaterialColorPropertyList(context);

                LoadMaterialVectorPropertyList(context);

                LoadMaterialTexturePropertyList(context);

                LoadMaterialCubemapPropertyList(context);

                LoadMaterialCopyList(context);
            }
        }

        private void LoadCoordinateExtSaveData(ChaFileCoordinate coordinate)
        {
            var objectTypesToLoad = PrepareCoordinateLoad();

            var data = GetCoordinateExtendedData(coordinate);
            if (data?.data != null)
            {
                var importDictionary = new Dictionary<int, int>();

                if (data.data.TryGetValue(TexDicSaveKey, out var texDic) && texDic != null)
                    importDictionary = MaterialEditLoadContext.ImportTextures(MessagePackSerializer.Deserialize<Dictionary<int, byte[]>>((byte[])texDic), x => x, SetAndGetTextureID);

                var context = new CharacterLoadContext
                {
                    Data = new MaterialEditLoadContext(data, importDictionary),
                    ObjectTypes = objectTypesToLoad,
                    DestinationCoordinate = CurrentCoordinateIndex
                };
                LoadMaterialShaderList(context);

                LoadRendererPropertyList(context);

                LoadMaterialNamePropertyList(context);

                LoadMaterialFloatPropertyList(context);

                LoadMaterialKeywordPropertyList(context);

                LoadMaterialColorPropertyList(context);

                LoadMaterialVectorPropertyList(context);

                LoadMaterialTexturePropertyList(context);

                LoadMaterialCubemapPropertyList(context);

                LoadMaterialCopyList(context);
            }
        }

        /// <summary></summary>
        public IEnumerator LoadData(bool clothes, bool accessories, bool hair)
        {
            return LoadData(clothes, accessories, hair, true);
        }

        /// <summary></summary>
        public IEnumerator LoadData(bool clothes, bool accessories, bool hair, bool body)
        {
            yield return null;
#if !EC
            if (KKAPI.Studio.StudioAPI.InsideStudio)
            {
                yield return null;
                yield return null;
            }
#endif
            while (ChaControl == null || ChaControl.GetHead() == null)
                yield return null;

            if (body)
                CorrectTongue();
#if KK || KKS
            if (KKAPI.Studio.StudioAPI.InsideStudio && body)
                CorrectFace();
#endif

            var scope = new CharacterRestoreScope(clothes, accessories, hair, body);

            //Instantiate all material copies before applying any edits to ensure edits are applied to copies
            RestoreMaterialCopyList(scope);

            // Rename materials before applying edits, but after copying materials, to ensure no missing material mishaps occur
            // Do not move this anywhere else
            RestoreMaterialNamePropertyList(scope);

            RestoreMaterialShaderList(scope);
            MigrateLegacyMaterialVectorProperties(clothes, accessories, hair, body);

            RestoreRendererPropertyList(scope);
            RestoreMaterialFloatPropertyList(scope);
            RestoreMaterialKeywordPropertyList(scope);
            RestoreMaterialColorPropertyList(scope);
            RestoreMaterialVectorPropertyList(scope);
            RestoreMaterialTexturePropertyList(scope);
            RestoreMaterialCubemapPropertyList(scope);
            RestoreProjectorPropertyList(scope);


#if KK || EC || KKS
            if (MaterialEditorPlugin.RimRemover.Value)
                RemoveRim();
#endif
        }

        /// <summary>
        /// Migrates Color-backed Vector overrides after shader overrides are active.
        /// Migration is in memory; persisted data changes only during a normal save.
        /// </summary>
        private void MigrateLegacyMaterialVectorProperties(bool clothes, bool accessories, bool hair, bool body)
        {
            MaterialVectorPropertyList.RemoveAll(property => property.Value == property.ValueOriginal);
            foreach (var property in MaterialColorPropertyList.ToList())
            {
                if (property.ObjectType == ObjectType.Clothing && !clothes) continue;
                if (property.ObjectType == ObjectType.Accessory && !accessories) continue;
                if (property.ObjectType == ObjectType.Hair && !hair) continue;
                if ((property.ObjectType == ObjectType.Clothing || property.ObjectType == ObjectType.Accessory) && property.CoordinateIndex != CurrentCoordinateIndex) continue;
                if (property.ObjectType == ObjectType.Character && !body) continue;

                var go = FindGameObject(property.ObjectType, property.Slot);
                MigrateLegacyMaterialVectorProperty(property.Slot, property.ObjectType, property.MaterialName, property.Property, go);
            }
        }

        /// <summary>
        /// Corrects the tongue materials since some of them are not properly refreshed on replacing a character
        /// </summary>
        private void CorrectTongue()
        {
#if KK || KKS
            if (!ChaControl.hiPoly) return;
#endif

#if KK || EC || KKS || AI || HS2
            //Get the tongue material used by the head since this one is properly refreshed with every character reload
            Material tongueMat = null;
            foreach (var renderer in GetRendererList(ChaControl.objHead))
            {
                var mat = GetMaterials(ChaControl.gameObject, renderer).FirstOrDefault(x => x.name.Contains("tang"));
                if (mat != null)
                    tongueMat = mat;
            }

            //Set the materials of the other tongues to the one from the head
            if (tongueMat != null)
            {
                string shaderName = tongueMat.shader.NameFormatted();
                string materialName = tongueMat.NameFormatted();

                SetShader(ChaControl.gameObject, materialName, shaderName);

                foreach (var property in XMLShaderProperties[XMLShaderProperties.ContainsKey(shaderName) ? shaderName : "default"])
                {
                    if (property.Value.Type == ShaderPropertyType.Color)
                        SetColor(ChaControl.gameObject, materialName, property.Key, tongueMat.GetColor("_" + property.Key));
                    else if (property.Value.Type == ShaderPropertyType.Vector)
                        SetVector(ChaControl.gameObject, materialName, property.Key, tongueMat.GetVector("_" + property.Key));
                    else if (property.Value.Type == ShaderPropertyType.Float)
                        SetFloat(ChaControl.gameObject, materialName, property.Key, tongueMat.GetFloat("_" + property.Key));
                    else if (property.Value.Type == ShaderPropertyType.Texture)
                    {
                        var texture = MaterialPropertyAccess.GetTexture(
                            tongueMat,
                            MaterialPropertyIdCache.Get(property.Key));
                        SetTexture(ChaControl.gameObject, materialName, property.Key, texture);
                    }
                    else if (property.Value.Type == ShaderPropertyType.Cubemap)
                    {
                        var cubemap = MaterialPropertyAccess.GetTexture(
                            tongueMat,
                            MaterialPropertyIdCache.Get(property.Key)) as Cubemap;
                        if (cubemap != null)
                            SetCubemap(ChaControl.gameObject, materialName, property.Key, cubemap);
                    }
                    else if (property.Value.Type == ShaderPropertyType.Keyword)
                        SetKeyword(ChaControl.gameObject, materialName, property.Key, tongueMat.IsKeywordEnabled("_" + property.Key));
                }
            }
#endif
        }

#if KK || KKS
        /// <summary>
        /// Force reload face textures
        /// </summary>
        private void CorrectFace()
        {
            ChaControl.ChangeSettingEyebrow();
            ChaControl.ChangeSettingEye(true, true, true);
            ChaControl.ChangeSettingEyeHiUp();
            ChaControl.ChangeSettingEyeHiDown();
            ChaControl.ChangeSettingEyelineUp();
            ChaControl.ChangeSettingEyelineDown();
            ChaControl.ChangeSettingWhiteOfEye(true, true);
            ChaControl.ChangeSettingNose();
        }
#endif

#if KK || EC || KKS
        private void RemoveRim()
        {
            for (var i = 0; i < ChaControl.objClothes.Length; i++)
                RemoveRimClothes(i);
            for (var i = 0; i < ChaControl.objHair.Length; i++)
                RemoveRimHair(i);
            for (var i = 0; i < ChaControl.GetAccessoryObjects().Length; i++)
                RemoveRimAccessory(i);
        }
        private void RemoveRimClothes(int slot)
        {
            var go = ChaControl.objClothes[slot];
            foreach (var renderer in GetRendererList(go))
                foreach (var material in GetMaterials(go, renderer))
                    if (material.HasProperty("_rimV") && GetMaterialFloatPropertyValue(slot, ObjectType.Clothing, material, "rimV", go) == null)
                        SetMaterialFloatProperty(slot, ObjectType.Clothing, material, "rimV", 0, go);
        }
        private IEnumerator RemoveRimHairCo(int slot)
        {
            yield return null;
            RemoveRimHair(slot);
        }
        private void RemoveRimHair(int slot)
        {
            var go = ChaControl.objHair[slot];
            foreach (var renderer in GetRendererList(go))
                foreach (var material in GetMaterials(go, renderer))
                    if (material.HasProperty("_rimV") && GetMaterialFloatPropertyValue(slot, ObjectType.Hair, material, "rimV", go) == null)
                        SetMaterialFloatProperty(slot, ObjectType.Hair, material, "rimV", 0, go);
        }
        private void RemoveRimAccessory(int slot)
        {
            var go = ChaControl.GetAccessoryObject(slot);
            if (go != null)
                foreach (var renderer in GetRendererList(go))
                    foreach (var material in GetMaterials(go, renderer))
                        if (material.HasProperty("_rimV") && GetMaterialFloatPropertyValue(slot, ObjectType.Accessory, material, "rimV", go) == null)
                            SetMaterialFloatProperty(slot, ObjectType.Accessory, material, "rimV", 0, go);
        }
#endif

        /// <summary>
        /// Finds the texture bytes in the dictionary of textures and returns its ID. If not found, adds the texture to the dictionary and returns the ID of the added texture.
        /// </summary>
        private int SetAndGetTextureID(byte[] textureBytes)
        {
            return TextureSaveHandler.GetOrAddTexture(TextureDictionary, textureBytes);
        }

        private List<ObjectType> PrepareCharacterLoad()
        {
            List<ObjectType> objectTypesToLoad = new List<ObjectType>();

            var loadFlags = MakerAPI.GetCharacterLoadFlags();
            if (loadFlags == null)
            {
                RendererPropertyList.Clear();
                ProjectorPropertyList.Clear();
                MaterialNamePropertyList.Clear();
                MaterialFloatPropertyList.Clear();
                MaterialKeywordPropertyList.Clear();
                MaterialColorPropertyList.Clear();
                MaterialVectorPropertyList.Clear();
                MaterialTexturePropertyList.Clear();
                MaterialCubemapPropertyList.Clear();
                MaterialShaderList.Clear();
                MaterialCopyList.Clear();
                AnimationControllerMap.Clear();

                objectTypesToLoad.Add(ObjectType.Accessory);
                objectTypesToLoad.Add(ObjectType.Character);
                objectTypesToLoad.Add(ObjectType.Clothing);
                objectTypesToLoad.Add(ObjectType.Hair);
            }
            else
            {
                bool changed = false;

                if (loadFlags.Face || loadFlags.Body)
                {
                    RemovePropertiesForLoad((type, coordinate) => type == ObjectType.Character, true);

                    objectTypesToLoad.Add(ObjectType.Character);

                    changed = true;
                }
                if (loadFlags.Clothes)
                {
                    RemovePropertiesForLoad((type, coordinate) => type == ObjectType.Clothing, true);
                    objectTypesToLoad.Add(ObjectType.Clothing);

                    RemovePropertiesForLoad((type, coordinate) => type == ObjectType.Accessory, true);
                    objectTypesToLoad.Add(ObjectType.Accessory);

                    changed = true;
                }
                if (loadFlags.Hair)
                {
                    RemovePropertiesForLoad((type, coordinate) => type == ObjectType.Hair, true);
                    objectTypesToLoad.Add(ObjectType.Hair);

                    changed = true;
                }

                if (changed)
                {
                    PurgeUnusedAnimation();
                }
            }

            return objectTypesToLoad;
        }

        private List<ObjectType> PrepareCoordinateLoad()
        {
            List<ObjectType> objectTypesToLoad = new List<ObjectType>();

            var loadFlags = MakerAPI.GetCoordinateLoadFlags();
            if (loadFlags == null)
            {
                RemovePropertiesForLoad((type, coordinate) => (type == ObjectType.Clothing || type == ObjectType.Accessory) && coordinate == CurrentCoordinateIndex, false);

                objectTypesToLoad.Add(ObjectType.Accessory);
                objectTypesToLoad.Add(ObjectType.Clothing);

                PurgeUnusedAnimation();
            }
            else
            {
                if (loadFlags.Clothes)
                {
                    RemovePropertiesForLoad((type, coordinate) => type == ObjectType.Clothing && coordinate == CurrentCoordinateIndex, false);
                    objectTypesToLoad.Add(ObjectType.Clothing);
                }
                if (loadFlags.Accessories)
                {
                    RemovePropertiesForLoad((type, coordinate) => type == ObjectType.Accessory && coordinate == CurrentCoordinateIndex, false);
                    objectTypesToLoad.Add(ObjectType.Accessory);
                }

                if (loadFlags.Clothes || loadFlags.Accessories)
                {
                    PurgeUnusedAnimation();
                }
            }

            return objectTypesToLoad;
        }

        private void RemovePropertiesForLoad(Func<ObjectType, int, bool> remove, bool includeProjectors)
        {
            RendererPropertyList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
            MaterialNamePropertyList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
            MaterialFloatPropertyList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
            MaterialKeywordPropertyList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
            MaterialColorPropertyList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
            MaterialVectorPropertyList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
            MaterialTexturePropertyList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
            MaterialCubemapPropertyList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
            MaterialShaderList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
            MaterialCopyList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
            // Coordinate loading historically excludes projector records.
            if (includeProjectors)
                ProjectorPropertyList.RemoveAll(x => remove(x.ObjectType, x.CoordinateIndex));
        }
    }
}
