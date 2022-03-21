using System.Collections.Generic;

namespace UnityDiffFixer
{
    public class Parser
    {
        private List<string> m_lines;
        private int m_lineIndex = 0;

        public Parser(List<string> lines)
        {
            m_lines = lines;
        }

        internal IndexedLine GetCurrentLineAndAdvance()
        {
            var old = m_lineIndex;
            m_lineIndex++;
            return new IndexedLine(old + 1, m_lines[old]);
        }

        internal int GetCurrentLineIndex()
        {
            return m_lineIndex;
        }

        internal bool IsAtEnd()
        {
            return m_lineIndex == m_lines.Count;
        }

        internal string PeekNextLine()
        {
            return m_lines[m_lineIndex + 1];
        }

        internal string PeekCurrentLine()
        {
            return m_lines[m_lineIndex];
        }
    }
}
