namespace DiffFixerAlgorithm
{
    public class DiffOptions
    {

        public DiffOptions(bool printToConsole)
        {
            this.ShouldPrintToConsole = printToConsole;
        }

        public bool ShouldPrintToConsole { get; private set; }
    }
}
