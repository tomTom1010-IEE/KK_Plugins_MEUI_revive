using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>Main-thread cooperative admission shared by asynchronous asset work.
    /// Waiting owners retain FIFO priority; inactive owners cannot strand admission.
    /// A Unity call is indivisible and may exceed the remaining frame budget.</summary>
    internal static class MaterialFrameWorkScheduler
    {
        private sealed class Waiter { internal object Owner; internal int Frame; }
        private sealed class Slice : IDisposable
        {
            private readonly long _start = Stopwatch.GetTimestamp();
            private bool _disposed;
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _spent += Stopwatch.GetTimestamp() - _start;
                _running = false;
            }
        }
        private static readonly List<Waiter> Waiting = new List<Waiter>();
        private static int _frame = -1;
        private static long _spent;
        private static bool _running;
        private static long _sliceStarted;
        internal static bool BudgetExhausted => _running
            && _spent + Stopwatch.GetTimestamp() - _sliceStarted
                >= MaterialWorkBudget.DefaultMilliseconds * Stopwatch.Frequency / 1000.0;

        internal static IDisposable TryEnter(object owner)
        {
            var frame = Time.frameCount;
            if (_frame != frame)
            {
                _frame = frame;
                _spent = 0;
                Waiting.RemoveAll(x => x.Frame < frame - 1);
            }
            var waiter = Waiting.Find(x => ReferenceEquals(x.Owner, owner));
            if (waiter == null) { waiter = new Waiter { Owner = owner }; Waiting.Add(waiter); }
            waiter.Frame = frame;
            if (_running || !ReferenceEquals(Waiting[0], waiter)
                || _spent >= MaterialWorkBudget.DefaultMilliseconds * Stopwatch.Frequency / 1000.0)
                return null;
            Waiting.RemoveAt(0);
            _running = true;
            _sliceStarted = Stopwatch.GetTimestamp();
            return new Slice();
        }

        internal static void Release(object owner) => Waiting.RemoveAll(x => ReferenceEquals(x.Owner, owner));
    }
}
