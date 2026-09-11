namespace MaterialEditorAPI
{
    /// <summary>Common row identity only. Value/reset/extension callbacks remain type-specific.</summary>
    internal static class PropertyRowBinding
    {
        internal static T BuiltIn<T>(T row, PropertyDescriptor descriptor) where T : RowModel
        {
            row.GameObject = descriptor.GameObject;
            row.Data = descriptor.Data;
            row.Material = descriptor.Material;
            row.Projector = descriptor.Projector;
            row.PropertyName = descriptor.Name;
            row.PublicDescriptor = descriptor.PublicDescriptor;
            return row;
        }

        internal static T Extension<T>(T row, MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor) where T : RowModel
        {
            row.GameObject = context.Target.GameObject;
            row.Data = context.Target.Data;
            row.Material = context.Target.Material;
            row.Projector = context.Target.Projector;
            row.PropertyName = descriptor.PropertyName;
            row.PublicDescriptor = descriptor;
            return row;
        }
    }
}
