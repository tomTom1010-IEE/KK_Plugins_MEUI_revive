using MaterialEditorAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class SceneController
    {
        private static readonly MaterialPropertyRecordQuery<MaterialFloatProperty> FloatPropertyQuery =
            new MaterialPropertyRecordQuery<MaterialFloatProperty>(x => new MaterialPropertyRecordKey(
                -1, 0, x.ID, x.MaterialName, x.Property));

        private static readonly MaterialPropertyRecordQuery<MaterialKeywordProperty> KeywordPropertyQuery =
            new MaterialPropertyRecordQuery<MaterialKeywordProperty>(x => new MaterialPropertyRecordKey(
                -1, 0, x.ID, x.MaterialName, x.Property));

        private static readonly MaterialPropertyRecordQuery<MaterialColorProperty> ColorPropertyQuery =
            new MaterialPropertyRecordQuery<MaterialColorProperty>(x => new MaterialPropertyRecordKey(
                -1, 0, x.ID, x.MaterialName, x.Property));

        private static readonly MaterialPropertyRecordQuery<MaterialVectorProperty> VectorPropertyQuery =
            new MaterialPropertyRecordQuery<MaterialVectorProperty>(x => new MaterialPropertyRecordKey(
                -1, 0, x.ID, x.MaterialName, x.Property));

        private static readonly MaterialPropertyRecordQuery<MaterialTextureProperty> TexturePropertyQuery =
            new MaterialPropertyRecordQuery<MaterialTextureProperty>(x => new MaterialPropertyRecordKey(
                -1, 0, x.ID, x.MaterialName, x.Property));

        private static readonly MaterialPropertyRecordQuery<MaterialCubemapProperty> CubemapPropertyQuery =
            new MaterialPropertyRecordQuery<MaterialCubemapProperty>(x => new MaterialPropertyRecordKey(
                -1, 0, x.ID, x.MaterialName, x.Property));

        private static readonly MaterialPropertyRecordQuery<MaterialShader> ShaderPropertyQuery =
            new MaterialPropertyRecordQuery<MaterialShader>(x => new MaterialPropertyRecordKey(
                -1, 0, x.ID, x.MaterialName, null));
    }
}
