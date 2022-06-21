using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.RepresentationModel;

namespace UnityDiffFixer
{
    public class YamlVisitorSerializer : IYamlVisitor
    {
        private StringBuilder m_buffer = new StringBuilder();
        private string m_source;
        private int m_serializingIndex = 0;

        public YamlVisitorSerializer(string source)
        {
            m_source = source;
        }

        public static int TokenLength(YamlNode node)
        {
            return node.End.Index - node.Start.Index;
        }

        public void Visit(YamlStream stream)
        {
            if (stream.Documents.Count != 1)
            {
                throw new Exception();
            }
            stream.Documents[0].Accept(this);

            var diff = m_source.Length - m_serializingIndex;
            m_buffer.Append(m_source.Substring(m_serializingIndex, diff));
            m_serializingIndex += diff;
        }

        public void Visit(YamlDocument document)
        {
            document.RootNode.Accept(this);
        }

        public void Visit(YamlScalarNode scalar)
        {
            var length = TokenLength(scalar);
            if (scalar.Start.Index != m_serializingIndex || scalar.Value.Length != length)
            {
                var diff = scalar.Start.Index - m_serializingIndex;
                m_buffer.Append(m_source.Substring(m_serializingIndex, diff));
                m_serializingIndex += diff;
            }

            switch (scalar.Style)
            {
                case YamlDotNet.Core.ScalarStyle.SingleQuoted:
                    m_buffer.Append("'");
                    break;
                case YamlDotNet.Core.ScalarStyle.DoubleQuoted:
                    m_buffer.Append("\"");
                    break;
                case YamlDotNet.Core.ScalarStyle.Plain:
                    break;
                default:
                    throw new Exception("Not supported scalar style");
            }

            m_buffer.Append(scalar.Value);

            switch (scalar.Style)
            {
                case YamlDotNet.Core.ScalarStyle.SingleQuoted:
                    m_buffer.Append("'");
                    break;
                case YamlDotNet.Core.ScalarStyle.DoubleQuoted:
                    m_buffer.Append("\"");
                    break;
                case YamlDotNet.Core.ScalarStyle.Plain:
                    break;
                default:
                    throw new Exception("Not supported scalar style");
            }

            m_serializingIndex += length;
        }

        public void Visit(YamlSequenceNode sequence)
        {
            foreach (var node in sequence.Children)
            {
                node.Accept(this);
            }
        }

        public void Visit(YamlMappingNode mapping)
        {
            var children = mapping.Children;
            foreach (KeyValuePair<YamlNode, YamlNode> kvp in children)
            {
                YamlNode key = kvp.Key;
                YamlNode val = kvp.Value;

                key.Accept(this);
                val.Accept(this);
            }
        }

        public string GetContent()
        {
            return m_buffer.ToString();
        }
    }
}