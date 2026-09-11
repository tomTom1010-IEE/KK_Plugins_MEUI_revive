using System;
using UnityEngine;

namespace MaterialEditorAPI
{
    internal enum MaterialEditorCubemapImportState
    {
        Reading,
        WaitingForAdmission,
        Converting,
        Ready,
        Applied,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Owns the resources used by one incremental Cubemap import. All methods
    /// except the background reader itself must be called on Unity's main
    /// thread. Unity's LoadImage/GetPixels32 path for PNG and all Cubemap APIs
    /// intentionally stay there; Radiance RGBE decode and panorama projection are advanced in
    /// bounded scanline batches. Only disk IO and hashing run on the worker.
    /// </summary>
    internal sealed class MaterialEditorCubemapImportCoordinator : IDisposable
    {
        private MaterialEditorCubemapBackgroundRead _backgroundRead;
        private MaterialEditorCubemapAcquireOperation _acquire;
        private MaterialEditorCubemapLease _warmLease;
        private byte[] _encodedData;
        private MaterialEditorCubemapContentKey _contentKey;
        private bool _disposed;
        private readonly bool _waitForAdmission;

        internal MaterialEditorCubemapImportCoordinator(string filePath)
        {
            State = MaterialEditorCubemapImportState.Reading;
            _backgroundRead = MaterialEditorCubemapBackgroundRead.Begin(filePath);
        }

        internal MaterialEditorCubemapImportState State { get; private set; }

        internal MaterialEditorCubemapImportCoordinator(byte[] encodedData)
        {
            _waitForAdmission = true;
            State = MaterialEditorCubemapImportState.Reading;
            _backgroundRead = MaterialEditorCubemapBackgroundRead.BeginData(encodedData);
        }

        internal MaterialEditorCubemapLease TakePreparedLease()
        {
            if (_disposed || State != MaterialEditorCubemapImportState.Ready) return null;
            var lease = _warmLease;
            _warmLease = null;
            _encodedData = null;
            _contentKey = null;
            return lease;
        }

        internal string Warning { get; private set; }

        internal string Error { get; private set; }

        internal int CompletedRows
        {
            get { return _acquire == null ? 0 : _acquire.CompletedRows; }
        }

        internal int TotalRows
        {
            get { return _acquire == null ? 0 : _acquire.TotalRows; }
        }

        internal bool IsTerminal
        {
            get
            {
                return State == MaterialEditorCubemapImportState.Ready
                       || State == MaterialEditorCubemapImportState.Applied
                       || State == MaterialEditorCubemapImportState.Failed
                       || State == MaterialEditorCubemapImportState.Cancelled;
            }
        }

        /// <summary>
        /// Advances at most one bounded batch of conversion work.
        /// </summary>
        internal void AdvanceOnMainThread()
        {
            if (_disposed || IsTerminal) return;
            using (var slice = MaterialFrameWorkScheduler.TryEnter(this))
            {
                if (slice != null) AdvanceAdmitted();
            }
        }

        private void AdvanceAdmitted()
        {
            if (_disposed || IsTerminal)
                return;

            try
            {
                if (State == MaterialEditorCubemapImportState.Reading
                    || State == MaterialEditorCubemapImportState.WaitingForAdmission)
                {
                    if (_backgroundRead != null)
                    {
                        if (!_backgroundRead.IsComplete)
                            return;

                        string readError;
                        if (!_backgroundRead.TryTakeResult(
                                out _encodedData,
                                out _contentKey,
                                out readError))
                            return;

                        _backgroundRead.Dispose();
                        _backgroundRead = null;
                        if (!string.IsNullOrEmpty(readError)
                            || _encodedData == null
                            || _contentKey == null)
                        {
                            Fail(
                                string.IsNullOrEmpty(readError)
                                    ? "The Cubemap source read completed without data."
                                    : readError);
                            return;
                        }
                    }

                    MaterialEditorCubemapLease cacheHit;
                    MaterialEditorCubemapAcquireOperation acquire;
                    string warning;
                    string error;
                    bool waiting;
                    if (!MaterialEditorCubemapCache.TryBeginAcquire(
                            _encodedData,
                            _contentKey,
                            out cacheHit,
                            out acquire,
                            out warning,
                            out error,
                            out waiting))
                    {
                        if (waiting && _waitForAdmission)
                        {
                            State = MaterialEditorCubemapImportState.WaitingForAdmission;
                            return;
                        }
                        Fail(error);
                        return;
                    }

                    Warning = warning;
                    if (cacheHit != null)
                    {
                        _warmLease = cacheHit;
                        State = MaterialEditorCubemapImportState.Ready;
                        return;
                    }

                    if (acquire == null)
                    {
                        Fail("Cubemap cache acquisition did not start.");
                        return;
                    }

                    _acquire = acquire;
                    State = MaterialEditorCubemapImportState.Converting;
                    // Do not also process rows in the setup frame. PNG decode
                    // and GetPixels32 are already unavoidable main-thread work;
                    // HDR decode begins incrementally on the following frame.
                    return;
                }

                string processError;
                if (!_acquire.ProcessFrame(out processError))
                {
                    Fail(processError);
                    return;
                }
                if (!_acquire.IsComplete)
                    return;

                _warmLease = _acquire.TakeLease();
                _acquire.Dispose();
                _acquire = null;
                if (_warmLease == null)
                {
                    Fail("Cubemap conversion completed without a cache lease.");
                    return;
                }

                State = MaterialEditorCubemapImportState.Ready;
            }
            catch (Exception exception)
            {
                Fail("Cubemap import failed: " + exception.Message);
            }
        }

        /// <summary>
        /// Calls the persistence path while the warm cache lease is still held.
        /// The source-bound key lets the controller acquire its own cache lease
        /// without repeating SHA-256 or conversion. The callback must return
        /// false only when no persistence path accepted the import.
        /// </summary>
        internal bool TryApply(
            Func<
                byte[],
                MaterialEditorCubemapContentKey,
                MaterialEditorCubemapLease,
                bool> apply)
        {
            if (_disposed || State != MaterialEditorCubemapImportState.Ready)
                return false;
            if (apply == null)
            {
                Fail("No Cubemap persistence callback was provided.");
                return false;
            }

            try
            {
                if (!apply(_encodedData, _contentKey, _warmLease))
                {
                    Fail("The selected Cubemap could not be persisted.");
                    return false;
                }
                State = MaterialEditorCubemapImportState.Applied;
                return true;
            }
            catch (Exception exception)
            {
                Fail("Could not persist the selected Cubemap: "
                     + exception.Message);
                return false;
            }
            finally
            {
                if (_warmLease != null)
                    _warmLease.Dispose();
                _warmLease = null;
                _encodedData = null;
                _contentKey = null;
            }
        }

        internal void Cancel()
        {
            if (_disposed || State == MaterialEditorCubemapImportState.Applied)
                return;
            State = MaterialEditorCubemapImportState.Cancelled;
            ReleaseOwnedResources();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            ReleaseOwnedResources();
        }

        private void Fail(string error)
        {
            Error = string.IsNullOrEmpty(error)
                ? "Cubemap import failed for an unknown reason."
                : error;
            State = MaterialEditorCubemapImportState.Failed;
            ReleaseOwnedResources();
        }

        private void ReleaseOwnedResources()
        {
            MaterialFrameWorkScheduler.Release(this);
            if (_backgroundRead != null)
                _backgroundRead.Dispose();
            _backgroundRead = null;
            if (_acquire != null)
                _acquire.Dispose();
            _acquire = null;
            if (_warmLease != null)
                _warmLease.Dispose();
            _warmLease = null;
            _encodedData = null;
            _contentKey = null;
        }
    }

}
