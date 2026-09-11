using System;
using System.Diagnostics;

namespace MaterialEditorAPI
{
    /// <summary>A cooperative budget between bounded units; never preempts a Unity call.</summary>
    internal struct MaterialWorkBudget
    {
        internal const double DefaultMilliseconds = 2.0;
        internal const int DefaultRowLimit = 16;
        private readonly long _started;
        private readonly double _ticks;
        private readonly int _limit;
        private int _units;

        internal MaterialWorkBudget(double milliseconds, int limit)
        {
            if (milliseconds <= 0 || double.IsNaN(milliseconds) || double.IsInfinity(milliseconds))
                throw new ArgumentOutOfRangeException("milliseconds");
            if (limit <= 0) throw new ArgumentOutOfRangeException("limit");
            _started = Stopwatch.GetTimestamp();
            _ticks = milliseconds * Stopwatch.Frequency / 1000.0;
            _limit = limit;
            _units = 0;
        }

        internal bool TryStartUnit()
        {
            if (_units >= _limit || (_units != 0 && Stopwatch.GetTimestamp() - _started >= _ticks))
                return false;
            _units++;
            return true;
        }
    }
}
