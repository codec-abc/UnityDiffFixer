using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace UnityDiffFixer
{
    public class UnityYAMLDocument
    {
        UnityYAMLHeader m_header;

        private Dictionary<UnityYAMLRootObject, List<YamlQuery>> m_terminalQueriesByUnityComponents = 
            new Dictionary<UnityYAMLRootObject, List<YamlQuery>>();

        private Dictionary<UnityYAMLRootObject, YamlStream> m_yamlStreamByUnityComponent =
            new Dictionary<UnityYAMLRootObject, YamlStream>();

        private Dictionary<UnityYAMLRootObject, string> m_textSourceByUnityComponent =
            new Dictionary<UnityYAMLRootObject, string>();

        private Parser m_parser;

        private static readonly Regex objectHeaderRegex = 
            new Regex
            (
                @"^--- !u!([0-9]*) &(.*)$", 
                RegexOptions.Compiled | RegexOptions.IgnoreCase
            );

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

        public UnityYAMLDocument(Parser parser)
        {
            this.m_parser = parser;
        }

        public static UnityYAMLDocument ParseUnityYAMLdocument(List<string> lines)
        {
            Parser parser = new Parser(lines);
            var document = new UnityYAMLDocument(parser);
            document.ParseHeader(document);
            document.ParseObjects(document);
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

        private void ParseObjects(UnityYAMLDocument document)
        {
            while (!m_parser.IsAtEnd()) 
            {
                var objectHeaderLine = m_parser.GetCurrentLineAndAdvance();
                var match = objectHeaderRegex.Match(objectHeaderLine.LineContent);

                var objectClassValue = match.Groups[1].ToString();
                long objectClass = long.Parse(objectClassValue);
                var objectIdValue = match.Groups[2].ToString();
                //long objectId = long.Parse(objectIdValue);

                var unityClass = UnityClassObjectUtils.LongToUnityClassObject(objectClass);

                // TODO: use this
                var unityObj = new UnityYAMLRootObject(unityClass, objectIdValue, objectHeaderLine);

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
                    var msg = "Cannot parse " + unityObj.GetOriginalLine().LineContent + "\r\n";
                    msg += "Internal error: " + e.ToString() + "\r\n";
                    msg += "\r\nWith Content:\r\n";
                    msg += bufferAsString;
                    throw new System.Exception(msg, e);
                }

                var queryBuilderVisitor = new YamlTerminalQueryBuilderVisitor();
                stream.Accept(queryBuilderVisitor);

                var queries = queryBuilderVisitor.GetAllTerminalQueries();

                m_terminalQueriesByUnityComponents.Add(unityObj, queries);
                m_yamlStreamByUnityComponent.Add(unityObj, stream);
                m_textSourceByUnityComponent.Add(unityObj, bufferAsString);
            }
        }
    }

    
}
