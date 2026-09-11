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
        /// <summary>
        /// Represents data for a shader, including its name, shader object, render queue, and optimization flag.
        /// </summary>
        public class ShaderData
        {
            /// <summary>
            /// Name of the shader.
            /// </summary>
            public string ShaderName;
            /// <summary>
            /// Shader object.
            /// </summary>
            public Shader Shader;
            /// <summary>
            /// Render queue value for the shader. Null if not specified.
            /// </summary>
            public int? RenderQueue;
            /// <summary>
            /// Indicates whether shader optimization is enabled.
            /// </summary>
            public bool ShaderOptimization;

            /// <summary>
            /// Initializes a new instance of the <see cref="ShaderData"/> class.
            /// </summary>
            /// <param name="shader">The shader object.</param>
            /// <param name="shaderName">The name of the shader.</param>
            /// <param name="renderQueue">The render queue value as a string. Defaults to an empty string.</param>
            /// <param name="shaderOptimization">The shader optimization flag as a string. Defaults to null.</param>
            public ShaderData(Shader shader, string shaderName, string renderQueue = "", string shaderOptimization = null)
            {
                Shader = shader;
                ShaderName = shaderName;

                if (renderQueue.IsNullOrEmpty())
                    RenderQueue = null;
                else if (int.TryParse(renderQueue, out int result))
                    RenderQueue = result;
                else
                    RenderQueue = null;

                if (bool.TryParse(shaderOptimization, out bool shaderOptimizationBool))
                    ShaderOptimization = shaderOptimizationBool;
                else
                    ShaderOptimization = true;
            }
        }

        /// <summary>
        /// Represents data for a shader property, including its name, type, default values, visibility, range, and category.
        /// </summary>
        public partial class ShaderPropertyData
        {
            /// <summary>
            /// Name of the shader property.
            /// </summary>
            public string Name;
            /// <summary>
            /// Type of the shader property.
            /// </summary>
            public ShaderPropertyType Type;
            /// <summary>
            /// Default value of the shader property.
            /// </summary>
            public string DefaultValue;
            /// <summary>
            /// Default value of the shader property when loaded from an asset bundle, like a texture.
            /// </summary>
            public string DefaultValueAssetBundle;
            /// <summary>
            /// Should only be used with texture properties. The `anisoLevel` of the texture, 0-16.
            /// </summary>
            public int? AnisoLevel;
            /// <summary>
            /// Should only be used with texture properties. The `filterMode` of the texture.
            /// </summary>
            public FilterMode? FilterMode;
            /// <summary>
            /// Should only be used with texture properties. The `wrapMode` of the texture.
            /// </summary>
            public TextureWrapMode? WrapMode;
            /// <summary>
            /// Should only be used with float properties. Minimum value displayed on the slider, if applicable.
            /// </summary>
            public float? MinValue;
            /// <summary>
            /// Should only be used with float properties. Maximum value displayed on the slider, if applicable.
            /// </summary>
            public float? MaxValue;
            /// <summary>
            /// Indicates whether the shader property is hidden.
            /// </summary>
            public bool Hidden;
            /// <summary>
            /// Category of the shader property.
            /// </summary>
            public string Category;
            // Preserve the flat Category attribute when a nested declaration
            // is cloned into the shared default-property catalog.
            internal string CategoryBeforeHierarchy;
            // Stable presentation hierarchy read from optional schema-2
            // Category/Subcategory parents. Category remains the flat
            // grouping/display value for consumers without hierarchy support.
            internal string CategoryId;
            internal string CategoryDisplayName;
            internal bool HasExplicitCategoryDisplayName;
            internal string SubcategoryId;
            internal string SubcategoryDisplayName;
            internal bool HasExplicitSubcategoryDisplayName;
            internal int? CategoryOrder;
            internal int DeclarationOrder;
            /// <summary>
            /// Optional label shown by Material Editor. Defaults to <see cref="Name"/>.
            /// </summary>
            public string DisplayName;
            internal bool HasExplicitDisplayName;
            /// <summary>
            /// Optional explicit ordering value from schema 2 metadata.
            /// </summary>
            public int? Order;
            /// <summary>
            /// Optional semantic editor identifier. A null value uses the editor implied by <see cref="Type"/>.
            /// </summary>
            public string EditorId;
            /// <summary>
            /// Optional inline English tooltip.
            /// </summary>
            public string TooltipText;
            /// <summary>
            /// Optional logical group identifier.
            /// </summary>
            public string Group;
            /// <summary>
            /// Optional condition controlling whether the property is shown.
            /// </summary>
            public MaterialEditorPropertyCondition ShowIf;
            /// <summary>
            /// Options used by an enum property editor.
            /// </summary>
            public List<MaterialEditorEnumOption> EnumOptions;
            /// <summary>
            /// Number of components shown by a vector editor, when specified.
            /// </summary>
            public int? VectorComponentCount;
            internal bool Invert;
            /// <summary>
            /// Numeric value written for the off state of a float-backed toggle.
            /// </summary>
            public float OffValue;
            /// <summary>
            /// Numeric value written for the on state of a float-backed toggle.
            /// </summary>
            public float OnValue;

            /// <summary>
            /// Initializes a new instance of the <see cref="ShaderPropertyData"/> class.
            /// </summary>
            /// <param name="name">Name of the shader property.</param>
            /// <param name="type">Type of the shader property.</param>
            /// <param name="defaultValue">Default value of the shader property.</param>
            /// <param name="defaultValueAB">Default value of the shader property when loaded from an asset bundle, like a texture.</param>
            /// <param name="anisoLevel">Should only be used with texture properties. The `anisoLevel` of the texture, 0-16.</param>
            /// <param name="filterMode">Should only be used with texture properties. The `filterMode` of the texture.</param>
            /// <param name="wrapMode">Should only be used with texture properties. The `wrapMode` of the texture.</param>
            /// <param name="minValue">Should only be used with float properties. Minimum value displayed on the slider, if applicable.</param>
            /// <param name="maxValue">Should only be used with float properties. Maximum value displayed on the slider, if applicable.</param>
            /// <param name="hidden">Indicates whether the shader property is hidden.</param>
            /// <param name="category">Category of the shader property.</param>
            public ShaderPropertyData(
                string name, ShaderPropertyType type,
                string defaultValue = null, string defaultValueAB = null,
                string anisoLevel = null, string filterMode = null, string wrapMode = null,
                string minValue = null, string maxValue = null,
                string hidden = null, string category = null
                )
            {
                Name = name;
                Type = type;
                DisplayName = name;
                EnumOptions = new List<MaterialEditorEnumOption>();
                OffValue = 0f;
                OnValue = 1f;
                DefaultValue = defaultValue.IsNullOrEmpty() ? null : defaultValue;
                DefaultValueAssetBundle = defaultValueAB.IsNullOrEmpty() ? null : defaultValueAB;

                if (!anisoLevel.IsNullOrWhiteSpace())
                {
                    int.TryParse(anisoLevel, out int outAnisoLevel);
                    AnisoLevel = Mathf.Clamp(outAnisoLevel, 0, 16);
                }

                if (!filterMode.IsNullOrWhiteSpace())
                {
                    int.TryParse(filterMode, out int outFilterMode);
                    if (Enum.IsDefined(typeof(FilterMode), outFilterMode)) FilterMode = (FilterMode)outFilterMode;
                }

                if (!wrapMode.IsNullOrWhiteSpace())
                {
                    int.TryParse(wrapMode, out int outWrapMode);
                    if (Enum.IsDefined(typeof(TextureWrapMode), outWrapMode)) WrapMode = (TextureWrapMode)outWrapMode;
                }

                if (!minValue.IsNullOrWhiteSpace() && !maxValue.IsNullOrWhiteSpace())
                {
                    if (TryParseManifestFloat(minValue, out float min)
                        && TryParseManifestFloat(maxValue, out float max))
                    {
                        MinValue = min;
                        MaxValue = max;
                    }
                }

                Hidden = bool.TryParse(hidden, out bool result) && result;
                Category = category;
            }

            internal ShaderPropertyData WithoutHierarchyForDefaultFallback()
            {
                if (string.IsNullOrEmpty(CategoryId)
                    && string.IsNullOrEmpty(SubcategoryId))
                    return this;

                var fallback = (ShaderPropertyData)MemberwiseClone();
                fallback.CategoryId = null;
                fallback.CategoryDisplayName = null;
                fallback.HasExplicitCategoryDisplayName = false;
                fallback.SubcategoryId = null;
                fallback.SubcategoryDisplayName = null;
                fallback.HasExplicitSubcategoryDisplayName = false;
                fallback.Category = CategoryBeforeHierarchy;
                fallback.CategoryBeforeHierarchy = null;
                return fallback;
            }

            internal ShaderPropertyData WithoutConditionsForUiFallback()
            {
                if (ShowIf == null)
                    return this;

                var fallback = (ShaderPropertyData)MemberwiseClone();
                fallback.ShowIf = null;
                return fallback;
            }

        }
    }
}
