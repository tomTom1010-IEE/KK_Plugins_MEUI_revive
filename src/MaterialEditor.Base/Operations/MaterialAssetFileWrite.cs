using System;
using System.IO;
using System.Threading;

namespace MaterialEditorAPI
{
    /// <summary>Writes a sibling temporary file on a worker; the owner commits it on its thread.</summary>
    internal sealed class MaterialAssetFileWrite : IDisposable
    {
        private readonly object _sync = new object();
        private readonly string _destination;
        private readonly string _temporary;
        private byte[] _data;
        private IDisposable _ownership;
        private volatile bool _cancelled;
        private volatile bool _complete;
        private string _error;

        private MaterialAssetFileWrite(string destination, byte[] data, IDisposable ownership)
        {
            _destination = destination;
            _temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            _data = data;
            _ownership = ownership;
        }

        internal bool IsComplete => _complete;
        internal string Error => _error;

        // Takes ownership even if the worker cannot start.
        internal static MaterialAssetFileWrite Begin(string destination, byte[] data, IDisposable ownership)
        {
            var job = new MaterialAssetFileWrite(destination, data, ownership);
            try
            {
                if (!ThreadPool.QueueUserWorkItem(job.Write))
                    throw new InvalidOperationException("Could not queue the export file write.");
            }
            catch (Exception ex) { job._error = ex.Message; job._data = null; job._complete = true; }
            return job;
        }

        private void Write(object state)
        {
            try
            {
                using (var stream = new FileStream(_temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    for (var offset = 0; offset < _data.Length && !_cancelled;)
                    {
                        var count = Math.Min(65536, _data.Length - offset);
                        stream.Write(_data, offset, count);
                        offset += count;
                    }
            }
            catch (Exception ex) { _error = ex.Message; }
            finally
            {
                lock (_sync)
                {
                    _data = null;
                    _complete = true;
                    if (_cancelled) ReleaseOwnership();
                }
                if (_cancelled || _error != null) Cleanup();
            }
        }

        // Main-thread publication point: cancellation/target checks precede this call.
        // Same-directory rename/replace avoids a partially written final file.
        internal bool TryCommit(out string error)
        {
            error = _error;
            if (_cancelled || !_complete)
            {
                error = "The export write is not ready or was cancelled.";
                return false;
            }
            if (error != null) return false;
            try
            {
                if (File.Exists(_destination)) File.Replace(_temporary, _destination, null);
                else File.Move(_temporary, _destination);
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        public void Dispose()
        {
            bool cleanup;
            lock (_sync)
            {
                _cancelled = true;
                cleanup = _complete;
                if (cleanup) ReleaseOwnership();
            }
            if (cleanup)
            {
                try { if (!ThreadPool.QueueUserWorkItem(_ => Cleanup())) Cleanup(); }
                catch { Cleanup(); }
            }
        }

        private void ReleaseOwnership()
        {
            var ownership = _ownership;
            _ownership = null;
            ownership?.Dispose();
        }

        private void Cleanup()
        {
            try { File.Delete(_temporary); }
            catch { /* Never delete or truncate the destination on cleanup failure. */ }
        }
    }
}
