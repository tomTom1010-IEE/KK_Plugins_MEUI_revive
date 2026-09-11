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
        private struct CharacterRestoreScope
        {
            internal readonly bool Body;
            private readonly bool _clothes;
            private readonly bool _accessories;
            private readonly bool _hair;

            internal CharacterRestoreScope(bool clothes, bool accessories, bool hair, bool body)
            {
                _clothes = clothes;
                _accessories = accessories;
                _hair = hair;
                Body = body;
            }

            internal bool Includes(ObjectType type, int coordinate, int currentCoordinate)
            {
                if (type == ObjectType.Clothing && !_clothes) return false;
                if (type == ObjectType.Accessory && !_accessories) return false;
                if (type == ObjectType.Hair && !_hair) return false;
                return (type != ObjectType.Clothing && type != ObjectType.Accessory)
                    || coordinate == currentCoordinate;
            }
        }

        private void RestoreMaterialCopyList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < MaterialCopyList.Count; i++)
            {
                var property = MaterialCopyList[i];
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;

                CopyMaterial(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.MaterialCopyName);
            }
        }

        private void RestoreMaterialNamePropertyList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < MaterialNamePropertyList.Count; i++)
            {
                var property = MaterialNamePropertyList[i];
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;

                MaterialAPI.SetName(FindGameObject(property.ObjectType, property.Slot), property.Renderer, property.MaterialName, property.Value);
            }
        }

        private void RestoreMaterialShaderList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < MaterialShaderList.Count; i++)
            {
                var property = MaterialShaderList[i];
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;

#if KK || EC || KKS
                if (property.ObjectType == ObjectType.Character && MaterialEditorPlugin.EyeMaterials.Contains(property.MaterialName))
                {
                    SetShader(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.ShaderName, true);
                }
                else
#endif
                {
                    SetShader(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.ShaderName);
                }
                SetRenderQueue(FindGameObject(property.ObjectType, property.Slot), property.MaterialName, property.RenderQueue);
            }
        }

        private void RestoreRendererPropertyList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < RendererPropertyList.Count; i++)
            {
                var property = RendererPropertyList[i];
#if KK
                if (property.Property == RendererProperties.UpdateWhenOffscreen) continue;
#endif
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;

                MaterialAPI.SetRendererProperty(FindGameObject(property.ObjectType, property.Slot), property.RendererName, property.Property, property.Value);
            }
        }

        private void RestoreMaterialFloatPropertyList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < MaterialFloatPropertyList.Count; i++)
            {
                var property = MaterialFloatPropertyList[i];
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;

                SetFloat(go, property.MaterialName, property.Property, float.Parse(property.Value));
            }
        }

        private void RestoreMaterialKeywordPropertyList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < MaterialKeywordPropertyList.Count; i++)
            {
                var property = MaterialKeywordPropertyList[i];
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;

                SetKeyword(go, property.MaterialName, property.Property, property.Value);
            }
        }

        private void RestoreMaterialColorPropertyList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < MaterialColorPropertyList.Count; i++)
            {
                var property = MaterialColorPropertyList[i];
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;

                SetColor(go, property.MaterialName, property.Property, property.Value);
            }
        }

        private void RestoreMaterialVectorPropertyList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < MaterialVectorPropertyList.Count; i++)
            {
                var property = MaterialVectorPropertyList[i];
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                var go = FindGameObject(property.ObjectType, property.Slot);
                if (Instance.CheckBlacklist(property.MaterialName, property.Property)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;

                SetVector(go, property.MaterialName, property.Property, property.Value);
            }
        }

        private void RestoreProjectorPropertyList(CharacterRestoreScope scope)
        {
            for (var i = 0; i < ProjectorPropertyList.Count; i++)
            {
                var property = ProjectorPropertyList[i];
                if (!scope.Includes(property.ObjectType, property.CoordinateIndex, CurrentCoordinateIndex)) continue;
                if (property.ObjectType == ObjectType.Character && !scope.Body) continue;

                MaterialAPI.SetProjectorProperty(FindGameObject(property.ObjectType, property.Slot), property.ProjectorName, property.Property, float.Parse(property.Value));
            }
        }
    }
}
