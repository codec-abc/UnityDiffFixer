using System;
using System.Collections.Generic;
using YamlDotNet.RepresentationModel;

namespace UnityDiffFixer
{
    internal class YamlTerminalQueryBuilderVisitor : IYamlVisitor
    {
        private List<YamlQuery> m_terminalQueries = new List<YamlQuery>();
        private Stack<YamlQuery> m_currentQuery = new Stack<YamlQuery>();

        public List<YamlQuery> GetAllTerminalQueries()
        {
            return m_terminalQueries;
        }

        public void Visit(YamlStream stream)
        {
            m_currentQuery.Push(new YamlDummyQuery());

            if (stream.Documents.Count != 1)
            {
                throw new Exception();
            }
            if (stream.Documents[0].RootNode.NodeType == YamlNodeType.Scalar)
            {
                throw new Exception("unsupported root node");
            }
            stream.Documents[0].Accept(this);
        }

        public void Visit(YamlDocument document)
        {
            document.RootNode.Accept(this);
        }

        public void Visit(YamlScalarNode scalar)
        {
            // var val = scalar.Value;
        }

        public void Visit(YamlSequenceNode sequence)
        {
            int i = 0;
            foreach (var node in sequence.Children)
            {
                var currentQuery = m_currentQuery.Peek();
                var nextQuery = new YamlQueryByIndex(currentQuery, i);
                m_currentQuery.Push(nextQuery); // new YamlQueryByName(currentQuery, scalarKey.Value)

                if (node.NodeType == YamlNodeType.Scalar)
                {
                    var scalarKey = (YamlScalarNode)node;
                    m_terminalQueries.Add(m_currentQuery.Peek());
                }
                else
                {
                    node.Accept(this);
                }

                m_currentQuery.Pop();

                i++;
            }
        }

        public void Visit(YamlMappingNode mapping)
        {
            var children = mapping.Children;
            foreach (KeyValuePair<YamlNode, YamlNode> kvp in children)
            {
                YamlNode key = kvp.Key;
                YamlNode val = kvp.Value;

                if (key.NodeType != YamlNodeType.Scalar)
                {
                    throw new Exception("Yaml mapping key node is not a scalar type");
                }
                else
                {
                    var currentQuery = m_currentQuery.Peek();
                    var scalarKey = (YamlScalarNode)key;
                    m_currentQuery.Push(new YamlQueryByName(currentQuery, scalarKey.Value));

                    if (val.NodeType == YamlNodeType.Scalar)
                    {
                        var chain = m_currentQuery.Peek();
                        m_terminalQueries.Add(chain);
                    }
                    else
                    {
                        val.Accept(this);
                    }
                    m_currentQuery.Pop();
                }
            }
        }
    }
}