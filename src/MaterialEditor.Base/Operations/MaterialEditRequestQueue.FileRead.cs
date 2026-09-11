using System;

namespace MaterialEditorAPI
{
    internal sealed partial class MaterialEditRequestQueue
    {
        internal Action EnqueueFile(MaterialEditTarget target, Func<bool> valid,
            string path, Func<byte[], bool> apply, Action<MaterialEditResult> completed)
        {
            MaterialAssetFileRead reader = null;
            Action<MaterialEditResult> finish = null;
            return Enqueue(target, valid, done =>
            {
                finish = done;
                reader = MaterialAssetFileRead.Begin(path);
                return () => reader.Dispose();
            }, result =>
            {
                reader?.Dispose();
                completed?.Invoke(result);
            }, advance: () =>
            {
                if (reader == null || !reader.IsComplete) return;
                MaterialEditResult result;
                try
                {
                    result = reader.Data == null
                        ? new MaterialEditResult(MaterialEditStatus.Failed, "Read", reader.Error)
                        : MaterialEditResult.FromApplied(apply(reader.Data));
                }
                catch (Exception ex)
                {
                    result = new MaterialEditResult(MaterialEditStatus.Failed, "Apply", ex.Message);
                }
                finish(result);
            });
        }
    }
}
