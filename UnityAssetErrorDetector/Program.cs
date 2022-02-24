using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityDiffFixer;

class Program
{
    public static int Main(string[] args)
    {
        var assetDir = @"C:\Workplace\repos\BTP_2\Assets";

        var nbError = 0;

        string[] extensions =
        {
            "*.asset",
            //"*.meta",
            "*.unity",
            "*.prefab"
        };

        var files = MyDirectory.GetFiles(assetDir, extensions, SearchOption.AllDirectories).ToList();

        Console.WriteLine($"starting");
        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);

            if (fileInfo.Exists)
            {
                
                string content = File.ReadAllText(file);

                if (!content.StartsWith("%YAML 1.1"))
                {
                    Console.WriteLine($"skipping {file}");
                    continue;
                }

                var lines = GetAllLinesFromText(content);
                try 
                {
                    //Console.WriteLine($"processing file {file}");
                    var _ = UnityYAMLDocument.ParseUnityYAMLdocument(lines);
                } 
                catch (Exception e)
                {
                    Console.WriteLine($"processing file {file}");
                    Console.WriteLine("Error " + e.Message);
                    Exception? inner = e;
                    while (inner != null)
                    {
                        inner = inner.InnerException;
                        if (inner != null) 
                        { 
                            Console.WriteLine(inner.Message);
                        }
                    }
                    nbError++;
                }
            }
        }

        Console.WriteLine("Done. Press enter to quit");
        Console.ReadLine();
        return 0;
    }

    public static List<string> GetAllLinesFromText(string content)
    {
        List<string> returned = new List<string>();
        using (StringReader sr = new StringReader(content))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                returned.Add(line);
            }
        }
        return returned;
    }

    public static class MyDirectory
    {   // Regex version
        public static IEnumerable<string> GetFiles(string path,
                            string searchPatternExpression = "",
                            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Regex reSearchPattern = new Regex(searchPatternExpression, RegexOptions.IgnoreCase);
            return Directory.EnumerateFiles(path, "*", searchOption)
                            .Where(file =>
                                     reSearchPattern.IsMatch(Path.GetExtension(file)));
        }

        // Takes same patterns, and executes in parallel
        public static IEnumerable<string> GetFiles(string path,
                            string[] searchPatterns,
                            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return searchPatterns.AsParallel()
                   .SelectMany(searchPattern =>
                          Directory.EnumerateFiles(path, searchPattern, searchOption));
        }
    }
}