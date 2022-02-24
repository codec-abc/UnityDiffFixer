using System;

namespace DiffFixerAlgorithm.FixHeuristics
{
    public class CloseToZero : INumberComparer
    {
        bool INumberComparer.AreSame(double oldValue, double newValue)
        {

            if (Math.Abs(oldValue) <= 0.0001 && Math.Abs(newValue) <= 0.0001)
            {
                return true;
            }

            return false;
        }
    }
}
