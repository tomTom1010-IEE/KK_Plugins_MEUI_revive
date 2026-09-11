using System;
using System.Collections;
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

    /// <summary>
    /// Unity lifecycle owner for one import. Attaching this component to the UI
    /// object guarantees OnDestroy cleanup even if Unity stops the coroutine.
    /// </summary>
    internal sealed class MaterialEditorCubemapImportRunner : MonoBehaviour
    {
        private MaterialEditorCubemapImportCoordinator _coordinator;
        private Coroutine _coroutine;
        private Action<MaterialEditResult> _completed;

        internal void Begin(
            string filePath,
            Func<bool> isTargetAlive,
            Func<
                byte[],
                MaterialEditorCubemapContentKey,
                MaterialEditorCubemapLease,
                MaterialEditResult> apply,
            Action<string> logInfo,
            Action<string> logWarning,
            Action<string> logError,
            Action<MaterialEditResult> completed)
        {
            if (_coordinator != null)
                throw new InvalidOperationException(
                    "This Cubemap import runner is already in use.");

            _completed = completed;
            _coordinator = new MaterialEditorCubemapImportCoordinator(filePath);
            _coroutine = StartCoroutine(
                Run(
                    isTargetAlive,
                    apply,
                    logInfo,
                    logWarning,
                    logError,
                    completed));
        }

        private IEnumerator Run(
            Func<bool> isTargetAlive,
            Func<
                byte[],
                MaterialEditorCubemapContentKey,
                MaterialEditorCubemapLease,
                MaterialEditResult> apply,
            Action<string> logInfo,
            Action<string> logWarning,
            Action<string> logError,
            Action<MaterialEditResult> completed)
        {
            var success = false;
            var result = new MaterialEditResult(MaterialEditStatus.Cancelled, "Target lifecycle");
            var previousState = _coordinator.State;
            SafeInvoke(
                logInfo,
                "Cubemap import started; reading and hashing the source in the background.");
            while (!_coordinator.IsTerminal)
            {
                if (!IsAlive(isTargetAlive))
                {
                    _coordinator.Cancel();
                    break;
                }

                _coordinator.AdvanceOnMainThread();
                if (previousState != _coordinator.State)
                {
                    if (_coordinator.State
                        == MaterialEditorCubemapImportState.Converting)
                    {
                        SafeInvoke(
                            logInfo,
                            "Cubemap decode/conversion started ("
                            + _coordinator.TotalRows
                            + " rows, "
                            + MaterialWorkBudget.DefaultMilliseconds
                            + " ms cooperative budget, at most "
                            + MaterialWorkBudget.DefaultRowLimit
                            + " rows per frame). Individual Unity calls can exceed the budget. PNG decode remains "
                            + "on the main thread; HDR decode and projection work are incremental.");
                    }
                    else if (previousState
                             == MaterialEditorCubemapImportState.Reading
                             && _coordinator.State
                             == MaterialEditorCubemapImportState.Ready)
                    {
                        SafeInvoke(
                            logInfo,
                            "Cubemap source found in the conversion cache; applying it without reprojection.");
                    }
                    previousState = _coordinator.State;
                }
                if (!_coordinator.IsTerminal)
                    yield return null;
            }

            if (_coordinator.State == MaterialEditorCubemapImportState.Ready
                && IsAlive(isTargetAlive))
            {
                if (!string.IsNullOrEmpty(_coordinator.Warning))
                    SafeInvoke(logWarning, _coordinator.Warning);
                success = _coordinator.TryApply((bytes, key, lease) =>
                {
                    result = apply(bytes, key, lease);
                    return result.Succeeded || result.Status == MaterialEditStatus.Unverified;
                });
            }

            if (_coordinator.State == MaterialEditorCubemapImportState.Failed)
                SafeInvoke(logError, _coordinator.Error);
            else if (result.Succeeded)
                SafeInvoke(logInfo, "Cubemap import completed.");
            if (_coordinator.State == MaterialEditorCubemapImportState.Failed)
                result = new MaterialEditResult(MaterialEditStatus.Failed, "Cubemap import", _coordinator.Error);
            _coordinator.Dispose();
            _coordinator = null;
            _coroutine = null;
            Complete(result);
            Destroy(this);
        }

        private static bool IsAlive(Func<bool> predicate)
        {
            if (predicate == null)
                return false;
            try
            {
                return predicate();
            }
            catch
            {
                return false;
            }
        }

        private static void SafeInvoke(Action<string> callback, string value)
        {
            if (callback == null)
                return;
            try
            {
                callback(value);
            }
            catch
            {
            }
        }

        private void Complete(MaterialEditResult result)
        {
            var callback = _completed;
            _completed = null;
            try { callback?.Invoke(result); }
            catch (Exception ex) { MaterialEditorPluginBase.Logger?.LogWarning(ex); }
        }

        internal void Cancel()
        {
            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = null;
            _coordinator?.Dispose();
            _coordinator = null;
            Complete(new MaterialEditResult(MaterialEditStatus.Cancelled, "Runner lifecycle"));
            Destroy(this);
        }

        private void OnDisable() => Cancel();
        private void OnDestroy()
        {
            _coordinator?.Dispose();
            _coordinator = null;
            Complete(new MaterialEditResult(MaterialEditStatus.Cancelled, "Runner destroyed"));
        }
    }
}
