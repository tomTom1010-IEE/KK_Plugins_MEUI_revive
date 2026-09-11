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
        var path = Path.GetTempFileName();
        try
        {
            var bytes = new byte[] { 3, 1, 4, 1, 5 };
            File.WriteAllBytes(path, bytes);
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
    public class GameObject { }
    public class Shader { }
    public class Material { public Shader shader = new Shader(); }
}
namespace MaterialEditorAPI
{
    public sealed class TestLogger { public void LogWarning(object value) { } }
    public static class MaterialEditorPluginBase { public static TestLogger Logger = new TestLogger(); }
    public static class Names { public static string NameFormatted(this UnityEngine.Material value) => "material"; }
}
