using System.Collections.Generic;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed class ExtensionPropertyRowBuilder
    {
        private readonly MaterialEditorPresentationActions _actions;

        internal ExtensionPropertyRowBuilder(
            MaterialEditorPresentationActions actions)
        {
            _actions = actions;
        }

        internal IEnumerable<RowModel> CreateRows(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorPropertyEditor editor)
        {
            var floatEditor = editor as MaterialEditorFloatPropertyEditor;
            if (floatEditor != null)
                return new[] { CreateExtensionFloatRow(context, descriptor, floatEditor) };

            var colorEditor = editor as MaterialEditorColorPropertyEditor;
            if (colorEditor != null)
                return new[] { CreateExtensionColorRow(context, descriptor, colorEditor) };

            var booleanEditor = editor as MaterialEditorBooleanPropertyEditor;
            if (booleanEditor != null)
                return new[] { CreateExtensionBooleanRow(context, descriptor, booleanEditor) };

            var textureEditor = editor as MaterialEditorTexturePropertyEditor;
            if (textureEditor != null)
                return PropertyTextureRowBuilder.CreateExtensionTextureRows(context, descriptor, textureEditor);

            var enumEditor = editor as MaterialEditorEnumPropertyEditor;
            if (enumEditor != null)
                return new[] { CreateExtensionEnumRow(context, descriptor, enumEditor) };

            var vectorEditor = editor as MaterialEditorVectorPropertyEditor;
            if (vectorEditor != null)
                return new[] { CreateExtensionVectorRow(context, descriptor, vectorEditor) };

            var toggleEditor = editor as MaterialEditorTogglePropertyEditor;
            if (toggleEditor != null)
                return new[] { CreateExtensionToggleRow(context, descriptor, toggleEditor) };

            MaterialEditorPluginBase.Logger?.LogWarning(
                $"Property editor '{descriptor.EditorId}' returned an unsupported editor type.");
            return new RowModel[0];
        }

        private static FloatPropertyRowModel CreateExtensionFloatRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorFloatPropertyEditor editor)
        {
            return PropertyRowBinding.Extension(new FloatPropertyRowModel(descriptor.DisplayName)
            {
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                HasRange = true,
                SliderMinimum = editor.Minimum,
                SliderMaximum = editor.Maximum,
                SelectInterpolable = editor.SelectInterpolable,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            }, context, descriptor);
        }

        private ColorPropertyRowModel CreateExtensionColorRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorColorPropertyEditor editor)
        {
            return PropertyRowBinding.Extension(new ColorPropertyRowModel(descriptor.DisplayName)
            {
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                SelectInterpolable = editor.SelectInterpolable,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset,
                Edit = (title, value, changed) =>
                    _actions.EditColor(
                        context.Target.Data,
                        context.Target.Material,
                        $"Material Editor - {title}",
                        value,
                        changed),
                SetToPalette = (title, value) =>
                    _actions.SetColorToPalette(
                        context.Target.Data,
                        context.Target.Material,
                        $"Material Editor - {title}",
                        value)
            }, context, descriptor);
        }

        private static KeywordPropertyRowModel CreateExtensionBooleanRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorBooleanPropertyEditor editor)
        {
            return PropertyRowBinding.Extension(new KeywordPropertyRowModel(descriptor.DisplayName)
            {
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            }, context, descriptor);
        }

        private static EnumPropertyRowModel CreateExtensionEnumRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorEnumPropertyEditor editor)
        {
            return PropertyRowBinding.Extension(new EnumPropertyRowModel(descriptor.DisplayName)
            {
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                IsMixed = editor.IsMixed,
                Options = editor.Options,
                SelectInterpolable = editor.SelectInterpolable,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            }, context, descriptor);
        }

        private static VectorPropertyRowModel CreateExtensionVectorRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorVectorPropertyEditor editor)
        {
            return PropertyRowBinding.Extension(new VectorPropertyRowModel(descriptor.DisplayName)
            {
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                ComponentCount = editor.ComponentCount,
                Minimum = descriptor.Minimum,
                Maximum = descriptor.Maximum,
                MixedComponents = CopyMixedComponents(editor.MixedComponents),
                SelectInterpolable = editor.SelectInterpolable,
                ComponentOnChange = editor.ComponentChanged,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            }, context, descriptor);
        }

        private static FloatTogglePropertyRowModel CreateExtensionToggleRow(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor,
            MaterialEditorTogglePropertyEditor editor)
        {
            return PropertyRowBinding.Extension(new FloatTogglePropertyRowModel(descriptor.DisplayName)
            {
                Value = editor.Value,
                OriginalValue = editor.OriginalValue,
                OffValue = editor.OffValue,
                OnValue = editor.OnValue,
                IsMixed = editor.IsMixed,
                SelectInterpolable = editor.SelectInterpolable,
                ValueOnChange = editor.ValueChanged,
                ValueOnReset = editor.Reset
            }, context, descriptor);
        }

        private static bool[] CopyMixedComponents(IList<bool> source)
        {
            var result = new bool[4];
            var copyCount = MaterialEditorMixedStatePerformance
                .GetComponentCopyCount(source, result.Length);
            for (var index = 0; index < copyCount; index++)
                result[index] = source[index];
            return result;
        }
    }
}
