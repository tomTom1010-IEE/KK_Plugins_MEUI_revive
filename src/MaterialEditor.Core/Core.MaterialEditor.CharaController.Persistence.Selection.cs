using System;
using System.Collections.Generic;
using KKAPI.Maker;

namespace KK_Plugins.MaterialEditor
{
    public partial class MaterialEditorCharaController
    {
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
