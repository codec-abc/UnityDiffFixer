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

            var oldValueFloor = Math.Floor(oldValue);
            var newValueFloor = Math.Floor(oldValue);

            bool areBothInteger = 
                oldValue - oldValueFloor == 0.0 &&
                newValue - newValueFloor == 0.0;

            // Don't revert small percent diff for integers as it can be used for bitfields
            if (oldDiff < 0.005 && newDiff < 0.005 && !areBothInteger)
            {
                return true;
            }

            return false;
        }

        
    }
}
