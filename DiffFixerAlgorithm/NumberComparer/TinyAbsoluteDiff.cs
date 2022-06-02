using System;

namespace DiffFixerAlgorithm.FixHeuristics
{
    public class TinyAbsoluteDiff : INumberComparer
    {
        bool INumberComparer.AreSame(double oldValue, double newValue)
        {
            var diff = Math.Abs(oldValue - newValue);

            if (diff < 0.0001)
            {
                return true;
            }

            return false;
        }

    }
}
