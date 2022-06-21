using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace UnityDiffFixer
{
    public enum YamlQueryType
    {
        Dummy,
        ByName,
        ByIndex,
    }

    public abstract class YamlQuery
    {
        public YamlQueryType QueryType { get; private set; }

        public YamlQuery Parent { get; private set; }

        public YamlQuery(YamlQuery parent, YamlQueryType queryType)
        {
            Parent = parent;
            QueryType = queryType;
        }

        public YamlQueryByName AsNameQuery()
        {
            return (YamlQueryByName)this;
        }

        public YamlQueryByIndex AsIndexQuery()
        {
            return (YamlQueryByIndex)this;
        }
    }

    public class YamlQueryByName : YamlQuery
    {
        public string Name { get; private set; }

        public YamlQueryByName(YamlQuery parent, string name) : base(parent, YamlQueryType.ByName)
        {
            Name = name;
        }

        public override string ToString()
        {
            return "[" + Name + "]";
        }
    }

    public class YamlQueryByIndex : YamlQuery
    {
        public int Index { get; private set; }

        public YamlQueryByIndex(YamlQuery parent, int index) : base(parent, YamlQueryType.ByIndex)
        {
            Index = index;
        }

        public override string ToString()
        {
            return "[" + Index + "]";
        }
    }

    public class YamlDummyQuery : YamlQuery
    {
        public YamlDummyQuery() : base(null, YamlQueryType.Dummy)
        {
        }

        public override string ToString()
        {
            return "query";
        }
    }

    public class YamlQueryBuilder
    {
        private readonly List<YamlQuery> m_yamlQueries = new List<YamlQuery>();
        private YamlQuery m_currentQuery;

        public YamlQueryBuilder()
        {
            m_currentQuery = new YamlDummyQuery();
            m_yamlQueries.Add(m_currentQuery);
        }

        public YamlQueryBuilder WithName(string name)
        {
            m_currentQuery = new YamlQueryByName(m_currentQuery, name);
            m_yamlQueries.Add(m_currentQuery);
            return this;
        }

        public YamlQueryBuilder WithIndex(int index)
        {
            m_currentQuery = new YamlQueryByIndex(m_currentQuery, index);
            m_yamlQueries.Add(m_currentQuery);
            return this;
        }

        public IReadOnlyList<YamlQuery> Build()
        {
            var returned = new List<YamlQuery>(m_yamlQueries);
            return returned;
        }
    }

    public class YamlQueryUtils
    {
        public static YamlNode RunQueryChain(IEnumerable<YamlQuery> queryList, YamlStream yamlStream)
        {
            // List<YamlQuery> queryList = GetQueryChainFromRootNode(terminalQuery);
            var currentNode = yamlStream.Documents[0].RootNode;

            foreach (var query in queryList)
            {
                currentNode = RunQuery(query, currentNode);
                if (currentNode == null)
                {
                    return null;
                }
            }

            return currentNode;
        }

        public static List<YamlQuery> GetQueryChainFromRootNode(YamlQuery terminalQuery)
        {
            var currentQuery = terminalQuery;
            var queryList = new List<YamlQuery>();

            while (currentQuery != null)
            {
                queryList.Add(currentQuery);
                currentQuery = currentQuery.Parent;
            }

            queryList.Reverse();
            return queryList;
        }

        public static YamlNode RunQuery(YamlQuery query, YamlNode currentNode)
        {
            if (query is YamlQueryByName)
            {
                var realQuery = query as YamlQueryByName;
                var node = currentNode as YamlMappingNode;
                var children = node.Children;

                foreach (var child in children)
                {
                    var key = child.Key as YamlScalarNode;
                    if (key.Value == realQuery.Name)
                    {
                        return child.Value;
                    }
                }
                return null;
            }
            else if (query is YamlQueryByIndex)
            {
                var realQuery = query as YamlQueryByIndex;
                var node = currentNode as YamlSequenceNode;
                return node.Children[realQuery.Index];
            }
            else if (query is YamlDummyQuery)
            {
                return currentNode;
            }
            else
            {
                throw new Exception("not implemented query type");
            }
        }

        public static string ReserializeNewDocWithFix(UnityYAMLDocument doc, Action<string> printAction = null)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(doc.GetOriginalHeader());
            foreach (var kvp in doc.GetYamlStreamByComponent())
            {
                builder.Append(kvp.Key.GetOriginalLine().LineContent);
                builder.Append(LineEndings.NewLine);
                var source = doc.GetSourceForComponent(kvp.Key);
                var serializerVisitor = new YamlVisitorSerializer(source);
                kvp.Value.Accept(serializerVisitor);
                var str = serializerVisitor.GetContent();

                // var sourceLines = StringUtils.GetAllLinesFromText(source);
                // var fixedLines = StringUtils.GetAllLinesFromText(str);
                // var sourceLinesCount = sourceLines.Count;
                // var fixedLinesCount = fixedLines.Count;
                var sourceLinesCount = CountLinesFast(source);
                var fixedLinesCount = CountLinesFast(str);

                if (sourceLinesCount != fixedLinesCount)
                {
                    if (printAction != null)
                    {
                        printAction($"Fix for component : {kvp.Key.GetOriginalLine().LineContent} has modified line count. Reverting auto-fixes.");
                    }
                    builder.Append(source);
                }
                else
                {
                    builder.Append(str);
                }
            }

            return builder.ToString();
        }

        private static int CountLinesFast(string inStr)
        {
            var count = 0;

            foreach (char c in inStr)
            {
                if (c == '\n')
                {
                    count++;
                }
            }
            return count;
        }
    }
}