using System.Collections.Generic;
using System.Globalization;

namespace MaterialEditorAPI
{
    /// <summary>One numeric option displayed by an enum property editor.</summary>
    public sealed class MaterialEditorEnumOption
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialEditorEnumOption"/> class.
        /// </summary>
        /// <param name="value">Numeric value associated with this option.</param>
        /// <param name="displayName">Display name for this option.</param>
        public MaterialEditorEnumOption(float value, string displayName)
        {
            Value = value;
            DisplayName = string.IsNullOrEmpty(displayName)
                ? value.ToString(CultureInfo.InvariantCulture)
                : displayName;
        }

        /// <summary>Numeric value associated with this option.</summary>
        public float Value { get; }
        /// <summary>Display name for this option.</summary>
        public string DisplayName { get; }
    }

    /// <summary>
    /// Unity-independent metadata parsed for one manifest property.
    /// </summary>
    internal sealed class ShaderPropertyUiMetadata
    {
        internal string DisplayName;
        internal int? Order;
        internal int? CategoryOrder;
        internal string EditorId;
        internal string TooltipText;
        internal string Group;
        internal MaterialEditorPropertyCondition ShowIf;
        internal readonly List<MaterialEditorEnumOption> EnumOptions =
            new List<MaterialEditorEnumOption>();
        internal int? VectorComponentCount;
        internal bool Invert;
        internal float OffValue;
        internal float OnValue = 1f;
    }
}
