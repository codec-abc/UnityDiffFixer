namespace DiffFixerAlgorithm
{
    public interface INumberComparer
    {
        bool AreSame(double oldValue, double newValue);
    }
}
