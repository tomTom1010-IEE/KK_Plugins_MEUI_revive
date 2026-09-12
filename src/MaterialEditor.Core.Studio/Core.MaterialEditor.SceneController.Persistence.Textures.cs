using System.Collections.Generic;
using KKAPI.Utilities;
using System.Linq;
using KKAPI.Studio.SaveLoad;
using MaterialEditorAPI;
using Studio;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class SceneController
    {
        private void LoadSceneMaterialTexturePropertyList(SceneLoadContext context)
        {
            context.Data.Read<MaterialTextureProperty>(nameof(MaterialTexturePropertyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(context.Items, loadedProperty.ID, out var objID);
                if (go != null)
                {
                    int? texID = null;
                    if (context.Operation == SceneOperationKind.Import)
                    {
                        if (loadedProperty.TexID != null)
                            texID = context.Data.RemapTexture(loadedProperty.TexID);
                        MEAnimationUtil.RemapTexID(loadedProperty.TexAnimationDef, context.Data.TextureIds);
                    }
                    else
                        texID = loadedProperty.TexID;

                    MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(objID, loadedProperty.MaterialName, loadedProperty.Property, texID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal, loadedProperty.TexAnimationDef);

                    bool setTex = false;
                    if (newTextureProperty.TexID != null)
                        setTex = SetTextureWithProperty(go, newTextureProperty);

                    bool setOffset = SetTextureOffset(go, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Offset);
                    bool setScale = SetTextureScale(go, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Scale);

                    if (setTex || setOffset || setScale)
                        MaterialTexturePropertyList.Add(newTextureProperty);
                }
            });
        }

        private void LoadSceneMaterialCubemapPropertyList(SceneLoadContext context)
        {
            context.Data.Read<MaterialCubemapProperty>(nameof(MaterialCubemapPropertyList), loadedProperty =>
            {
                GameObject go = ExtractGameObject(
                    context.Items,
                    loadedProperty.ID,
                    out var objID);
                if (go == null)
                    return;

                int? texID = loadedProperty.TexID;
                if (context.Operation == SceneOperationKind.Import
                    && loadedProperty.TexID.HasValue)
                    texID = context.Data.RemapTexture(loadedProperty.TexID);

                var newCubemapProperty = new MaterialCubemapProperty(
                    objID,
                    loadedProperty.MaterialName,
                    loadedProperty.Property,
                    texID);
                if (newCubemapProperty.TexID.HasValue
                    && SetCubemapWithProperty(go, newCubemapProperty))
                    MaterialCubemapPropertyList.Add(newCubemapProperty);
            });
        }

    }
}
