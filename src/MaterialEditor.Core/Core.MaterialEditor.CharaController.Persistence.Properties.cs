using System.Collections.Generic;
using System.Linq;
using MaterialEditorAPI;
using UnityEngine;

namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorCharaController
    {
        private sealed class CharacterLoadContext
        {
            internal MaterialEditLoadContext Data;
            internal List<ObjectType> ObjectTypes;
            internal int? DestinationCoordinate;
            internal MaterialEditorCharaController DuplicateSource;
            internal int Coordinate(ObjectType type, int saved) =>
                DestinationCoordinate ?? (type == ObjectType.Character ? 0 : saved);
        }

        private void LoadMaterialShaderList(CharacterLoadContext context)
        {
            context.Data.Read<MaterialShader>(nameof(MaterialShaderList), loadedProperty =>
            {
                int coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                if (context.ObjectTypes.Contains(loadedProperty.ObjectType))
                    MaterialShaderList.Add(new MaterialShader(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
            });
        }

        private void LoadRendererPropertyList(CharacterLoadContext context)
        {
            context.Data.Read<RendererProperty>(nameof(RendererPropertyList), loadedProperty =>
            {
                int coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                if (context.ObjectTypes.Contains(loadedProperty.ObjectType))
                    RendererPropertyList.Add(new RendererProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void LoadProjectorPropertyList(CharacterLoadContext context)
        {
            context.Data.Read<ProjectorProperty>(nameof(ProjectorPropertyList), loadedProperty =>
            {
                int coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                if (context.ObjectTypes.Contains(loadedProperty.ObjectType))
                    ProjectorPropertyList.Add(new ProjectorProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.ProjectorName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void LoadMaterialNamePropertyList(CharacterLoadContext context)
        {
            context.Data.Read<MaterialNameProperty>(nameof(MaterialNamePropertyList), loadedProperty =>
            {
                int coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                if (context.ObjectTypes.Contains(loadedProperty.ObjectType))
                    MaterialNamePropertyList.Add(new MaterialNameProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value));
            });
        }

        private void LoadMaterialFloatPropertyList(CharacterLoadContext context)
        {
            context.Data.Read<MaterialFloatProperty>(nameof(MaterialFloatPropertyList), loadedProperty =>
            {
                int coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                if (context.ObjectTypes.Contains(loadedProperty.ObjectType))
                    MaterialFloatPropertyList.Add(new MaterialFloatProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void LoadMaterialKeywordPropertyList(CharacterLoadContext context)
        {
            context.Data.Read<MaterialKeywordProperty>(nameof(MaterialKeywordPropertyList), loadedProperty =>
            {
                int coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                if (context.ObjectTypes.Contains(loadedProperty.ObjectType))
                    MaterialKeywordPropertyList.Add(new MaterialKeywordProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void LoadMaterialColorPropertyList(CharacterLoadContext context)
        {
            context.Data.Read<MaterialColorProperty>(nameof(MaterialColorPropertyList), loadedProperty =>
            {
                int coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                if (context.ObjectTypes.Contains(loadedProperty.ObjectType))
                    MaterialColorPropertyList.Add(new MaterialColorProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void LoadMaterialVectorPropertyList(CharacterLoadContext context)
        {
            context.Data.Read<MaterialVectorProperty>(nameof(MaterialVectorPropertyList), loadedProperty =>
            {
                int coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                if (context.ObjectTypes.Contains(loadedProperty.ObjectType))
                    MaterialVectorPropertyList.Add(new MaterialVectorProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void LoadMaterialTexturePropertyList(CharacterLoadContext context)
        {
            context.Data.Read<MaterialTextureProperty>(nameof(MaterialTexturePropertyList), loadedProperty =>
            {
                if (context.ObjectTypes.Contains(loadedProperty.ObjectType) && !loadedProperty.NullCheck())
                {
                    int? texID = null;
                    if (loadedProperty.TexID != null && context.Data.TextureIds.TryGetValue((int)loadedProperty.TexID, out var importTextID))
                        texID = importTextID;
                    MEAnimationUtil.RemapTexID(loadedProperty.TexAnimationDef, context.Data.TextureIds);
                    int coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                    MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.Property, texID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal, loadedProperty.TexAnimationDef);
                    MaterialTexturePropertyList.Add(newTextureProperty);
                }
            });
        }

        private void LoadMaterialCubemapPropertyList(CharacterLoadContext context)
        {
            context.Data.Read<MaterialCubemapProperty>(nameof(MaterialCubemapPropertyList), loadedProperty =>
            {
                if (!context.ObjectTypes.Contains(loadedProperty.ObjectType) || loadedProperty.NullCheck())
                    return;

                int importTexID;
                if (!context.Data.TextureIds.TryGetValue(loadedProperty.TexID.Value, out importTexID))
                    return;
                var coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                var newCubemapProperty = new MaterialCubemapProperty(
                    loadedProperty.ObjectType,
                    coordinateIndex,
                    loadedProperty.Slot,
                    loadedProperty.MaterialName,
                    loadedProperty.Property,
                    importTexID);

                if (context.DuplicateSource != null)
                {
                    var sourceProperty = context.DuplicateSource.MaterialCubemapPropertyList.FirstOrDefault(x =>
                        x.ObjectType == loadedProperty.ObjectType
                        && x.CoordinateIndex == coordinateIndex
                        && x.Slot == loadedProperty.Slot
                        && x.MaterialName == loadedProperty.MaterialName
                        && x.Property == loadedProperty.Property);
                    GameObject sourceGameObject = null;
                    if (sourceProperty != null
                        && (sourceProperty.ObjectType == ObjectType.Character
                            || sourceProperty.ObjectType == ObjectType.Hair
                            || sourceProperty.CoordinateIndex == context.DuplicateSource.CurrentCoordinateIndex))
                        sourceGameObject = context.DuplicateSource.FindGameObject(
                            sourceProperty.ObjectType,
                            sourceProperty.Slot);
                    newCubemapProperty.InheritCubemapOriginalSnapshot(
                        sourceProperty,
                        sourceGameObject);
                }
                MaterialCubemapPropertyList.Add(newCubemapProperty);
            });
        }

        private void LoadMaterialCopyList(CharacterLoadContext context)
        {
            context.Data.Read<MaterialCopy>(nameof(MaterialCopyList), loadedProperty =>
            {
                int coordinateIndex = context.Coordinate(loadedProperty.ObjectType, loadedProperty.CoordinateIndex);
                if (context.ObjectTypes.Contains(loadedProperty.ObjectType))
                    MaterialCopyList.Add(new MaterialCopy(loadedProperty.ObjectType, coordinateIndex, loadedProperty.Slot, loadedProperty.MaterialName, loadedProperty.MaterialCopyName));
            });
        }

    }
}
