using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace UnityDiffFixer
{
    public class IndexedLine
    {
        private int m_lineIndex;
        private string m_lineContent;

        public IndexedLine(int lineIndex, string lineContent)
        {
            LineIndex = lineIndex;
            m_lineContent = lineContent;
        }

        public int LineIndex { get => m_lineIndex; private set => m_lineIndex = value; }
        public string LineContent { get => m_lineContent; private set => m_lineContent = value; }
    }

    public class UnityYAMLRootObject
    {
        UnityClassObject m_unityClass;
        string m_id;
        IndexedLine m_originalHeaderLine;

        public UnityYAMLRootObject(UnityClassObject unityClass, string objectId, IndexedLine objectHeaderLine)
        {
            m_unityClass = unityClass;
            m_id = objectId;
            m_originalHeaderLine = objectHeaderLine;
        }

        public override bool Equals(object obj)
        {
            return obj is UnityYAMLRootObject @object &&
                   m_unityClass == @object.m_unityClass &&
                   m_id.Equals(@object.m_id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(m_unityClass, m_id);
        }

        public UnityClassObject GetUnityClassObject()
        {
            return m_unityClass;
        }

        public string GetId()
        {
            return m_id;
        }

        public IndexedLine GetOriginalLine()
        {
            return m_originalHeaderLine;
        }

        public override string ToString()
        {
            return "" + m_id.ToString();
        }
    }

    enum PropType
    {
        Number,
        String
    }

    class UnityYAMLProperty
    {
        Object Value;
        PropType Type;
        string Name;
    }

    class UnityYAMLObject
    {
        List<UnityYAMLProperty> Props;
        List<UnityYAMLObject> Childs;
    }

    public class UnityYAMLHeader
    {
        YAMLVersion m_version;
        TagVersion m_tagVersion;
        string m_source;


        public class YAMLVersion
        {
            public int m_lineIndex;
            string m_version;

            public YAMLVersion(string yamlVersionLine, int lineIndex)
            {
                this.m_version = yamlVersionLine;
                this.m_lineIndex = lineIndex;
            }
        }

        public string GetOriginalSource()
        {
            return m_source;
        }

        public class TagVersion
        {
            int m_lineIndex;
            string m_tagValue;

            public TagVersion(string tagVersionLine, int lineIndex)
            {
                this.m_tagValue = tagVersionLine;
                this.m_lineIndex = lineIndex;
            }
        }

        public UnityYAMLHeader(YAMLVersion version, TagVersion tagVersion, string source)
        {
            m_version = version;
            m_tagVersion = tagVersion;
            m_source = source;
        }
    }

    public static class StringUtils
    {
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
    }

    public class YamlSorter
    {
        public static string SortAscending(string content)
        {
            var lines = StringUtils.GetAllLinesFromText(content);
            var document = UnityYAMLDocument.ParseUnityYAMLdocument(lines);

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
            return sorted;
        }

        public static string SortAsPrevious(string oldContent, string newContent, Action<string> printAction)
        {
            var oldLines = StringUtils.GetAllLinesFromText(oldContent);
            var oldDocument = UnityYAMLDocument.ParseUnityYAMLdocument(oldLines);

            var newLines = StringUtils.GetAllLinesFromText(newContent);
            var newDocument = UnityYAMLDocument.ParseUnityYAMLdocument(newLines);

            var streamByComponentDictOldDoc = oldDocument.GetYamlStreamByComponent();
            var streamByComponentOldDoc = streamByComponentDictOldDoc.ToList();

            var streamByComponentDictNewDoc = newDocument.GetYamlStreamByComponent();
            var streamByComponentNewDoc = streamByComponentDictNewDoc.ToList();

            var notExistingCompInPreviousVersion = new List<KeyValuePair<UnityYAMLRootObject, YamlStream>>();
            var existingCompInPreviousVersion = new List<Tuple<KeyValuePair<UnityYAMLRootObject, YamlStream>, int>>();

            int? previousOldIndex = null;
            int inOrder = 0;
            int notInOrder = 0;

            streamByComponentNewDoc.ForEach
            (
                 c =>
                 {
                     var id = c.Key.GetId();
                     var oldIndex = streamByComponentOldDoc.FindIndex(oldC => oldC.Key.GetId() == id);
                     
                     if (oldIndex >= 0)
                     {
                         if (previousOldIndex != null)
                         {
                             if (previousOldIndex.Value + 1 == oldIndex)
                             {
                                 inOrder++;
                             } 
                             else
                             {
                                 notInOrder++;
                             }
                         }

                         previousOldIndex = oldIndex;

                         existingCompInPreviousVersion.Add
                        (
                             new Tuple<KeyValuePair<UnityYAMLRootObject, YamlStream>, int>(c, oldIndex)
                        );
                     } 
                     else
                     {
                         previousOldIndex = null;
                         notExistingCompInPreviousVersion.Add(c);
                     }
                 }
            );

            existingCompInPreviousVersion.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            notExistingCompInPreviousVersion.Sort((a, b) => a.Key.GetId().CompareTo(b.Key.GetId()));

            StringBuilder builder = new StringBuilder();
            builder.Append(newDocument.GetOriginalHeader());

            foreach (var kvp in existingCompInPreviousVersion)
            {
                builder.Append(kvp.Item1.Key.GetOriginalLine().LineContent);
                builder.Append(LineEndings.NewLine);
                var source = newDocument.GetSourceForComponent(kvp.Item1.Key);
                builder.Append(source);
            }

            foreach (var kvp in notExistingCompInPreviousVersion)
            {
                builder.Append(kvp.Key.GetOriginalLine().LineContent);
                builder.Append(LineEndings.NewLine);
                var source = newDocument.GetSourceForComponent(kvp.Key);
                builder.Append(source);
            }

            printAction($"Number of blocks in same order {inOrder}");
            printAction($"Number of blocks in different order {notInOrder}");
            printAction($"Number of new blocks {notExistingCompInPreviousVersion.Count}");

            var sorted = builder.ToString();
            return sorted;
        }

    }
}
