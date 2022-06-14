using System;
using System.Collections.Generic;
using System.Dynamic;
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
        private readonly UnityClassObject m_unityClass;
        private readonly string m_id;
        private readonly IndexedLine m_originalHeaderLine;
        private readonly string m_guid;
        private List<YamlQuery> m_queries;
        private YamlStream m_yamlStream;
        private string m_source;
        private UnityYAMLDocument m_document;

        public UnityYAMLRootObject(
            UnityClassObject unityClass,
            string objectId,
            IndexedLine objectHeaderLine,
            string guid)
        {
            m_unityClass = unityClass;
            m_id = objectId;
            m_originalHeaderLine = objectHeaderLine;
            m_guid = guid;
        }

        public override bool Equals(object obj)
        {
            return obj is UnityYAMLRootObject @object &&
                   m_unityClass == @object.m_unityClass &&
                   m_id.Equals(@object.m_id);
        }

        public string GetGUID()
        {
            return m_guid;
        }

        public override int GetHashCode()
        {
            int seed = 1009;
            int factor = 9176;
            var hash = seed;

            hash = hash * factor + m_unityClass.GetHashCode();
            hash = hash * factor + m_id.GetHashCode();

            return hash;
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

        internal string GetSource()
        {
            return m_source;
        }

        internal YamlStream GetYamlStream()
        {
            return m_yamlStream;
        }

        internal List<YamlQuery> GetTerminalQueries()
        {
            return m_queries;
        }

        internal UnityYAMLDocument GetYamlDocument()
        {
            return m_document;
        }

        internal void SetTerminalQueriesByUnityComponents(List<YamlQuery> queries)
        {
            m_queries = queries;
        }

        internal void SetYamlStream(YamlStream stream)
        {
            m_yamlStream = stream;
        }

        internal void SetSource(string source)
        {
            m_source = source;
        }

        internal void SetDocument(UnityYAMLDocument document)
        {
            m_document = document;
        }

        internal YamlNode RunQuery(List<YamlQuery> queryChain)
        {
            return YamlQueryUtils.RunQueryChain(queryChain, GetYamlStream());
        }
    }

    public enum PropType
    {
        Number,
        String,
    }

    public class UnityYAMLProperty
    {
        public object Value { get; set; }

        public PropType Type { get; set; }

        public string Name { get; set; }
    }

    public class UnityYAMLObject
    {
        public List<UnityYAMLProperty> Props { get; set; }

        public List<UnityYAMLObject> Childs { get; set; }
    }

    public class UnityYAMLHeader
    {
        private readonly YAMLVersion m_version;
        private readonly TagVersion m_tagVersion;
        private readonly string m_source;

        public class YAMLVersion
        {
            private readonly int m_lineIndex;
            private readonly string m_version;

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
            private readonly int m_lineIndex;
            private readonly string m_tagValue;

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
            return sorted;
        }

        public static string SortAsPrevious(string oldContent, string newContent, Action<string> printAction)
        {
            var oldLines = StringUtils.GetAllLinesFromText(oldContent);
            var oldDocument = UnityYAMLDocument.ParseUnityYAMLdocument(oldLines, null);

            var newLines = StringUtils.GetAllLinesFromText(newContent);
            var newDocument = UnityYAMLDocument.ParseUnityYAMLdocument(newLines, null);

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

                         existingCompInPreviousVersion.Add(
                             new Tuple<KeyValuePair<UnityYAMLRootObject, YamlStream>, int>(c, oldIndex));
                     }
                     else
                     {
                         previousOldIndex = null;
                         notExistingCompInPreviousVersion.Add(c);
                     }
                 });

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

    public static class YamlUtils
    {
        public static dynamic ToExpando(this UnityYAMLRootObject obj)
        {
            return ToExpando(obj.GetSource());
        }

        public static Dictionary<string, object> ExpandoToDictionary(ExpandoObject obj)
        {
            var returned = new Dictionary<string, object>();

            foreach (var property in (IDictionary<string, object>)obj)
            {
                if (property.Value is ExpandoObject expObj)
                {
                    returned.Add(property.Key, ExpandoToDictionary(expObj));
                }
                else
                {
                    returned.Add(property.Key, property.Value);
                }
            }

            // {
            //    if (kvp.Value is ExpandoObject expObj)
            //    {
            //        returned[kvp.Key] = ExpandoToDictionary(expObj);
            //    }
            // }
            return returned;
        }

        /// <summary>
        /// Converts a YAML string to an <code>ExpandoObject</code>.
        /// </summary>
        /// <param name="yaml">The YAML string to convert.</param>
        /// <returns>Converted object.</returns>
        public static ExpandoObject ToExpando(string yaml)
        {
            using (var sr = new StringReader(yaml))
            {
                var stream = new YamlStream();
                stream.Load(sr);
                var firstDocument = stream.Documents[0].RootNode;
                dynamic exp = ToExpando(firstDocument);
                return exp;
            }
        }

        /// <summary>
        /// Converts a YAML node to an <code>ExpandoObject</code>.
        /// </summary>
        /// <param name="node">The node to convert.</param>
        /// <returns>Converted object.</returns>
        public static ExpandoObject ToExpando(YamlNode node)
        {
            ExpandoObject exp = new ExpandoObject();
            exp = (ExpandoObject)ToExpandoImpl(exp, node);
            return exp;
        }

        static object ToExpandoImpl(ExpandoObject parent, YamlNode node)
        {
            if (node is YamlScalarNode scalar)
            {
                // TODO: Try converting to double, DateTime and return that.
                string val = scalar.Value;
                return val;
            }
            else if (node is YamlMappingNode mapping)
            {
                foreach (KeyValuePair<YamlNode, YamlNode> child in mapping.Children)
                {
                    YamlScalarNode keyNode = (YamlScalarNode)child.Key;
                    string keyName = keyNode.Value;
                    var exp = new ExpandoObject();
                    object val = ToExpandoImpl(exp, child.Value);

                    // exp[keyName] = val;
                    // if (exp.ContainsKey(keyName))
                    // {
                    //    exp.Remove(keyName);
                    // }
                    // exp.KeyName = val;
                    // exp.TryAdd(keyName, val);
                    parent.SetProperty(keyName, val);
                }
            }
            else if (node is YamlSequenceNode sequence)
            {
                var childNodes = new List<object>();
                foreach (YamlNode child in sequence.Children)
                {
                    var childExp = new ExpandoObject();
                    object childVal = ToExpandoImpl(childExp, child);
                    childNodes.Add(childVal);
                }
                return childNodes;
            }

            return parent;
        }

        public static void SetProperty(this IDictionary<string, object> target, string name, object thing)
        {
            target[name] = thing;
        }

        public static ExpandoObject CreateChildProp(ExpandoObject obj, string name)
        {
            IDictionary<string, object> myUnderlyingObject = obj;
            var prop = new ExpandoObject();
            myUnderlyingObject.Add(name, prop);
            return prop;
        }

        public static void CreateChildStringProp(ExpandoObject obj, string name, string value)
        {
            IDictionary<string, object> myUnderlyingObject = obj;
            myUnderlyingObject.Add(name, value);
        }

        public static List<YamlQuery> GetScriptGuidQueryChain()
        {
            var root = new YamlDummyQuery();
            var monoBehaviourQuery = new YamlQueryByName(root, "MonoBehaviour");
            var scriptQuery = new YamlQueryByName(monoBehaviourQuery, "m_Script");
            var guid = new YamlQueryByName(scriptQuery, "guid");

            return new List<YamlQuery>()
            {
                root,
                monoBehaviourQuery,
                scriptQuery,
                guid,
            };
        }

        public static void RemoveProps(ExpandoObject expObj, string propName)
        {
            ((IDictionary<string, object>)expObj).Remove(propName);
        }
    }

    public class UnityFileEntry
    {
        public UnityFileEntry(
            string path,
            string content,
            List<string> lines,
            UnityYAMLDocument yamlDoc)
        {
            Path = path;
            Content = content;
            Lines = lines;
            YamlDoc = yamlDoc;
        }

        public string Path { get; set; }

        public string Content { get; set; }

        public List<string> Lines { get; set; }

        public UnityYAMLDocument YamlDoc { get; set; }
    }
}