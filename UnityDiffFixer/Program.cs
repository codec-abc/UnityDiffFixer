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
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                var locationExe = System.Reflection.Assembly.GetEntryAssembly().Location;
                var locationDirExe = Path.GetDirectoryName(locationExe);
                Options options = null;// Options.GetDefault(locationDirExe);

                var printAction = new Action<string>(a =>
                {
                    if (options.IsPrintingToConsole)
                    {
                        Console.WriteLine(a);
                    }
                });

                try
                {
                    var jsonOptionsPath = Path.Combine(locationDirExe, "options.json");
                    var bytes = File.ReadAllBytes(jsonOptionsPath);
                    var utf8Reader = new Utf8JsonReader(bytes);
                    options = JsonSerializer.Deserialize<Options>(ref utf8Reader);
                }
                catch (Exception e)
                {
                    if (options.IsPrintingToConsole)
                    {
                        ShowPopup("Error " + e.ToString(), MsgType.Error, options);
                    }
                }

                if (args.Length != 2)
                {
                    if (options.IsPrintingToConsole)
                    {
                        ShowPopup("incorrect number of parameters", MsgType.Error, options);
                    }
                    if (options.WaitBeforeExit)
                    {
                        ReadLine();
                    }
                    return -1;
                }

                if (options.IsPrintingToConsole)
                {
                    //ShowPopup(args[0] + " " + args[1], MsgType.Information);
                }

                string before = File.ReadAllText(args[0]);
                string after = File.ReadAllText(args[1]);

                if (options.ShouldDoBackup)
                {
                    try
                    {
                        var filename = Path.GetFileName(args[1]);
                        var extension = Path.GetExtension(filename);
                        var nameWithoutExtension = Path.GetFileNameWithoutExtension(args[1]);
                        var timestamp = DateTime.Now.ToString("yyyy-MM-dd--hh-mm-ss-fff");
                        var backupDirPath = Environment.ExpandEnvironmentVariables(options.BackupDirectoryPath);
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
                        if (options.ShouldDoBackup && options.IsPrintingToConsole)
                        {
                            ShowPopup("Aborting because we cannot do backup: " + e.Message, MsgType.Error, options);
                        }
                        if (options.WaitBeforeExit)
                        {
                            ReadLine();
                        }
                        return -1;
                    }
                }

                var oldLines = StringUtils.GetAllLinesFromText(before);
                var newLines = StringUtils.GetAllLinesFromText(after);

                if (options.ShouldTryToFixDiff)
                {
                    var oldDocument = UnityYAMLDocument.ParseUnityYAMLdocument(oldLines);
                    var newDocument = UnityYAMLDocument.ParseUnityYAMLdocument(newLines);

                    var diffOptions = 
                        new DiffOptions
                        (
                            options.IsPrintingToConsole
                        );

                    var comparerAndFixer = new UnityDiffComparerAndFixer(oldDocument, newDocument, diffOptions);

                    var newNbLines = newLines.Count;

                    var fixedYaml = comparerAndFixer.GetFixedVersion(printAction);

                    var FixedNbLines = StringUtils.GetAllLinesFromText(fixedYaml).Count;

                    if (newNbLines != FixedNbLines && options.IsPrintingToConsole)
                    {
                        ShowPopup
                        (
                            $"ERROR !!! Fixing YAML changed lines count: original: {newNbLines}, fixed: {FixedNbLines}", 
                            MsgType.Error, 
                            options
                        );
                    }

                    if (options.ShouldSortByComponentID)
                    {
                        var sortedYaml = YamlSorter.SortAsPrevious(before, fixedYaml, printAction);
                        var sortedNbLines = StringUtils.GetAllLinesFromText(sortedYaml).Count;
                        if (sortedNbLines != FixedNbLines && options.IsPrintingToConsole)
                        {
                            ShowPopup
                            (
                                $"ERROR !!! Sorting YAML changed lines count: fixed: {FixedNbLines}, sorted: {sortedNbLines}", 
                                MsgType.Error, 
                                options
                            );
                        }

                        fixedYaml = sortedYaml;
                    }

                    File.WriteAllText(args[1], fixedYaml);
                }
                else
                {
                    if (options.ShouldSortByComponentID)
                    {
                        var fixedYaml = YamlSorter.SortAsPrevious(before, after, printAction);
                        var sortedNbLines = StringUtils.GetAllLinesFromText(fixedYaml).Count;
                        var nbLines = StringUtils.GetAllLinesFromText(after).Count;
                        if (sortedNbLines != nbLines && options.IsPrintingToConsole)
                        {
                            ShowPopup
                            (
                                $"ERROR !!! Sorting YAML changed lines count: original: {nbLines}, sorted: {sortedNbLines}", 
                                MsgType.Error,
                                options
                            );
                        }
                        File.WriteAllText(args[1], fixedYaml);
                    }
                    else
                    {
                        File.WriteAllText(args[1], after);
                    }
                }

                if (!string.IsNullOrWhiteSpace(options.DiffExePath))
                {
                    try
                    {
                        var process = new Process();
                        var arguments = "";
                        if (!string.IsNullOrWhiteSpace(options.DiffArguments))
                        {
                            arguments = string.Format(options.DiffArguments, args[0], args[1]);
                        }

                        var path = Environment.ExpandEnvironmentVariables(options.DiffExePath);

                        process.StartInfo.FileName = path;
                        process.StartInfo.Arguments = arguments;
                        process.Start();
                        process.WaitForExit();
                    }
                    catch (Exception e)
                    {
                        if (options.IsPrintingToConsole)
                        {
                            ShowPopup
                            (
                                "Error when trying to launch exe: " + e.Message, 
                                MsgType.Error,
                                options
                            );
                        }

                        if (options.WaitBeforeExit)
                        {
                            ReadLine();
                        }
                        return -1;
                    }
                }
                if (options.WaitBeforeExit)
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

        private static void ReadLine()
        {
            Console.WriteLine("Push Enter Key To Continue");
            Console.ReadLine();
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
