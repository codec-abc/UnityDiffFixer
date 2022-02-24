using System;

namespace DiffFixerAlgorithm.FixHeuristics
{
    public class SmallPercentDifference : INumberComparer
    {
        bool INumberComparer.AreSame(double oldValue, double newValue)
        {

            var diff = Math.Abs(oldValue - newValue);

            var oldDiff = Math.Abs(diff / oldValue);
            var newDiff = Math.Abs(diff / newValue);

            if (oldDiff < 0.005 && newDiff < 0.005)
            {
                return true;
            }

            return false;
        }

        
    }
}
