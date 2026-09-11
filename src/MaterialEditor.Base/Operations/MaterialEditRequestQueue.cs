using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>Runtime-only target identity; never serialized into cards or scenes.</summary>
    internal sealed class MaterialEditTarget
    {
        internal readonly GameObject Root;
        internal readonly Material Material;
        internal readonly string MaterialName;
        internal readonly string Property;
        private readonly Shader _shader;

        internal MaterialEditTarget(GameObject root, Material material, string property)
        {
            Root = root;
            Material = material;
            MaterialName = material == null ? null : material.NameFormatted();
            Property = property;
            _shader = material == null ? null : material.shader;
        }

        internal bool IsAlive => Root != null && Material != null
            && Material.shader == _shader && Material.NameFormatted() == MaterialName;

        internal bool Matches(GameObject root, string materialName, string property) =>
            ReferenceEquals(Root, root) && (materialName == null || MaterialName == materialName)
            && (property == null || Property == property);

        internal bool SameProperty(MaterialEditTarget other) =>
            other != null && Matches(other.Root, other.MaterialName, other.Property);
    }

    /// <summary>
    /// Main-thread FIFO. Acceptance precedes async reads. Owners pump at most one
    /// start per Update; completions never recursively start the next request.
    /// </summary>
    internal sealed class MaterialEditRequestQueue : IDisposable
    {
        private sealed class Request
        {
            internal MaterialEditTarget Target;
            internal string WatchPath;
            internal Func<bool> IsValid;
            internal Func<Action<MaterialEditResult>, Action> Start;
            internal Action<MaterialEditResult> Completed;
            internal Action Cancel;
            internal bool Finished;
            internal MaterialEditStatus Status;

            internal void Finish(MaterialEditResult result)
            {
                if (Finished) return;
                Finished = true;
                Status = result.Status;
                var callback = Completed;
                Completed = null;
                Start = null;
                Cancel = null;
                IsValid = null;
                try { callback?.Invoke(result); }
                catch (Exception ex) { MaterialEditorPluginBase.Logger?.LogWarning(ex); }
            }
        }

        private const int Capacity = 32;
        private static readonly List<WeakReference> Queues = new List<WeakReference>();
        private readonly List<Request> _pending = new List<Request>();
        private Request _active;
        private bool _disposed;

        internal MaterialEditRequestQueue()
        {
            Queues.RemoveAll(x => !x.IsAlive);
            Queues.Add(new WeakReference(this));
        }

        internal Action Enqueue(MaterialEditTarget target, Func<bool> valid,
            Func<Action<MaterialEditResult>, Action> start,
            Action<MaterialEditResult> completed, string watchPath = null)
        {
            var request = new Request { Target = target, IsValid = valid,
                Start = start, Completed = completed, WatchPath = watchPath };
            // Only watcher refreshes coalesce. Explicit user imports are never displaced.
            if (watchPath != null)
                CancelWhere(x => x.WatchPath == watchPath && target.SameProperty(x.Target),
                    MaterialEditStatus.Superseded, false);
            // A superseded completion may dispose this owner reentrantly.
            if (_disposed)
            {
                request.Finish(new MaterialEditResult(MaterialEditStatus.Cancelled, "Owner disposed"));
                return null;
            }
            if (_pending.Count + (_active == null || _active.Finished ? 0 : 1) >= Capacity)
            {
                request.Finish(new MaterialEditResult(MaterialEditStatus.Failed, "Queue", "Import queue is full."));
                return null;
            }
            _pending.Add(request);
            return () =>
            {
                _pending.Remove(request);
                CancelRequest(request, MaterialEditStatus.Cancelled);
            };
        }

        internal void Pump()
        {
            if (_disposed) return;
            if (_active != null && !_active.Finished)
            {
                if (!Valid(_active)) CancelRequest(_active, MaterialEditStatus.Cancelled);
                return;
            }
            _active = null;
            if (_pending.Count == 0) return;
            var request = _pending[0];
            _pending.RemoveAt(0);
            _active = request;
            if (!Valid(request))
            {
                request.Finish(new MaterialEditResult(MaterialEditStatus.Cancelled, "Stale target"));
                return;
            }
            try
            {
                var cancel = request.Start(request.Finish);
                if (!request.Finished) request.Cancel = cancel;
                else if (request.Status == MaterialEditStatus.Cancelled || request.Status == MaterialEditStatus.Superseded)
                    cancel?.Invoke();
            }
            catch (Exception ex)
            {
                request.Finish(new MaterialEditResult(MaterialEditStatus.Failed, "Import", ex.Message));
            }
        }

        private static bool Valid(Request request)
        {
            try { return request.Target.IsAlive && (request.IsValid == null || request.IsValid()); }
            catch { return false; }
        }

        private static void CancelRequest(Request request, MaterialEditStatus status)
        {
            if (request.Finished) return;
            var cancel = request.Cancel;
            request.Cancel = null;
            // Mark terminal before cancelling a runner, which can call back synchronously.
            request.Finish(new MaterialEditResult(status, "Lifecycle"));
            try { cancel?.Invoke(); }
            catch (Exception ex) { MaterialEditorPluginBase.Logger?.LogWarning(ex); }
        }

        private void CancelWhere(Predicate<Request> match, MaterialEditStatus status, bool includeActive)
        {
            var removed = _pending.FindAll(match);
            _pending.RemoveAll(match);
            if (includeActive && _active != null && match(_active))
                CancelRequest(_active, status);
            foreach (var request in removed) CancelRequest(request, status);
        }

        internal void CancelAll() => CancelWhere(x => true, MaterialEditStatus.Cancelled, true);

        internal static void CancelTarget(GameObject root, string materialName = null, string property = null)
        {
            foreach (var weak in Queues.ToArray())
            {
                var queue = weak.Target as MaterialEditRequestQueue;
                queue?.CancelWhere(x => x.Target.Matches(root, materialName, property),
                    MaterialEditStatus.Cancelled, true);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            CancelAll();
            _active = null;
            Queues.RemoveAll(x => !x.IsAlive || ReferenceEquals(x.Target, this));
        }
    }
}
