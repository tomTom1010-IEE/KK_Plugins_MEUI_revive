using System.Collections.Generic;
using MaterialEditorAPI;
using UnityEngine;
using static MaterialEditorAPI.MaterialAPI;

namespace KK_Plugins.MaterialEditor
{
    public partial class SceneController
    {
        // Accumulate for the entire copy event so new records cannot become sources
        // for a later destination in the same event.
        private sealed class SceneCopyBatch
        {
            internal readonly List<RendererProperty> rendererPropertyListNew = new List<RendererProperty>();
            internal readonly List<ProjectorProperty> projectorPropertyListNew = new List<ProjectorProperty>();
            internal readonly List<MaterialNameProperty> materialNamePropertyListNew = new List<MaterialNameProperty>();
            internal readonly List<MaterialFloatProperty> materialFloatPropertyListNew = new List<MaterialFloatProperty>();
            internal readonly List<MaterialKeywordProperty> materialKeywordPropertyListNew = new List<MaterialKeywordProperty>();
            internal readonly List<MaterialColorProperty> materialColorPropertyListNew = new List<MaterialColorProperty>();
            internal readonly List<MaterialVectorProperty> materialVectorPropertyListNew = new List<MaterialVectorProperty>();
            internal readonly List<MaterialTextureProperty> materialTexturePropertyListNew = new List<MaterialTextureProperty>();
            internal readonly List<MaterialCubemapProperty> materialCubemapPropertyListNew = new List<MaterialCubemapProperty>();
            internal readonly List<MaterialShader> materialShaderListNew = new List<MaterialShader>();
            internal readonly List<MaterialCopy> materialCopyListNew = new List<MaterialCopy>();
        }

        private sealed class SceneCopyContext
        {
            internal int SourceId;
            internal int DestinationId;
            internal GameObject Root;
            internal GameObject SourceRoot;
            internal SceneCopyBatch Output;
        }

        private void CopyMaterialCopyList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(MaterialCopyList), MaterialCopyList, loadedProperty =>
            {
                if (loadedProperty.ID == context.SourceId)
                {
                    CopyMaterial(context.Root, loadedProperty.MaterialName, loadedProperty.MaterialCopyName);
                    context.Output.materialCopyListNew.Add(new MaterialCopy(context.DestinationId, loadedProperty.MaterialName, loadedProperty.MaterialCopyName));
                }
            });
        }

        private void CopyMaterialNamePropertyList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(MaterialNamePropertyList), MaterialNamePropertyList, loadedProperty =>
            {
                if (loadedProperty.ID == context.SourceId)
                {
                    MaterialAPI.SetName(context.Root, loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value);
                    context.Output.materialNamePropertyListNew.Add(new MaterialNameProperty(context.DestinationId, loadedProperty.Renderer, loadedProperty.MaterialName, loadedProperty.Value));
                }
            });
        }

        private void CopyMaterialShaderList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(MaterialShaderList), MaterialShaderList, loadedProperty =>
            {
                if (loadedProperty.ID == context.SourceId)
                {
                    bool setShader = SetShader(context.Root, loadedProperty.MaterialName, loadedProperty.ShaderName);
                    bool setRenderQueue = SetRenderQueue(context.Root, loadedProperty.MaterialName, loadedProperty.RenderQueue);
                    if (setShader || setRenderQueue)
                        context.Output.materialShaderListNew.Add(new MaterialShader(context.DestinationId, loadedProperty.MaterialName, loadedProperty.ShaderName, loadedProperty.ShaderNameOriginal, loadedProperty.RenderQueue, loadedProperty.RenderQueueOriginal));
                }
            });
        }

        private void CopyRendererPropertyList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(RendererPropertyList), RendererPropertyList, loadedProperty =>
            {
                if (loadedProperty.ID == context.SourceId)
                    if (MaterialAPI.SetRendererProperty(context.Root, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value))
                        context.Output.rendererPropertyListNew.Add(new RendererProperty(context.DestinationId, loadedProperty.RendererName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void CopyProjectorPropertyList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(ProjectorPropertyList), ProjectorPropertyList, loadedProperty =>
            {
                if (loadedProperty.ID == context.SourceId)
                    if (MaterialAPI.SetProjectorProperty(context.Root, loadedProperty.ProjectorName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                        context.Output.projectorPropertyListNew.Add(new ProjectorProperty(context.DestinationId, loadedProperty.ProjectorName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void CopyMaterialFloatPropertyList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(MaterialFloatPropertyList), MaterialFloatPropertyList, loadedProperty =>
            {
                if (loadedProperty.ID == context.SourceId)
                    if (SetFloat(context.Root, loadedProperty.MaterialName, loadedProperty.Property, float.Parse(loadedProperty.Value)))
                        context.Output.materialFloatPropertyListNew.Add(new MaterialFloatProperty(context.DestinationId, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void CopyMaterialKeywordPropertyList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(MaterialKeywordPropertyList), MaterialKeywordPropertyList, loadedProperty =>
            {
                if (loadedProperty.ID == context.SourceId)
                    if (SetKeyword(context.Root, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                        context.Output.materialKeywordPropertyListNew.Add(new MaterialKeywordProperty(context.DestinationId, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void CopyMaterialColorPropertyList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(MaterialColorPropertyList), MaterialColorPropertyList, loadedProperty =>
            {
                if (loadedProperty.ID == context.SourceId)
                    if (SetColor(context.Root, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                        context.Output.materialColorPropertyListNew.Add(new MaterialColorProperty(context.DestinationId, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void CopyMaterialVectorPropertyList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(MaterialVectorPropertyList), MaterialVectorPropertyList, loadedProperty =>
            {
                if (loadedProperty.ID == context.SourceId)
                    if (SetVector(context.Root, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value))
                        context.Output.materialVectorPropertyListNew.Add(new MaterialVectorProperty(context.DestinationId, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.Value, loadedProperty.ValueOriginal));
            });
        }

        private void CopyMaterialTexturePropertyList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(MaterialTexturePropertyList), MaterialTexturePropertyList, loadedProperty =>
            {
                if (loadedProperty.ID == context.SourceId)
                {
                    MaterialTextureProperty newTextureProperty = new MaterialTextureProperty(context.DestinationId, loadedProperty.MaterialName, loadedProperty.Property, loadedProperty.TexID, loadedProperty.Offset, loadedProperty.OffsetOriginal, loadedProperty.Scale, loadedProperty.ScaleOriginal, loadedProperty.TexAnimationDef);

                    bool setTex = false;
                    if (loadedProperty.TexID != null)
                        setTex = SetTextureWithProperty(context.Root, newTextureProperty);

                    bool setOffset = SetTextureOffset(context.Root, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Offset);
                    bool setScale = SetTextureScale(context.Root, newTextureProperty.MaterialName, newTextureProperty.Property, newTextureProperty.Scale);

                    if (setTex || setOffset || setScale) context.Output.materialTexturePropertyListNew.Add(newTextureProperty);
                }
            });
        }

        private void CopyMaterialCubemapPropertyList(SceneCopyContext context)
        {
            MaterialEditLoadContext.ApplyRecords(nameof(MaterialCubemapPropertyList), MaterialCubemapPropertyList, loadedProperty =>
            {
                if (loadedProperty.ID != context.SourceId)
                    return;

                var newCubemapProperty = new MaterialCubemapProperty(
                    context.DestinationId,
                    loadedProperty.MaterialName,
                    loadedProperty.Property,
                    loadedProperty.TexID);
                newCubemapProperty.InheritCubemapOriginalSnapshot(
                    loadedProperty,
                    context.SourceRoot);
                if (newCubemapProperty.TexID.HasValue
                    && SetCubemapWithProperty(
                        context.Root,
                        newCubemapProperty))
                    context.Output.materialCubemapPropertyListNew.Add(newCubemapProperty);
            });
        }

        private void CommitCopiedProperties(SceneCopyBatch batch)
        {
            RendererPropertyList.AddRange(batch.rendererPropertyListNew);
            ProjectorPropertyList.AddRange(batch.projectorPropertyListNew);
            MaterialNamePropertyList.AddRange(batch.materialNamePropertyListNew);
            MaterialFloatPropertyList.AddRange(batch.materialFloatPropertyListNew);
            MaterialKeywordPropertyList.AddRange(batch.materialKeywordPropertyListNew);
            MaterialColorPropertyList.AddRange(batch.materialColorPropertyListNew);
            MaterialVectorPropertyList.AddRange(batch.materialVectorPropertyListNew);
            MaterialTexturePropertyList.AddRange(batch.materialTexturePropertyListNew);
            MaterialCubemapPropertyList.AddRange(batch.materialCubemapPropertyListNew);
            MaterialShaderList.AddRange(batch.materialShaderListNew);
            MaterialCopyList.AddRange(batch.materialCopyListNew);
        }
    }
}
