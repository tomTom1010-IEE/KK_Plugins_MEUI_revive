using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using UnityEngine;
using XUnity.ResourceRedirector;
using static MaterialEditorAPI.MaterialAPI;

namespace MaterialEditorAPI
{
    public partial class MaterialEditorPluginBase
    {
        public partial class ShaderPropertyData
        {
            internal static bool TryParse(
                XmlElement propertyElement,
                Action<string> warning,
                out ShaderPropertyData propertyData,
                int schemaVersion = 2)
            {
                propertyData = null;
                if (propertyElement == null)
                    return false;

                var propertyName = propertyElement.GetAttribute("Name").Trim();
                if (propertyName.Length == 0)
                {
                    warning?.Invoke("A shader Property without a Name was ignored.");
                    return false;
                }

                var declaredPropertyType = propertyElement.GetAttribute("Type");
                ShaderPropertyType propertyType;
                string aliasEditorId;
                if (!TryParsePropertyType(
                        declaredPropertyType,
                        schemaVersion,
                        out propertyType,
                        out aliasEditorId))
                {
                    warning?.Invoke(
                        "Shader property '" + propertyName + "' has unknown Type '"
                        + declaredPropertyType + "' and was ignored.");
                    return false;
                }
                if (schemaVersion >= 2
                    && string.Equals(
                        declaredPropertyType == null
                            ? string.Empty
                            : declaredPropertyType.Trim(),
                        "Dropdown",
                        StringComparison.OrdinalIgnoreCase))
                {
                    warning?.Invoke(
                        "Shader property '" + propertyName
                        + "' uses Type 'Dropdown'; use Type 'Enum' with the Enums attribute.");
                }

                string min = null;
                string max = null;
                var range = propertyElement.GetAttribute("Range");
                if (!range.IsNullOrWhiteSpace())
                {
                    var rangeSplit = range.Split(',');
                    float parsedMinimum;
                    float parsedMaximum;
                    if (rangeSplit.Length == 2
                        && TryParseManifestFloat(rangeSplit[0], out parsedMinimum)
                        && TryParseManifestFloat(rangeSplit[1], out parsedMaximum))
                    {
                        min = parsedMinimum.ToString(CultureInfo.InvariantCulture);
                        max = parsedMaximum.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        warning?.Invoke(
                            "Shader property '" + propertyName + "' has invalid Range '"
                            + range + "'; the default slider range will be used.");
                    }
                }

                propertyData = new ShaderPropertyData(
                    propertyName,
                    propertyType,
                    propertyElement.GetAttribute("DefaultValue"),
                    propertyElement.GetAttribute("DefaultValueAssetBundle"),
                    propertyElement.GetAttribute("AnisoLevel"),
                    propertyElement.GetAttribute("FilterMode"),
                    propertyElement.GetAttribute("WrapMode"),
                    min,
                    max,
                    propertyElement.GetAttribute("Hidden"),
                    propertyElement.GetAttribute("Category"));

                var metadata = schemaVersion >= 2
                    ? ShaderPropertyMetadataParser.Parse(
                        propertyElement,
                        warning)
                    : new ShaderPropertyUiMetadata();
                var isCanonicalBoolean = schemaVersion >= 2
                    && string.Equals(
                        declaredPropertyType == null
                            ? string.Empty
                            : declaredPropertyType.Trim(),
                        "Boolean",
                        StringComparison.OrdinalIgnoreCase);
                if (isCanonicalBoolean)
                {
                    if (propertyElement.HasAttribute("OffValue")
                        || propertyElement.HasAttribute("OnValue"))
                    {
                        warning?.Invoke(
                            "Shader property '" + propertyName
                            + "' uses fixed Boolean values 0 and 1; OffValue/OnValue attributes are ignored. Use Invert=\"true\" to swap them.");
                    }
                    metadata.OffValue = metadata.Invert ? 1f : 0f;
                    metadata.OnValue = metadata.Invert ? 0f : 1f;
                }
                var hasExplicitEditor = schemaVersion >= 2
                    && !propertyElement.GetAttribute("Editor").IsNullOrWhiteSpace();
                if (!hasExplicitEditor
                    && metadata.EditorId.IsNullOrEmpty()
                    && !aliasEditorId.IsNullOrEmpty())
                {
                    metadata.EditorId = aliasEditorId;
                }
                if (!ShaderPropertyEditorPolicy.IsCompatible(
                        metadata.EditorId,
                        propertyType))
                {
                    warning?.Invoke(
                        "Shader property '" + propertyName + "' declares Editor '"
                        + propertyElement.GetAttribute("Editor") + "' for Type '"
                        + declaredPropertyType + "' (normalized backing Type '"
                        + propertyType + "'); its type editor will be used.");
                    metadata.EditorId = null;
                    metadata.VectorComponentCount = null;
                }
                if (metadata.EditorId == MaterialEditorPropertyEditorIds.Enum
                    && metadata.EnumOptions.Count == 0)
                {
                    warning?.Invoke(
                        "Shader property '" + propertyName + "' declares Type '"
                        + declaredPropertyType
                        + "' as an enum without a valid Enums attribute or Option elements; "
                        + "the Float editor will be used.");
                    metadata.EditorId = null;
                }
                propertyData.DisplayName = metadata.DisplayName.IsNullOrEmpty()
                    ? propertyName
                    : metadata.DisplayName;
                propertyData.HasExplicitDisplayName = schemaVersion >= 2
                    && !metadata.DisplayName.IsNullOrEmpty();
                propertyData.Order = metadata.Order;
                propertyData.CategoryOrder = metadata.CategoryOrder;
                propertyData.EditorId = metadata.EditorId;
                propertyData.TooltipText = metadata.TooltipText;
                propertyData.Group = metadata.Group;
                propertyData.ShowIf = metadata.ShowIf;
                propertyData.EnumOptions.AddRange(metadata.EnumOptions);
                propertyData.VectorComponentCount = metadata.VectorComponentCount;
                propertyData.Invert = metadata.Invert;
                propertyData.OffValue = metadata.OffValue;
                propertyData.OnValue = metadata.OnValue;
                if (schemaVersion >= 2)
                {
                    ApplyHierarchyMetadata(
                        propertyElement,
                        propertyData,
                        warning);
                }
                return true;
            }





            private static void ApplyHierarchyMetadata(
                XmlElement propertyElement,
                ShaderPropertyData propertyData,
                Action<string> warning)
            {
                var parent = propertyElement.ParentNode as XmlElement;
                XmlElement subcategoryElement = null;
                XmlElement categoryElement = null;

                if (HasElementName(parent, "Subcategory"))
                {
                    subcategoryElement = parent;
                    parent = parent.ParentNode as XmlElement;
                }

                if (HasElementName(parent, "Category"))
                    categoryElement = parent;

                if (subcategoryElement != null && categoryElement == null)
                {
                    warning?.Invoke(
                        "Shader property '" + propertyData.Name
                        + "' is inside a Subcategory without a parent Category; "
                        + "the Subcategory metadata was ignored.");
                    return;
                }

                if (categoryElement == null)
                    return;

                var categoryId = ReadHierarchyAttribute(categoryElement, "Id");
                if (categoryId == null)
                {
                    warning?.Invoke(
                        "Shader property '" + propertyData.Name
                        + "' is inside a Category without an Id; the nested "
                        + "Category metadata was ignored.");
                    return;
                }

                var declaredCategoryDisplayName =
                    ReadHierarchyAttribute(categoryElement, "DisplayName");
                var categoryDisplayName = declaredCategoryDisplayName
                                          ?? categoryId;
                propertyData.CategoryBeforeHierarchy = propertyData.Category;
                propertyData.CategoryId = categoryId;
                propertyData.CategoryDisplayName = categoryDisplayName;
                propertyData.HasExplicitCategoryDisplayName =
                    declaredCategoryDisplayName != null;
                propertyData.Category = categoryDisplayName;

                if (subcategoryElement == null)
                    return;

                var subcategoryId =
                    ReadHierarchyAttribute(subcategoryElement, "Id");
                if (subcategoryId == null)
                {
                    warning?.Invoke(
                        "Shader property '" + propertyData.Name
                        + "' is inside a Subcategory without an Id; the "
                        + "Subcategory metadata was ignored.");
                    return;
                }

                propertyData.SubcategoryId = subcategoryId;
                var declaredSubcategoryDisplayName =
                    ReadHierarchyAttribute(subcategoryElement, "DisplayName");
                propertyData.SubcategoryDisplayName =
                    declaredSubcategoryDisplayName ?? subcategoryId;
                propertyData.HasExplicitSubcategoryDisplayName =
                    declaredSubcategoryDisplayName != null;
            }

            private static bool HasElementName(
                XmlElement element,
                string expectedName)
            {
                return element != null
                       && string.Equals(
                           element.LocalName,
                           expectedName,
                           StringComparison.Ordinal);
            }

            private static string ReadHierarchyAttribute(
                XmlElement element,
                string attributeName)
            {
                if (element == null || !element.HasAttribute(attributeName))
                    return null;
                var value = element.GetAttribute(attributeName).Trim();
                return value.Length == 0 ? null : value;
            }

            private static bool TryParsePropertyType(
                string value,
                int schemaVersion,
                out ShaderPropertyType propertyType,
                out string aliasEditorId)
            {
                propertyType = default(ShaderPropertyType);
                aliasEditorId = null;
                if (value.IsNullOrWhiteSpace())
                    return false;

                var trimmedValue = value.Trim();
                if (schemaVersion >= 2)
                {
                    if (string.Equals(
                            trimmedValue,
                            "Boolean",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        propertyType = ShaderPropertyType.Float;
                        aliasEditorId = MaterialEditorPropertyEditorIds.Toggle;
                        return true;
                    }

                    if (string.Equals(
                            trimmedValue,
                            "Dropdown",
                            StringComparison.OrdinalIgnoreCase)
                        || string.Equals(
                            trimmedValue,
                            "Enum",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        propertyType = ShaderPropertyType.Float;
                        aliasEditorId = MaterialEditorPropertyEditorIds.Enum;
                        return true;
                    }
                }

                try
                {
                    var parsed = (ShaderPropertyType)Enum.Parse(
                        typeof(ShaderPropertyType),
                        trimmedValue,
                        true);
                    if (!Enum.IsDefined(typeof(ShaderPropertyType), parsed))
                        return false;
                    propertyType = parsed;
                    return true;
                }
                catch (ArgumentException)
                {
                    return false;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }

            private static bool TryParseManifestFloat(string value, out float result)
            {
                if ((float.TryParse(
                         value,
                         NumberStyles.Float,
                         CultureInfo.InvariantCulture,
                         out result)
                     || float.TryParse(value, out result))
                    && !float.IsNaN(result)
                    && !float.IsInfinity(result))
                {
                    return true;
                }

                result = 0f;
                return false;
            }

        }
    }
}
