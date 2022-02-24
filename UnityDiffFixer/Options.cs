using System;
using System.IO;
using System.Text.Json;

namespace UnityDiffFixerCommandLine
{
    public class Options
    {
        public bool ShouldDoBackup { get; set; }
        public string BackupDirectoryPath { get; set; }

        public bool IsPrintingToConsole { get; set; }

        public string DiffExePath { get; set; }

        public string DiffArguments { get; set; }

        public bool ShouldTryToFixDiff { get; set; }

        public bool ShouldSortByComponentID { get; set; }

        public bool WaitBeforeExit { get; set; }

        internal static Options GetDefault(string exeDirPath)
        {
            var codePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            codePath = Path.Combine(codePath, @"Programs\Microsoft VS Code\code.exe");
            return new Options()
            {
                ShouldDoBackup = true,
                BackupDirectoryPath = Path.Combine(exeDirPath, "backup"),
                IsPrintingToConsole = true,
                DiffExePath = codePath,
                DiffArguments = " --diff \"{0}\" \"{1}\" "
            };

            //var dir = Path.GetDirectoryName(exeDirPath);
            //var filePath = Path.Combine(dir, "options.json");
            //return JsonSerializer.Deserialize<Options>(File.ReadAllText(filePath));
        }
    }
}
