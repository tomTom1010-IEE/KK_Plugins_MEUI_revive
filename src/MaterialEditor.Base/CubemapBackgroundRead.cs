using System;
using System.IO;
using System.Threading;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Reads and hashes one Cubemap source without blocking Unity's main thread.
    /// File IO and SHA-256 are managed-only work; Unity decode remains the
    /// responsibility of the main-thread import coordinator.
    /// </summary>
    internal sealed class MaterialEditorCubemapBackgroundRead : IDisposable
    {
        private readonly object _sync = new object();
        private readonly string _filePath;
        private byte[] _sourceData;
        private volatile bool _cancelled;
        private volatile bool _completed;
        private byte[] _data;
        private MaterialEditorCubemapContentKey _contentKey;
        private string _error;

        private MaterialEditorCubemapBackgroundRead(string filePath)
        {
            _filePath = filePath;
        }

        internal bool IsComplete
        {
            get { return _completed; }
        }

        internal int WorkerThreadId { get; private set; }

        internal static MaterialEditorCubemapBackgroundRead Begin(
            string filePath)
        {
            var operation = new MaterialEditorCubemapBackgroundRead(filePath);
            if (string.IsNullOrEmpty(filePath))
            {
                operation.Complete(
                    null,
                    null,
                    "No Cubemap source file was selected.");
                return operation;
            }

            try
            {
                if (!ThreadPool.QueueUserWorkItem(operation.ReadOnWorker))
                {
                    operation.Complete(
                        null,
                        null,
                        "Could not queue the Cubemap source read.");
                }
            }
            catch (Exception exception)
            {
                operation.Complete(
                    null,
                    null,
                    "Could not queue the Cubemap source read: "
                    + exception.Message);
            }
            return operation;
        }

        internal static MaterialEditorCubemapBackgroundRead BeginData(byte[] data)
        {
            var operation = new MaterialEditorCubemapBackgroundRead(null) { _sourceData = data };
            try
            {
                if (data == null) throw new ArgumentNullException("data");
                if (!ThreadPool.QueueUserWorkItem(operation.ReadOnWorker))
                    throw new InvalidOperationException("Could not queue Cubemap source hashing.");
            }
            catch (Exception ex)
            {
                operation._sourceData = null;
                operation.Complete(null, null, ex.Message);
            }
            return operation;
        }

        internal bool TryTakeResult(
            out byte[] data,
            out MaterialEditorCubemapContentKey contentKey,
            out string error)
        {
            data = null;
            contentKey = null;
            error = null;
            if (!_completed)
                return false;

            lock (_sync)
            {
                if (_cancelled)
                {
                    error = "The Cubemap source read was cancelled.";
                    return true;
                }

                data = _data;
                contentKey = _contentKey;
                error = _error;
                _data = null;
                _contentKey = null;
                return true;
            }
        }

        public void Dispose()
        {
            _cancelled = true;
            lock (_sync)
            {
                _data = null;
                _contentKey = null;
            }
        }

        private void ReadOnWorker(object state)
        {
            WorkerThreadId = Thread.CurrentThread.ManagedThreadId;
            byte[] data = null;
            MaterialEditorCubemapContentKey contentKey = null;
            string error = null;
            try
            {
                data = _sourceData;
                if (data == null)
                {
                    var fileInfo = new FileInfo(_filePath);
                    if (!fileInfo.Exists)
                        error = "The selected Cubemap source file no longer exists.";
                    else if (MaterialEditorCubemapProjection.TryValidateSourceFileLength(fileInfo.Length, out error))
                        data = File.ReadAllBytes(_filePath);
                }
                if (!_cancelled && data != null)
                {
                    if (!MaterialEditorCubemapProjection.TryValidateSourceFileLength(data.LongLength, out error)
                        || !MaterialEditorCubemapContentKey.TryCompute(data, out contentKey, out error))
                        data = null;
                }
            }
            catch (Exception exception)
            {
                error = "Could not read the selected Cubemap source: "
                        + exception.Message;
                data = null;
                contentKey = null;
            }

            _sourceData = null;
            Complete(data, contentKey, error);
        }

        private void Complete(
            byte[] data,
            MaterialEditorCubemapContentKey contentKey,
            string error)
        {
            lock (_sync)
            {
                if (_cancelled)
                    return;

                _data = data;
                _contentKey = contentKey;
                _error = error;
                // Volatile publication happens last so the main thread never
                // observes a partial result.
                _completed = true;
            }
        }
    }
}
