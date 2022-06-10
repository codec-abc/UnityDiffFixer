using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace UnityDiffFixer
{
    public class UnityYAMLDocument
    {
        private readonly Dictionary<UnityYAMLRootObject, List<YamlQuery>> m_terminalQueriesByUnityComponents =
            new Dictionary<UnityYAMLRootObject, List<YamlQuery>>();

        private readonly Dictionary<UnityYAMLRootObject, YamlStream> m_yamlStreamByUnityComponent =
            new Dictionary<UnityYAMLRootObject, YamlStream>();

        private readonly Dictionary<UnityYAMLRootObject, string> m_textSourceByUnityComponent =
            new Dictionary<UnityYAMLRootObject, string>();

        private readonly Parser m_parser;

        private UnityYAMLHeader m_header;

        private string m_guid;

        private static readonly Regex s_objectHeaderRegex =
            new Regex
            (
                @"^--- !u!([0-9]*) &(.*)$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string GetGUID()
        {
            return m_guid;
        }

        public Dictionary<UnityYAMLRootObject, List<YamlQuery>> GetQueriesByComponent()
        {
            return m_terminalQueriesByUnityComponents;
        }

        public Dictionary<UnityYAMLRootObject, YamlStream> GetYamlStreamByComponent()
        {
            return m_yamlStreamByUnityComponent;
        }

        public Dictionary<UnityYAMLRootObject, string> GetYamlSourceByComponent()
        {
            return m_textSourceByUnityComponent;
        }

        public List<YamlQuery> GetYamlQueriesForComponent(UnityYAMLRootObject key)
        {
            return m_terminalQueriesByUnityComponents[key];
        }

        public void RemoveComponentWithId(string id)
        {
            var toRemoves = new List<UnityYAMLRootObject>();

            foreach (var kvp in m_terminalQueriesByUnityComponents)
            {
                if (kvp.Key.GetId() == id)
                {
                    toRemoves.Add(kvp.Key);
                }
            }

            foreach (var toRemove in toRemoves)
            {
                m_terminalQueriesByUnityComponents.Remove(toRemove);
                m_yamlStreamByUnityComponent.Remove(toRemove);
                m_textSourceByUnityComponent.Remove(toRemove);
            }
        }

        public YamlStream GetYamlStreamForComponent(UnityYAMLRootObject key)
        {
            return m_yamlStreamByUnityComponent[key];
        }

        public string GetSourceForComponent(UnityYAMLRootObject key)
        {
            return m_textSourceByUnityComponent[key];
        }

        public bool HasComponent(UnityYAMLRootObject key)
        {
            return m_yamlStreamByUnityComponent.ContainsKey(key);
        }

        private UnityYAMLDocument(Parser parser, string fileGUID)
        {
            this.m_parser = parser;
            this.m_guid = fileGUID;
        }

        public static UnityYAMLDocument ParseUnityYAMLdocument(List<string> lines, string fileGUID)
        {
            Parser parser = new Parser(lines);
            var document = new UnityYAMLDocument(parser, fileGUID);
            document.ParseHeader(document);
            document.ParseObjects(document, fileGUID);
            return document;
        }

        public string GetOriginalHeader()
        {
            return m_header.GetOriginalSource();
        }

        private void ParseHeader(UnityYAMLDocument document)
        {
            // TODO: add check about version
            UnityYAMLHeader.YAMLVersion yamlVersion;
            UnityYAMLHeader.TagVersion tagVersion;
            string headerSource = "";
            var yamlVersionLine = m_parser.GetCurrentLineAndAdvance();
            headerSource += yamlVersionLine.LineContent + LineEndings.NewLine;
            yamlVersion = new UnityYAMLHeader.YAMLVersion(yamlVersionLine.LineContent, yamlVersionLine.LineIndex);
            var tagVersionLine = m_parser.GetCurrentLineAndAdvance();
            headerSource += tagVersionLine.LineContent + LineEndings.NewLine;
            tagVersion = new UnityYAMLHeader.TagVersion(tagVersionLine.LineContent, tagVersionLine.LineIndex);
            document.m_header = new UnityYAMLHeader(yamlVersion, tagVersion, headerSource);
        }

        private void ParseObjects(UnityYAMLDocument document, string fileGUID)
        {
            while (!m_parser.IsAtEnd())
            {
                var objectHeaderLine = m_parser.GetCurrentLineAndAdvance();
                var match = s_objectHeaderRegex.Match(objectHeaderLine.LineContent);

                var objectClassValue = match.Groups[1].ToString();
                long objectClass = long.Parse(objectClassValue);
                var objectIdValue = match.Groups[2].ToString();

                // long objectId = long.Parse(objectIdValue);
                var unityClass = UnityClassObjectUtils.LongToUnityClassObject(objectClass);

                var unityObj = new UnityYAMLRootObject(unityClass, objectIdValue, objectHeaderLine, fileGUID);

                var buffer = new StringBuilder();
                var nextLine = m_parser.PeekCurrentLine();

                while (!nextLine.StartsWith("--- !u") && !m_parser.IsAtEnd())
                {
                    buffer.Append(m_parser.GetCurrentLineAndAdvance().LineContent);
                    buffer.Append(LineEndings.NewLine);
                    if (m_parser.IsAtEnd())
                    {
                        break;
                    }
                    nextLine = m_parser.PeekCurrentLine();
                }

                var stream = new YamlStream();
                var bufferAsString = buffer.ToString();
                var reader = new StringReader(bufferAsString);

                try
                {
                    stream.Load(reader);
                }
                catch (System.Exception e)
                {
                    var msg = "Cannot parse " + unityObj.GetOriginalLine().LineContent + "\n";
                    msg += "Internal error: " + e.ToString() + "\n";
                    msg += "\nWith Content:\n";
                    msg += bufferAsString;
                    throw new System.Exception(msg, e);
                }

                var queryBuilderVisitor = new YamlTerminalQueryBuilderVisitor();
                stream.Accept(queryBuilderVisitor);

                var queries = queryBuilderVisitor.GetAllTerminalQueries();

                m_terminalQueriesByUnityComponents.Add(unityObj, queries);
                m_yamlStreamByUnityComponent.Add(unityObj, stream);
                m_textSourceByUnityComponent.Add(unityObj, bufferAsString);

                unityObj.SetTerminalQueriesByUnityComponents(queries);
                unityObj.SetYamlStream(stream);
                unityObj.SetSource(bufferAsString);
                unityObj.SetDocument(document);
            }
        }
    }
}