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
        ByIndex
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

    public class YamlQueryUtils
    {
        public static YamlNode RunQueryChain(List<YamlQuery> queryList, YamlStream yamlStream)
        {
            //List<YamlQuery> queryList = GetQueryChainFromRootNode(terminalQuery);
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

        public static string ReserializeNewDocWithFix(UnityYAMLDocument doc)
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
                builder.Append(str);
            }

            return builder.ToString();
        }
    }
}
