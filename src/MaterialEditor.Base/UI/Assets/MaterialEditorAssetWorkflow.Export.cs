using System;
using System.IO;
using UnityEngine;
using static MaterialEditorAPI.MaterialEditorPluginBase;

namespace MaterialEditorAPI
{
    internal sealed partial class MaterialEditorAssetWorkflow
    {
        internal void ExportCubemap(Material material, string propertyName)
        {
            var cubemap = MaterialPropertyAccess.GetTexture(material, MaterialPropertyIdCache.Get(propertyName)) as Cubemap;
            if (!IsHostAlive() || cubemap == null) return;
            var filename = Path.Combine(ExportPath,
                $"_Export_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}_{SanitizeMaterialName(material)}_{propertyName}.png");
            var target = new MaterialEditTarget(_host.gameObject, material, propertyName);
            MaterialEditorCubemapConversion.ExportOperation operation = null;
            MaterialAssetFileWrite writer = null;
            Action<MaterialEditResult> finish = null;
            Action cleanup = () => { operation?.Dispose(); writer?.Dispose(); };
            _exports.Enqueue(target,
                () => IsHostAlive() && cubemap != null
                    && MaterialPropertyAccess.GetTexture(material, MaterialPropertyIdCache.Get(propertyName)) == cubemap,
                done =>
                {
                    finish = done;
                    string error;
                    if (!MaterialEditorCubemapConversion.TryBeginExport(cubemap, out operation, out error))
                        done(new MaterialEditResult(MaterialEditStatus.Failed, "Export admission", error));
                    return cleanup;
                }, result =>
                {
                    cleanup();
                    if (!IsHostAlive()) return;
                    if (result.Succeeded)
                    {
                        MaterialEditorPluginBase.Logger.LogInfo($"Exported {filename}");
                        Utilities.OpenFileInExplorer(filename);
                    }
                    else if (result.Status == MaterialEditStatus.Failed)
                    {
                        MaterialEditorPluginBase.Logger.LogError(result.Stage + ": " + result.Diagnostic);
                        MaterialEditorPluginBase.Logger.LogMessage(result.Diagnostic);
                    }
                }, advance: () =>
                {
                    string error;
                    if (writer != null)
                    {
                        if (!writer.IsComplete) return;
                        var saved = writer.TryCommit(out error);
                        finish(new MaterialEditResult(saved ? MaterialEditStatus.Succeeded : MaterialEditStatus.Failed,
                            "Export write", error));
                        return;
                    }
                    if (!operation.AdvanceScheduled(out error))
                    {
                        finish(new MaterialEditResult(MaterialEditStatus.Failed, operation.Stage, error));
                        return;
                    }
                    if (!operation.IsComplete) return;
                    writer = MaterialAssetFileWrite.Begin(filename, operation.TakeData(), operation.TakeReservation());
                    operation.Dispose();
                    operation = null;
                });
        }
    }
}
