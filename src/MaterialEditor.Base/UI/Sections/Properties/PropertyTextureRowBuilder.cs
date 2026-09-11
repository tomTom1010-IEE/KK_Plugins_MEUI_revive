using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class PropertyTextureRowBuilder
    {
        private readonly MaterialEditService _editService;
        private readonly MaterialEditorPresentationActions _actions;

        internal PropertyTextureRowBuilder(
            MaterialEditService editService,
            MaterialEditorPresentationActions actions)
        {
            _editService = editService;
            _actions = actions;
        }

        internal IEnumerable<RowModel> CreateTextureRows(
            PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var projector = descriptor.Projector;
            var propertyName = descriptor.Name;
            var currentTexture = MaterialPropertyAccess.GetTexture(
                material,
                descriptor.PropertyHandle);

            var textureItem = PropertyRowBinding.BuiltIn(new TexturePropertyRowModel(descriptor.DisplayName)
            {
                Changed = !_editService.GetMaterialTextureValueOriginal(data, material, propertyName, gameObject),
                Exists = currentTexture != null,
                Export = () => _actions.ExportTexture(material, propertyName),
                SelectInterpolable = () =>
                    _actions.SelectInterpolable(
                        gameObject,
                        RowModel.RowItemType.TextureProperty,
                        descriptor.MaterialName,
                        propertyName,
                        string.Empty)
            }, descriptor);
            textureItem.Import = () =>
                _actions.ImportTexture(textureItem, gameObject, data, material, propertyName);
            textureItem.Reset = () =>
            {
                _editService.RemoveMaterialTexture(
                    data,
                    material,
                    propertyName,
                    gameObject);
                textureItem.Changed =
                    !_editService.GetMaterialTextureValueOriginal(
                        data,
                        material,
                        propertyName,
                        gameObject);
                var restoredTexture = MaterialPropertyAccess.GetTexture(
                    material,
                    descriptor.PropertyHandle);
                textureItem.Exists = restoredTexture != null;
            };

            var textureOffset = MaterialPropertyAccess.GetTextureOffset(
                material,
                descriptor.PropertyHandle);
            var textureOffsetOriginal =
                _editService.GetMaterialTextureOffsetOriginal(data, material, propertyName, gameObject)
                ?? textureOffset;
            var textureScale = MaterialPropertyAccess.GetTextureScale(
                material,
                descriptor.PropertyHandle);
            var textureScaleOriginal =
                _editService.GetMaterialTextureScaleOriginal(data, material, propertyName, gameObject)
                ?? textureScale;

            var textureOffsetScaleItem = PropertyRowBinding.BuiltIn(new TextureOffsetScaleRowModel()
            {
                Offset = textureOffset,
                OriginalOffset = textureOffsetOriginal,
                OffsetOnChange = value =>
                    _editService.SetMaterialTextureOffset(data, material, propertyName, value, gameObject),
                OffsetOnReset = () =>
                    _editService.RemoveMaterialTextureOffset(data, material, propertyName, gameObject),
                Scale = textureScale,
                OriginalScale = textureScaleOriginal,
                ScaleOnChange = value =>
                    _editService.SetMaterialTextureScale(data, material, propertyName, value, gameObject),
                ScaleOnReset = () =>
                    _editService.RemoveMaterialTextureScale(data, material, propertyName, gameObject)
            }, descriptor);

            return new RowModel[] { textureItem, textureOffsetScaleItem };
        }

        internal CubemapPropertyRowModel CreateCubemapRow(PropertyDescriptor descriptor)
        {
            var gameObject = descriptor.GameObject;
            var data = descriptor.Data;
            var material = descriptor.Material;
            var propertyName = descriptor.Name;
            var cubemapItem = PropertyRowBinding.BuiltIn(new CubemapPropertyRowModel(descriptor.DisplayName)
            {
                Changed = !_editService.GetMaterialCubemapValueOriginal(
                    data,
                    material,
                    propertyName,
                    gameObject),
                Exists = MaterialPropertyAccess.GetTexture(
                    material,
                    descriptor.PropertyHandle) is Cubemap,
                Export = () => _actions.ExportCubemap(material, propertyName)
            }, descriptor);
            cubemapItem.Import = () =>
                _actions.ImportCubemap(
                    cubemapItem,
                    gameObject,
                    data,
                    material,
                    propertyName);
            cubemapItem.Reset = () =>
            {
                _editService.RemoveMaterialCubemap(
                    data,
                    material,
                    propertyName,
                    gameObject);
                cubemapItem.Changed =
                    !_editService.GetMaterialCubemapValueOriginal(
                        data,
                        material,
                        propertyName,
                        gameObject);
                cubemapItem.Exists = MaterialPropertyAccess.GetTexture(
                    material,
                    descriptor.PropertyHandle) is Cubemap;
            };
            return cubemapItem;
        }


        internal static IEnumerable<RowModel> CreateExtensionTextureRows(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorTexturePropertyEditor editor)
        {
            var texture = PropertyRowBinding.Extension(new TexturePropertyRowModel(descriptor.DisplayName)
            {
                Changed = editor.Changed,
                Exists = editor.Exists,
                SelectInterpolable = editor.SelectInterpolable,
                Export = editor.Export ?? (() => { }),
                Import = editor.Import ?? (() => { }),
                Reset = editor.Reset ?? (() => { })
            }, context, descriptor);
            var transform = new TextureOffsetScaleRowModel
            {
                GameObject = context.Target.GameObject,
                Data = context.Target.Data,
                Material = context.Target.Material,
                Projector = context.Target.Projector,
                PropertyName = descriptor.PropertyName,
                PublicDescriptor = descriptor,
                Offset = editor.Offset,
                OriginalOffset = editor.OriginalOffset,
                OffsetOnChange = editor.OffsetChanged ?? (_ => { }),
                OffsetOnReset = editor.ResetOffset ?? (() => { }),
                Scale = editor.Scale,
                OriginalScale = editor.OriginalScale,
                ScaleOnChange = editor.ScaleChanged ?? (_ => { }),
                ScaleOnReset = editor.ResetScale ?? (() => { })
            };
            return new RowModel[] { texture, transform };
        }
    }
}
