using DiffFixerAlgorithm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using UnityDiffFixer;

namespace UnityDiffFixerCommandLine
{
    public class Program
    {
        private Options m_options;

        [STAThread]
        static int Main(string[] args)
        {
            var program = new Program();
            return program.Run(args);
        }

        private int Run(string[] args)
        {
            try
            {
                m_options = GetOptions();
                if (m_options == null)
                {
                    throw new Exception("Option parsing in invalid");
                }
            }
            catch (Exception e)
            {
                ShowPopup("Internal error. Cannot get options: " + e.Message, MsgType.Error, null);
                ReadLine();
                return -1;
            }

            if (args.Length == 0)
            {
                return RunGUICase();
            }
            else
            {
                if (args.Length != 2)
                {
                    if (m_options.IsPrintingToConsole)
                    {
                        ShowPopup("incorrect number of parameters", MsgType.Error, m_options);
                    }
                    if (m_options.WaitBeforeExit)
                    {
                        ReadLine();
                    }
                    return -1;
                }

                return RunNormalCase(args[0], args[1]);
            }
        }

        private int RunGUICase()
        {
            var gui = new GUIRunner(this);
            gui.ShowDialog();
            return gui.GetReturnCode(); ;
        }

        private static Options GetOptions()
        {
            var locationExe = System.Reflection.Assembly.GetEntryAssembly().Location;
            var locationDirExe = Path.GetDirectoryName(locationExe);
            Options options = null;
            var jsonOptionsPath = Path.Combine(locationDirExe, "options.json");
            var bytes = File.ReadAllBytes(jsonOptionsPath);
            var utf8Reader = new Utf8JsonReader(bytes);
            options = JsonSerializer.Deserialize<Options>(ref utf8Reader);
            return options;
        }

        public int RunFromGUI(string oldFilePath, string newFilePath)
        {
            return RunNormalCase(oldFilePath, newFilePath);
        }

        private int RunNormalCase(string oldFilePathArg, string newFilePathArg)
        {
            try
            {
                var printAction = new Action<string>(a =>
                {
                    if (m_options.IsPrintingToConsole)
                    {
                        Console.WriteLine(a);
                    }
                });

                string before = File.ReadAllText(oldFilePathArg);
                string after = File.ReadAllText(newFilePathArg);

                if (m_options.ShouldDoBackup)
                {
                    try
                    {
                        var filename = Path.GetFileName(newFilePathArg);
                        var extension = Path.GetExtension(filename);
                        var nameWithoutExtension = Path.GetFileNameWithoutExtension(newFilePathArg);
                        var timestamp = DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss-fff");
                        var backupDirPath = Environment.ExpandEnvironmentVariables(m_options.BackupDirectoryPath);
                        Directory.CreateDirectory(backupDirPath);

                        var backupPath = Path.Combine
                        (
                            backupDirPath,
                            nameWithoutExtension + "-" + timestamp + extension
                        );

                        File.WriteAllText(backupPath, after);
                    }
                    catch (Exception e)
                    {
                        if (m_options.ShouldDoBackup && m_options.IsPrintingToConsole)
                        {
                            ShowPopup("Aborting because we cannot do backup: " + e.Message, MsgType.Error, m_options);
                        }
                        if (m_options.WaitBeforeExit)
                        {
                            ReadLine();
                        }
                        return -1;
                    }
                }

                var oldLines = StringUtils.GetAllLinesFromText(before);
                var newLines = StringUtils.GetAllLinesFromText(after);

                if (m_options.ShouldTryToFixDiff)
                {
                    var oldDocument = UnityYAMLDocument.ParseUnityYAMLdocument(oldLines);
                    var newDocument = UnityYAMLDocument.ParseUnityYAMLdocument(newLines);

                    var diffOptions =
                        new DiffOptions
                        (
                            m_options.IsPrintingToConsole
                        );

                    var comparerAndFixer = new UnityDiffComparerAndFixer(oldDocument, newDocument, diffOptions);

                    var newNbLines = newLines.Count;

                    var fixedYaml = comparerAndFixer.GetFixedVersion(printAction);

                    var FixedNbLines = StringUtils.GetAllLinesFromText(fixedYaml).Count;

                    if (newNbLines != FixedNbLines && m_options.IsPrintingToConsole)
                    {
                        ShowPopup
                        (
                            $"ERROR !!! Fixing YAML changed lines count: original: {newNbLines}, fixed: {FixedNbLines}",
                            MsgType.Error,
                            m_options
                        );
                    }

                    if (m_options.ShouldSortByComponentID)
                    {
                        var sortedYaml = YamlSorter.SortAsPrevious(before, fixedYaml, printAction);
                        var sortedNbLines = StringUtils.GetAllLinesFromText(sortedYaml).Count;
                        if (sortedNbLines != FixedNbLines && m_options.IsPrintingToConsole)
                        {
                            ShowPopup
                            (
                                $"ERROR !!! Sorting YAML changed lines count: fixed: {FixedNbLines}, sorted: {sortedNbLines}",
                                MsgType.Error,
                                m_options
                            );
                        }

                        fixedYaml = sortedYaml;
                    }

                    File.WriteAllText(newFilePathArg, fixedYaml);
                }
                else
                {
                    if (m_options.ShouldSortByComponentID)
                    {
                        var fixedYaml = YamlSorter.SortAsPrevious(before, after, printAction);
                        var sortedNbLines = StringUtils.GetAllLinesFromText(fixedYaml).Count;
                        var nbLines = StringUtils.GetAllLinesFromText(after).Count;
                        if (sortedNbLines != nbLines && m_options.IsPrintingToConsole)
                        {
                            ShowPopup
                            (
                                $"ERROR !!! Sorting YAML changed lines count: original: {nbLines}, sorted: {sortedNbLines}",
                                MsgType.Error,
                                m_options
                            );
                        }
                        File.WriteAllText(newFilePathArg, fixedYaml);
                    }
                    else
                    {
                        File.WriteAllText(newFilePathArg, after);
                    }
                }

                if (!string.IsNullOrWhiteSpace(m_options.DiffExePath))
                {
                    try
                    {
                        var process = new Process();
                        var arguments = "";
                        if (!string.IsNullOrWhiteSpace(m_options.DiffArguments))
                        {
                            arguments = string.Format(m_options.DiffArguments, oldFilePathArg, newFilePathArg);
                        }

                        var path = Environment.ExpandEnvironmentVariables(m_options.DiffExePath);

                        process.StartInfo.FileName = path;
                        process.StartInfo.Arguments = arguments;
                        printAction($"Starting process {path} {arguments}");
                        printAction("==============================");
                        process.Start();
                        process.WaitForExit();
                    }
                    catch (Exception e)
                    {
                        if (m_options.IsPrintingToConsole)
                        {
                            ShowPopup
                            (
                                "Error when trying to launch exe: " + e.Message,
                                MsgType.Error,
                                m_options
                            );
                        }

                        if (m_options.WaitBeforeExit)
                        {
                            ReadLine();
                        }
                        return -1;
                    }
                }
                if (m_options.WaitBeforeExit)
                {
                    ReadLine();
                }
                return 0;
            }
            catch (Exception e)
            {
                ShowPopup("Internal error: " + e.Message, MsgType.Error, null);

                ReadLine();
                return -1;
            }
        }

        private void ReadLine()
        {
            if (m_options != null && m_options.WaitBeforeExit)
            {
                Console.WriteLine("Push Enter Key To Continue");
                Console.ReadLine();
            }
        }

        public enum MsgType
        {
            Error,
            Warning,
            Information
        }

        private static void ShowPopup(string msg, MsgType msgType, Options options)
        {
            if (options != null && options.IsPrintingToConsole)
            {
                Console.WriteLine(msg);
            }

            MessageBox.Show
            (
                msg,
                GetTitle(msgType),
                MessageBoxButtons.OK,
                MsgTypeToMessageBoxIcon(msgType)
            );
        }

        private static MessageBoxIcon MsgTypeToMessageBoxIcon(MsgType msgType)
        {
            switch (msgType)
            {
                case MsgType.Error: return MessageBoxIcon.Error;
                case MsgType.Warning: return MessageBoxIcon.Warning;
                case MsgType.Information: return MessageBoxIcon.Information;  
            }

            return MessageBoxIcon.Information;
        }

        private static string GetTitle(MsgType msgType)
        {
            switch (msgType)
            {
                case MsgType.Error: return "Error";
                case MsgType.Warning: return "Warning";
                case MsgType.Information: return "Information";
            }

            return "";
        }
    }
}
