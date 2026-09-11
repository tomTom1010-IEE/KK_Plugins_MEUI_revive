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
    public partial class MaterialEditorCharaController
    {
        private void RestoreMaterialTexturePropertyList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < MaterialTexturePropertyList.Count; i++)
            {
                var property = MaterialTexturePropertyList[i];
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;

                SetTextureWithProperty(go, property);
                SetTextureOffset(go, property.MaterialName, property.Property, property.Offset);
                SetTextureScale(go, property.MaterialName, property.Property, property.Scale);
            }
        }

        private void RestoreMaterialCubemapPropertyList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < MaterialCubemapPropertyList.Count; i++)
            {
                var property = MaterialCubemapPropertyList[i];
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;

                SetCubemapWithProperty(go, property);
            }
        }
    }
}
