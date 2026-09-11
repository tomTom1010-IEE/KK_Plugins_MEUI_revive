using MaterialEditorAPI;
using System.Diagnostics;

static class Program
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Until(Func<bool> ready, Action tick = null)
    {
        var timer = Stopwatch.StartNew();
        while (!ready())
        {
            if (timer.ElapsedMilliseconds > 10000) throw new Exception("Timed out");
            tick?.Invoke();
            Thread.Sleep(1);
        }
    }

    static void Main()
    {
        var ownerA = new object();
        var ownerB = new object();
        using (var slice = MaterialFrameWorkScheduler.TryEnter(ownerA))
        {
            Check(slice != null, "First owner admitted");
            Thread.Sleep(5);
            Check(MaterialFrameWorkScheduler.BudgetExhausted, "Shared elapsed budget");
        }
        Check(MaterialFrameWorkScheduler.TryEnter(ownerB) == null, "Second owner waits for frame budget");
        UnityEngine.Time.frameCount++;
        Check(MaterialFrameWorkScheduler.TryEnter(ownerA) == null, "Waiting owner has FIFO priority");
        using (var slice = MaterialFrameWorkScheduler.TryEnter(ownerB)) Check(slice != null, "Waiting owner progresses next frame");
        MaterialFrameWorkScheduler.Release(ownerA);
        var budget = new MaterialWorkBudget(10000, 2);
        Check(budget.TryStartUnit() && budget.TryStartUnit() && !budget.TryStartUnit(), "Work-unit cap");
        budget = new MaterialWorkBudget(0.000001, 10);
        Thread.Sleep(1);
        Check(budget.TryStartUnit() && !budget.TryStartUnit(), "Expired budget still makes one unit of progress");
        var path = Path.GetTempFileName();
        try
        {
            var bytes = new byte[] { 3, 1, 4, 1, 5 };
            File.WriteAllBytes(path, bytes);
            using (var hash = MaterialEditorCubemapBackgroundRead.BeginData(bytes))
            {
                Until(() => hash.IsComplete);
                Check(hash.TryTakeResult(out var source, out var key, out var error)
                    && error == null && ReferenceEquals(source, bytes) && key.Matches(bytes), "Restore hash identity");
                Check(hash.WorkerThreadId != Environment.CurrentManagedThreadId, "Hash must run on a worker");
            }
            using (var write = MaterialAssetFileWrite.Begin(path, new byte[] { 9, 8 }, null))
            {
                Until(() => write.IsComplete);
                Check(File.ReadAllBytes(path).SequenceEqual(bytes), "Write must not publish before commit");
                write.Dispose();
                Check(!write.TryCommit(out _), "Cancelled write must not commit");
                Check(File.ReadAllBytes(path).SequenceEqual(bytes), "Cancellation must preserve the old file");
            }
            using (var write = MaterialAssetFileWrite.Begin(path, bytes, null))
            {
                Until(() => write.IsComplete);
                Check(write.TryCommit(out _), "Atomic replacement");
                Check(File.ReadAllBytes(path).SequenceEqual(bytes), "Committed payload");
            }
            using (var first = MaterialAssetFileRead.Begin(path))
            using (var second = MaterialAssetFileRead.Begin(path))
            {
                Until(() => first.IsComplete);
                Check(first.Data.SequenceEqual(bytes), "Read payload");
                Check(!second.IsComplete, "Ready buffer must retain the admission slot");
                first.Dispose();
                Until(() => second.IsComplete);
                Check(second.Data.SequenceEqual(bytes), "Next reader must progress after consumption");
            }
            var root = new UnityEngine.GameObject();
            var material = new UnityEngine.Material();
            var target = new MaterialEditTarget(root, material, "Tex");
            var mainThread = Environment.CurrentManagedThreadId;
            using (var disabledOwner = new MaterialEditRequestQueue())
            {
                disabledOwner.EnqueueFile(target, () => true, path, data => true, r => { });
                disabledOwner.Pump();
                using (var followingReader = MaterialAssetFileRead.Begin(path))
                {
                    disabledOwner.CancelAll();
                    Until(() => followingReader.IsComplete);
                    Check(followingReader.Data.SequenceEqual(bytes), "Disabled owner releases shared read admission without another pump");
                }
            }
            var child = new UnityEngine.GameObject();
            child.transform.parent = root.transform;
            var childTarget = new MaterialEditTarget(child, material, "Tex");
            Check(childTarget.Matches(root, "material", "Tex"), "Parent cancels child scope");
            Check(target.Matches(child, "material", "Tex"), "Child invalidates overlapping parent scope");
            Check(!childTarget.Matches(new UnityEngine.GameObject(), "material", "Tex"), "Unrelated objects stay isolated");
            Check(!childTarget.SameProperty(target), "Watcher coalescing keeps exact scope");
            using (var queue = new MaterialEditRequestQueue())
            {
                var completions = 0;
                queue.Enqueue(target, () => true, done => () => { }, r => completions++,
                    advance: () => { queue.Dispose(); throw new Exception("Reentrant disposal"); });
                queue.Pump();
                queue.Pump();
                Check(completions == 1, "Reentrant disposal completes once and contains the exception");
            }
            using (var queue = new MaterialEditRequestQueue())
            {
                var order = new List<int>();
                var completed = 0;
                for (var i = 0; i < 3; i++)
                {
                    var id = i;
                    queue.EnqueueFile(target, () => true, path, data =>
                    {
                        Check(Environment.CurrentManagedThreadId == mainThread, "Apply must stay on the pumping thread");
                        Check(data.SequenceEqual(bytes), "Apply payload");
                        order.Add(id);
                        return true;
                    }, result => { Check(result.Succeeded, "Completion result"); completed++; });
                }
                Until(() => completed == 3, queue.Pump);
                Check(order.SequenceEqual(new[] { 0, 1, 2 }), "Import ordering");
            }
            using (var queue = new MaterialEditRequestQueue())
            using (var blocker = MaterialAssetFileRead.Begin(path))
            {
                var completions = 0;
                var applied = false;
                var cancel = queue.EnqueueFile(target, () => true, path,
                    data => { applied = true; return true; },
                    result => { Check(result.Status == MaterialEditStatus.Cancelled, "Cancelled result"); completions++; });
                queue.Pump();
                cancel(); cancel(); blocker.Dispose(); queue.Pump();
                Check(completions == 1 && !applied, "Cancellation must complete once without application");
            }
            using (var queue = new MaterialEditRequestQueue())
            {
                MaterialEditResult result = null;
                queue.EnqueueFile(target, () => true, path + ".missing", data => true, r => result = r);
                Until(() => result != null, queue.Pump);
                Check(result.Status == MaterialEditStatus.Failed && result.Stage == "Read", "Read errors must be reported");
            }
            Console.WriteLine("Asset scheduling checks passed (managed stand-ins; no Unity runtime coverage).");
        }
        finally { File.Delete(path); }
    }
}

namespace UnityEngine
{
    public static class Time { public static int frameCount; }
    public class GameObject { public Transform transform = new Transform(); }
    public class Transform
    {
        public Transform parent;
        public bool IsChildOf(Transform other)
        { for (var node = this; node != null; node = node.parent) if (ReferenceEquals(node, other)) return true; return false; }
    }
    public class Shader { }
    public class Material { public Shader shader = new Shader(); }
}
namespace MaterialEditorAPI
{
    public sealed class TestLogger { public void LogWarning(object value) { } }
    public static class MaterialEditorPluginBase { public static TestLogger Logger = new TestLogger(); }
    public static class Names { public static string NameFormatted(this UnityEngine.Material value) => "material"; }
    public static class MaterialEditorCubemapProjection
    {
        public static bool TryValidateSourceFileLength(long length, out string error)
        { error = null; return length > 0; }
    }
}
