using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using UnityDiffFixer;

namespace UnityDiffFixerCommandLine
{
    class Program
    {
        static int Main(string[] args)
        {
            string content = File.ReadAllText(args[0]);

            var lines = StringUtils.GetAllLinesFromText(content);
            var document = UnityYAMLDocument.ParseUnityYAMLdocument(lines, null);

            StringBuilder builder = new StringBuilder();
            builder.Append(document.GetOriginalHeader());
            var streamByComponentDict = document.GetYamlStreamByComponent();
            var streamByComponent = streamByComponentDict.ToList();
            streamByComponent.Sort((a, b) => a.Key.GetId().CompareTo(b.Key.GetId()));

            foreach (var kvp in streamByComponent)
            {
                builder.Append(kvp.Key.GetOriginalLine().LineContent);
                builder.Append(LineEndings.NewLine);
                var source = document.GetSourceForComponent(kvp.Key);
                builder.Append(source);
            }

            var sorted = builder.ToString();
            File.WriteAllText(args[1], sorted);
            return 0;
        }
    }
}

