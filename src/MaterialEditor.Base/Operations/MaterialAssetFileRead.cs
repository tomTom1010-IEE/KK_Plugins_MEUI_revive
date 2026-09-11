using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Managed-only file read. One process-wide admission slot includes the ready
    /// buffer until its main-thread consumer disposes it; pending jobs hold paths only.
    /// </summary>
    internal sealed class MaterialAssetFileRead : IDisposable
    {
        private static readonly object Gate = new object();
        private static readonly List<MaterialAssetFileRead> Pending = new List<MaterialAssetFileRead>();
        private static MaterialAssetFileRead _active;
        private readonly string _path;
        private volatile bool _cancelled;
        private volatile bool _complete;
        private byte[] _data;
        private string _error;

        private MaterialAssetFileRead(string path) { _path = path; }
        internal bool IsComplete => _complete;
        internal byte[] Data => _data;
        internal string Error => _error;

        internal static MaterialAssetFileRead Begin(string path)
        {
            var job = new MaterialAssetFileRead(path);
            lock (Gate)
            {
                if (Pending.Count >= 32)
                {
                    job._error = "The background file-read queue is full.";
                    job._complete = true;
                }
                else
                {
                    Pending.Add(job);
                    StartNext();
                }
            }
            return job;
        }

        // Gate is held. Failed worker admission must not strand the next job.
        private static void StartNext()
        {
            while (_active == null && Pending.Count != 0)
            {
                var job = Pending[0];
                Pending.RemoveAt(0);
                _active = job;
                try
                {
                    if (!ThreadPool.QueueUserWorkItem(job.Read))
                        throw new InvalidOperationException("Could not queue a background file read.");
                    return;
                }
                catch (Exception ex)
                {
                    job._error = ex.Message;
                    job._complete = true;
                    _active = null;
                }
            }
        }

        private void Read(object state)
        {
            byte[] data = null;
            string error = null;
            try
            {
                // Retry only transient sharing/read failures, not decode failures.
                for (var attempt = 0; attempt < 3 && !_cancelled; attempt++)
                {
                    try
                    {
                        using (var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            var length = checked((int)stream.Length);
                            data = new byte[length];
                            var offset = 0;
                            while (offset < length && !_cancelled)
                            {
                                var read = stream.Read(data, offset, Math.Min(65536, length - offset));
                                if (read == 0) throw new EndOfStreamException("The image file changed while reading.");
                                offset += read;
                            }
                        }
                        break;
                    }
                    catch (IOException)
                    {
                        data = null;
                        if (attempt == 2) throw;
                        Thread.Sleep(50);
                    }
                }
            }
            catch (Exception ex) { error = ex.Message; data = null; }
            lock (Gate)
            {
                _data = _cancelled ? null : data;
                _error = error;
                _complete = true;
                if (_cancelled) ReleaseSlot();
            }
        }

        public void Dispose()
        {
            lock (Gate)
            {
                _cancelled = true;
                _data = null;
                Pending.Remove(this);
                // An active worker keeps admission until its final buffer is released.
                if (_complete) ReleaseSlot();
            }
        }

        private void ReleaseSlot()
        {
            if (!ReferenceEquals(_active, this)) return;
            _active = null;
            StartNext();
        }
    }
}
