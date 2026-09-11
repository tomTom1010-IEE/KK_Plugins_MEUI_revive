using System;
using System.Collections;
using UnityEngine;

namespace MaterialEditorAPI
{
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
