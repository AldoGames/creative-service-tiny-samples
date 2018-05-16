#if NET_4_6
using System;
using System.Text;

namespace Unity.Tiny
{
    public enum BraceLayout
    {
        EndOfLine,
        EndOfLineSpace,
        NextLine,
        NextLineIndent
    }

    public class CodeStyle
    {
        public string Indent { get; set; }
        public BraceLayout BraceLayout { get; set; }
        public string BeginBrace { get; set; }
        public string EndBrace { get; set; }
        public string NewLine { get; set; }

        public static CodeStyle JavaScript => new CodeStyle()
        {
            Indent = "    ",
            BraceLayout = BraceLayout.EndOfLineSpace,
            BeginBrace = "{",
            EndBrace = "}",
            NewLine = Environment.NewLine
        };
        
        public static CodeStyle CSharp => new CodeStyle()
        {
            Indent = "    ",
            BraceLayout = BraceLayout.NextLine,
            BeginBrace = "{",
            EndBrace = "}",
            NewLine = Environment.NewLine
        };
    }

    public class Scope : IDisposable
    {
        private Action m_Disposed;

        public Scope(Action disposed)
        {
            m_Disposed = disposed;
        }

        public void Dispose()
        {
            if (null == m_Disposed)
            {
                return;
            }

            m_Disposed.Invoke();
            m_Disposed = null;
        }
    }

    public class UTinyCodeWriter
    {
        private readonly StringBuilder m_StringBuilder;

        private int m_Indent;

        public CodeStyle CodeStyle { get; set; }

        public int Length
        {
            get { return m_StringBuilder.Length; }
            set { m_StringBuilder.Length = value; }
        }

        public UTinyCodeWriter()
            : this(CodeStyle.JavaScript)
        { }

        public UTinyCodeWriter(CodeStyle codeStyle)
        {
            m_StringBuilder = new StringBuilder();
            CodeStyle = codeStyle;
        }

        public UTinyCodeWriter Prepend(string value)
        {
            m_StringBuilder.Insert(0, value);
            return this;
        }
        
        public UTinyCodeWriter WriteRaw(string value)
        {
            m_StringBuilder.Append(value);
            return this;
        }

        public UTinyCodeWriter Line()
        {
            m_StringBuilder.Append(CodeStyle.NewLine);
            return this;
        }

        public UTinyCodeWriter LineFormat(string format, params object[] args)
        {
            return Line(string.Format(format, args));
        }

        public UTinyCodeWriter Line(string content)
        {
            WriteIndent();
            m_StringBuilder.Append(content);
            m_StringBuilder.Append(CodeStyle.NewLine);
            return this;
        }

        public Scope Scope(string content, bool endLine = true)
        {
            return Scope(content, CodeStyle.BraceLayout, endLine);
        }

        public Scope Scope(string content, BraceLayout layout, bool endLine = true)
        {
            WriteIndent();

            m_StringBuilder.Append(content);

            WriteBeginScope(layout);

            return new Scope(() =>
            {
                WriteEndScope(layout, endLine);
            });
        }

        private void WriteBeginScope(BraceLayout layout)
        {
            switch (layout)
            {
                case BraceLayout.EndOfLine:
                    m_StringBuilder.Append(CodeStyle.BeginBrace);
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    break;
                case BraceLayout.EndOfLineSpace:
                    m_StringBuilder.AppendFormat(" {0}", CodeStyle.BeginBrace);
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    break;
                case BraceLayout.NextLine:
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    WriteIndent();
                    m_StringBuilder.Append(CodeStyle.BeginBrace);
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    break;
                case BraceLayout.NextLineIndent:
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    IncrementIndent();
                    WriteIndent();
                    m_StringBuilder.Append(CodeStyle.BeginBrace);
                    m_StringBuilder.Append(CodeStyle.NewLine);
                    break;
            }
            IncrementIndent();
        }

        private void WriteEndScope(BraceLayout layout, bool endLine)
        {
            switch (layout)
            {
                case BraceLayout.EndOfLine:
                case BraceLayout.EndOfLineSpace:
                case BraceLayout.NextLine:
                    DecrementIndent();
                    WriteIndent();
                    WriteRaw(CodeStyle.EndBrace);
                    break;
                case BraceLayout.NextLineIndent:
                    DecrementIndent();
                    WriteRaw(CodeStyle.EndBrace);
                    WriteIndent();
                    DecrementIndent();
                    break;
            }

            if (endLine)
            {
                WriteRaw(CodeStyle.NewLine);
            }
        }

        public void IncrementIndent()
        {
            m_Indent++;
        }

        public void DecrementIndent()
        {
            if (m_Indent > 0)
            {
                m_Indent--;
            }
        }

        public void Clear()
        {
            m_StringBuilder.Length = 0;
        }

        public void WriteIndent()
        {
            for (var i = 0; i < m_Indent; i++)
            {
                m_StringBuilder.Append(CodeStyle.Indent);
            }
        }

        public override string ToString()
        {
            return m_StringBuilder.ToString();
        }

        public string Substring(int begin, int end = 0)
        {
            return m_StringBuilder.ToString(begin, end == 0 ? m_StringBuilder.Length - begin : end);
        }
    }
}
#endif // NET_4_6
