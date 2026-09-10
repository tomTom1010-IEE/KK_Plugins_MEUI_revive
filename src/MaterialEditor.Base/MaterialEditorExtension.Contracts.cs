using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Features exposed by the semantic Material Editor extension API.
    /// </summary>
    [Flags]
    public enum MaterialEditorApiCapability
    {
        /// <summary>No optional capability.</summary>
        None = 0,
        /// <summary>Semantic label click notifications.</summary>
        LabelClickEvents = 1,
        /// <summary>Renderer, material, shader, and property selection notifications.</summary>
        SelectionEvents = 2,
        /// <summary>Registration of additional property descriptors.</summary>
        PropertyDescriptorProviders = 4,
        /// <summary>Registration of semantic property editors.</summary>
        PropertyEditors = 8,
        /// <summary>Stable facade for Material Editor edit and storage operations.</summary>
        EditServiceFacade = 16,
        /// <summary>English property tooltip metadata supplied by descriptors and shader catalogs.</summary>
        PropertyTooltips = 32,
        /// <summary>Float-backed enum property editors.</summary>
        EnumPropertyEditors = 128,
        /// <summary>Two-, three-, and four-component vector property editors.</summary>
        VectorPropertyEditors = 256,
        /// <summary>Conditional property visibility.</summary>
        ConditionalPropertyVisibility = 512,
        /// <summary>Float-backed toggle property editors.</summary>
        ToggleFloatPropertyEditors = 1024
    }

    /// <summary>
    /// Semantic target represented by a Material Editor selection notification.
    /// </summary>
    public enum MaterialEditorSelectionType
    {
        /// <summary>A renderer.</summary>
        Renderer,
        /// <summary>A material.</summary>
        Material,
        /// <summary>A shader.</summary>
        Shader,
        /// <summary>A shader or extension property.</summary>
        Property
    }

    /// <summary>
    /// How a semantic selection changed.
    /// </summary>
    public enum MaterialEditorSelectionAction
    {
        /// <summary>The target was selected.</summary>
        Selected,
        /// <summary>The target was deselected.</summary>
        Deselected,
        /// <summary>The target was activated without changing a persistent selection set.</summary>
        Activated
    }

    /// <summary>
    /// Stable Material Editor target context. This type does not expose UI objects.
    /// </summary>
    public sealed class MaterialEditorTargetContext
    {
        internal MaterialEditorTargetContext(
            GameObject gameObject,
            object data,
            Renderer renderer,
            Material material,
            Projector projector,
            MaterialEditorEditService editService)
        {
            GameObject = gameObject;
            Data = data;
            Renderer = renderer;
            Material = material;
            Projector = projector;
            EditService = editService;
        }

        /// <summary>Root object currently being edited.</summary>
        public GameObject GameObject { get; }
        /// <summary>Opaque storage context supplied to Material Editor.</summary>
        public object Data { get; }
        /// <summary>Renderer represented by this context, when applicable.</summary>
        public Renderer Renderer { get; }
        /// <summary>Material represented by this context, when applicable.</summary>
        public Material Material { get; }
        /// <summary>Projector represented by this context, when applicable.</summary>
        public Projector Projector { get; }
        /// <summary>Edit and persistence operations bound to the current root object and data.</summary>
        public MaterialEditorEditService EditService { get; }
    }

    /// <summary>
    /// Context passed to custom property descriptor providers and editor factories.
    /// </summary>
    public sealed class MaterialEditorPropertyContext
    {
        internal MaterialEditorPropertyContext(
            MaterialEditorTargetContext target,
            string materialName,
            string shaderName)
        {
            Target = target;
            MaterialName = materialName ?? string.Empty;
            ShaderName = shaderName ?? string.Empty;
        }

        /// <summary>Target and edit service for the current material.</summary>
        public MaterialEditorTargetContext Target { get; }
        /// <summary>Formatted material name shown by Material Editor.</summary>
        public string MaterialName { get; }
        /// <summary>Formatted shader name used by Material Editor.</summary>
        public string ShaderName { get; }
    }

    /// <summary>
    /// UI-independent description of an extension property.
    /// </summary>
    public sealed class MaterialEditorPropertyDescriptor
    {
        /// <summary>
        /// Create a property descriptor.
        /// </summary>
        /// <param name="id">Stable identifier unique within the registering provider.</param>
        /// <param name="displayName">Label shown in Material Editor.</param>
        /// <param name="editorId">Semantic editor identifier.</param>
        public MaterialEditorPropertyDescriptor(string id, string displayName, string editorId)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A property descriptor ID is required.", nameof(id));
            if (string.IsNullOrEmpty(editorId))
                throw new ArgumentException("A property editor ID is required.", nameof(editorId));

            Id = id;
            DisplayName = string.IsNullOrEmpty(displayName) ? id : displayName;
            EditorId = editorId;
            PropertyName = id;
            Category = string.Empty;
            Group = string.Empty;
            EnumOptions = new List<MaterialEditorEnumOption>();
            OffValue = 0f;
            OnValue = 1f;
        }

        /// <summary>Stable identifier unique within the registering provider.</summary>
        public string Id { get; }
        /// <summary>Label shown in Material Editor.</summary>
        public string DisplayName { get; set; }
        /// <summary>Semantic editor identifier.</summary>
        public string EditorId { get; }
        /// <summary>Backing shader property name, without the leading underscore.</summary>
        public string PropertyName { get; set; }
        /// <summary>Optional collapsible category. Extension categories are appended after manifest categories.</summary>
        public string Category { get; set; }
        /// <summary>Sort order within the extension category.</summary>
        public int Order { get; set; }
        /// <summary>Optional logical group within the category.</summary>
        public string Group { get; set; }
        /// <summary>Optional condition controlling whether the property is visible.</summary>
        public MaterialEditorPropertyCondition VisibilityCondition { get; set; }
        /// <summary>Options used by the built-in enum editor.</summary>
        public IList<MaterialEditorEnumOption> EnumOptions { get; set; }
        /// <summary>Number of vector components to display, from two through four.</summary>
        public int? VectorComponentCount { get; set; }
        /// <summary>Numeric value represented by the off state of a float-backed toggle.</summary>
        public float OffValue { get; set; }
        /// <summary>Numeric value represented by the on state of a float-backed toggle.</summary>
        public float OnValue { get; set; }
        /// <summary>Optional provider-owned metadata returned unchanged to the editor factory.</summary>
        public object Tag { get; set; }
        /// <summary>Optional lower bound used by the built-in float editor.</summary>
        public float? Minimum { get; set; }
        /// <summary>Optional upper bound used by the built-in float editor.</summary>
        public float? Maximum { get; set; }
        /// <summary>Optional English tooltip shown when hovering the property label.</summary>
        public string TooltipText { get; set; }

    }

    /// <summary>
    /// Built-in semantic editor identifiers available to custom descriptors.
    /// </summary>
    public static class MaterialEditorPropertyEditorIds
    {
        /// <summary>Float slider and numeric input.</summary>
        public const string Float = "materialeditor.float";
        /// <summary>RGBA inputs and color picker.</summary>
        public const string Color = "materialeditor.color";
        /// <summary>Boolean toggle.</summary>
        public const string Boolean = "materialeditor.boolean";
        /// <summary>Texture import/export plus offset and scale.</summary>
        public const string Texture = "materialeditor.texture";
        /// <summary>Native Cubemap import/export.</summary>
        public const string Cubemap = "materialeditor.cubemap";
        /// <summary>Float-backed enum dropdown.</summary>
        public const string Enum = "materialeditor.enum";
        /// <summary>Two-component vector input.</summary>
        public const string Vector2 = "materialeditor.vector2";
        /// <summary>Three-component vector input.</summary>
        public const string Vector3 = "materialeditor.vector3";
        /// <summary>Four-component vector input.</summary>
        public const string Vector4 = "materialeditor.vector4";
        /// <summary>Float-backed toggle.</summary>
        public const string Toggle = "materialeditor.toggle";
    }

    /// <summary>
    /// Base class for semantic property editors. Internal row and control types are deliberately not exposed.
    /// </summary>
    public abstract class MaterialEditorPropertyEditor
    {
        /// <summary>Initialize a semantic property editor.</summary>
        protected MaterialEditorPropertyEditor()
        {
        }
    }

    /// <summary>Semantic float property editor.</summary>
    public sealed class MaterialEditorFloatPropertyEditor : MaterialEditorPropertyEditor
    {
        /// <summary>Create a float editor definition.</summary>
        public MaterialEditorFloatPropertyEditor(
            float value,
            float originalValue,
            Action<float> valueChanged,
            Action reset)
        {
            Value = value;
            OriginalValue = originalValue;
            ValueChanged = valueChanged ?? throw new ArgumentNullException(nameof(valueChanged));
            Reset = reset ?? throw new ArgumentNullException(nameof(reset));
            Maximum = 1f;
        }

        /// <summary>Current value.</summary>
        public float Value { get; }
        /// <summary>Original value used by Reset and changed-state display.</summary>
        public float OriginalValue { get; }
        /// <summary>Slider minimum.</summary>
        public float Minimum { get; set; }
        /// <summary>Slider maximum.</summary>
        public float Maximum { get; set; }
        /// <summary>Called after the user changes the value.</summary>
        public Action<float> ValueChanged { get; }
        /// <summary>Called when the value returns to its original state.</summary>
        public Action Reset { get; }
        /// <summary>Optional Timeline interpolation selection action.</summary>
        public Action SelectInterpolable { get; set; }
    }

    /// <summary>Semantic float-backed enum property editor.</summary>
    public sealed class MaterialEditorEnumPropertyEditor : MaterialEditorPropertyEditor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialEditorEnumPropertyEditor"/> class.
        /// </summary>
        /// <param name="value">Current value of the enum property.</param>
        /// <param name="originalValue">Original value used by Reset and changed-state display.</param>
        /// <param name="options">Enumeration options to display in the dropdown.</param>
        /// <param name="valueChanged">Called after the user selects a value.</param>
        /// <param name="reset">Called when the value returns to its original state.</param>
        public MaterialEditorEnumPropertyEditor(
            float value,
            float originalValue,
            IEnumerable<MaterialEditorEnumOption> options,
            Action<float> valueChanged,
            Action reset)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            Options = new List<MaterialEditorEnumOption>();
            foreach (var option in options)
                if (option != null)
                    Options.Add(option);
            if (Options.Count == 0)
                throw new ArgumentException("At least one enum option is required.", nameof(options));

            Value = value;
            OriginalValue = originalValue;
            ValueChanged = valueChanged ?? throw new ArgumentNullException(nameof(valueChanged));
            Reset = reset ?? throw new ArgumentNullException(nameof(reset));
        }

        /// <summary>Current value of the enum property.</summary>
        public float Value { get; }
        /// <summary>Original value used by Reset and changed-state display.</summary>
        public float OriginalValue { get; }
        /// <summary>Whether multiple extension targets currently have different values.</summary>
        public bool IsMixed { get; set; }
        /// <summary>Enumeration options to display in the dropdown.</summary>
        public IList<MaterialEditorEnumOption> Options { get; }
        /// <summary>Called after the user selects a value.</summary>
        public Action<float> ValueChanged { get; }
        /// <summary>Called when the value returns to its original state.</summary>
        public Action Reset { get; }
        /// <summary>Optional Timeline interpolation selection action.</summary>
        public Action SelectInterpolable { get; set; }
    }

    /// <summary>Semantic vector property editor.</summary>
    public sealed class MaterialEditorVectorPropertyEditor : MaterialEditorPropertyEditor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialEditorVectorPropertyEditor"/> class.
        /// </summary>
        /// <param name="value">Current four-component backing value.</param>
        /// <param name="originalValue">Original four-component backing value.</param>
        /// <param name="componentCount">Number of components shown by the editor (2-4).</param>
        /// <param name="valueChanged">Called after the user changes the vector.</param>
        /// <param name="reset">Called when the vector returns to its original state.</param>
        public MaterialEditorVectorPropertyEditor(
            Vector4 value,
            Vector4 originalValue,
            int componentCount,
            Action<Vector4> valueChanged,
            Action reset)
        {
            if (componentCount < 2 || componentCount > 4)
                throw new ArgumentOutOfRangeException(nameof(componentCount));

            Value = value;
            OriginalValue = originalValue;
            ComponentCount = componentCount;
            MixedComponents = new bool[4];
            ValueChanged = valueChanged ?? throw new ArgumentNullException(nameof(valueChanged));
            Reset = reset ?? throw new ArgumentNullException(nameof(reset));
        }

        /// <summary>Current four-component backing value.</summary>
        public Vector4 Value { get; }
        /// <summary>Original four-component backing value.</summary>
        public Vector4 OriginalValue { get; }
        /// <summary>Number of components shown by the editor.</summary>
        public int ComponentCount { get; }
        /// <summary>Per-component mixed state for multi-target extension editors.</summary>
        public IList<bool> MixedComponents { get; set; }
        /// <summary>
        /// Optional per-component callback for multi-target editors. When set,
        /// changing one component does not require overwriting mixed components.
        /// </summary>
        public Action<int, float> ComponentChanged { get; set; }
        /// <summary>Called after the user changes the vector.</summary>
        public Action<Vector4> ValueChanged { get; }
        /// <summary>Called when the vector returns to its original state.</summary>
        public Action Reset { get; }
        /// <summary>Optional Timeline interpolation selection action.</summary>
        public Action SelectInterpolable { get; set; }
    }

    /// <summary>Semantic float-backed toggle property editor.</summary>
    public sealed class MaterialEditorTogglePropertyEditor : MaterialEditorPropertyEditor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialEditorTogglePropertyEditor"/> class.
        /// </summary>
        /// <param name="value">Current value of the toggle property.</param>
        /// <param name="originalValue">Original value used by Reset and changed-state display.</param>
        /// <param name="offValue">Numeric value represented by the off state of the toggle.</param>
        /// <param name="onValue">Numeric value represented by the on state of the toggle.</param>
        /// <param name="valueChanged">Called after the user toggles the value.</param>
        /// <param name="reset">Called when the value returns to its original state.</param>
        public MaterialEditorTogglePropertyEditor(
            float value,
            float originalValue,
            float offValue,
            float onValue,
            Action<float> valueChanged,
            Action reset)
        {
            Value = value;
            OriginalValue = originalValue;
            OffValue = offValue;
            OnValue = onValue;
            ValueChanged = valueChanged ?? throw new ArgumentNullException(nameof(valueChanged));
            Reset = reset ?? throw new ArgumentNullException(nameof(reset));
        }

        /// <summary>Current value of the toggle property.</summary>
        public float Value { get; }
        /// <summary>Original value used by Reset and changed-state display.</summary>
        public float OriginalValue { get; }
        /// <summary>Whether multiple extension targets currently have different values.</summary>
        public bool IsMixed { get; set; }
        /// <summary>Numeric value represented by the off state of the toggle.</summary>
        public float OffValue { get; }
        /// <summary>Numeric value represented by the on state of the toggle.</summary>
        public float OnValue { get; }
        /// <summary>Called after the user toggles the value.</summary>
        public Action<float> ValueChanged { get; }
        /// <summary>Called when the value returns to its original state.</summary>
        public Action Reset { get; }
        /// <summary>Optional Timeline interpolation selection action.</summary>
        public Action SelectInterpolable { get; set; }
    }

    /// <summary>Semantic color property editor.</summary>
    public sealed class MaterialEditorColorPropertyEditor : MaterialEditorPropertyEditor
    {
        /// <summary>Create a color editor definition.</summary>
        public MaterialEditorColorPropertyEditor(
            Color value,
            Color originalValue,
            Action<Color> valueChanged,
            Action reset)
        {
            Value = value;
            OriginalValue = originalValue;
            ValueChanged = valueChanged ?? throw new ArgumentNullException(nameof(valueChanged));
            Reset = reset ?? throw new ArgumentNullException(nameof(reset));
        }

        /// <summary>Current color.</summary>
        public Color Value { get; }
        /// <summary>Original color.</summary>
        public Color OriginalValue { get; }
        /// <summary>Called after the user changes the color.</summary>
        public Action<Color> ValueChanged { get; }
        /// <summary>Called when the color returns to its original state.</summary>
        public Action Reset { get; }
        /// <summary>Optional Timeline interpolation selection action.</summary>
        public Action SelectInterpolable { get; set; }
    }

    /// <summary>Semantic boolean property editor.</summary>
    public sealed class MaterialEditorBooleanPropertyEditor : MaterialEditorPropertyEditor
    {
        /// <summary>Create a boolean editor definition.</summary>
        public MaterialEditorBooleanPropertyEditor(
            bool value,
            bool originalValue,
            Action<bool> valueChanged,
            Action reset)
        {
            Value = value;
            OriginalValue = originalValue;
            ValueChanged = valueChanged ?? throw new ArgumentNullException(nameof(valueChanged));
            Reset = reset ?? throw new ArgumentNullException(nameof(reset));
        }

        /// <summary>Current value.</summary>
        public bool Value { get; }
        /// <summary>Original value.</summary>
        public bool OriginalValue { get; }
        /// <summary>Called after the user changes the value.</summary>
        public Action<bool> ValueChanged { get; }
        /// <summary>Called when the value returns to its original state.</summary>
        public Action Reset { get; }
    }

    /// <summary>Semantic texture property editor.</summary>
    public sealed class MaterialEditorTexturePropertyEditor : MaterialEditorPropertyEditor
    {
        /// <summary>Whether the texture differs from its original state.</summary>
        public bool Changed { get; set; }
        /// <summary>Whether a texture is currently assigned.</summary>
        public bool Exists { get; set; }
        /// <summary>Current texture offset.</summary>
        public Vector2 Offset { get; set; }
        /// <summary>Original texture offset.</summary>
        public Vector2 OriginalOffset { get; set; }
        /// <summary>Current texture scale.</summary>
        public Vector2 Scale { get; set; }
        /// <summary>Original texture scale.</summary>
        public Vector2 OriginalScale { get; set; }
        /// <summary>Optional texture export action.</summary>
        public Action Export { get; set; }
        /// <summary>Optional texture import action.</summary>
        public Action Import { get; set; }
        /// <summary>Optional texture reset action.</summary>
        public Action Reset { get; set; }
        /// <summary>Called after the user changes the texture offset.</summary>
        public Action<Vector2> OffsetChanged { get; set; }
        /// <summary>Called when the texture offset returns to its original state.</summary>
        public Action ResetOffset { get; set; }
        /// <summary>Called after the user changes the texture scale.</summary>
        public Action<Vector2> ScaleChanged { get; set; }
        /// <summary>Called when the texture scale returns to its original state.</summary>
        public Action ResetScale { get; set; }
        /// <summary>Optional Timeline interpolation selection action.</summary>
        public Action SelectInterpolable { get; set; }
    }

    /// <summary>Provides extension properties for one material.</summary>
    public delegate IEnumerable<MaterialEditorPropertyDescriptor> MaterialEditorPropertyDescriptorProvider(
        MaterialEditorPropertyContext context);

    /// <summary>Creates a semantic editor for one extension property.</summary>
    public delegate MaterialEditorPropertyEditor MaterialEditorPropertyEditorFactory(
        MaterialEditorPropertyContext context,
        MaterialEditorPropertyDescriptor descriptor);

    /// <summary>Semantic Material Editor selection notification.</summary>
    public sealed class MaterialEditorSelectionEventArgs : EventArgs
    {
        internal MaterialEditorSelectionEventArgs(
            MaterialEditorSelectionType selectionType,
            MaterialEditorSelectionAction action,
            string name,
            MaterialEditorTargetContext context,
            MaterialEditorPropertyDescriptor property)
        {
            SelectionType = selectionType;
            Action = action;
            Name = name ?? string.Empty;
            Context = context;
            Property = property;
        }

        /// <summary>Type of semantic target.</summary>
        public MaterialEditorSelectionType SelectionType { get; }
        /// <summary>Selection operation that occurred.</summary>
        public MaterialEditorSelectionAction Action { get; }
        /// <summary>Renderer, material, shader, or property name.</summary>
        public string Name { get; }
        /// <summary>Stable target and edit-service context.</summary>
        public MaterialEditorTargetContext Context { get; }
        /// <summary>Property descriptor for property selections, when available.</summary>
        public MaterialEditorPropertyDescriptor Property { get; }
    }
}
