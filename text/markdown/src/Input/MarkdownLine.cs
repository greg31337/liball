using System;

namespace markdown;

internal class MarkdownLine
{
    internal int LineNumber;
    internal string Text;
    internal string LineEnding;
    internal MarkdownSourceSpan ContentSpan;
    internal MarkdownSourceSpan LineSpan;

    internal MarkdownLine()
    {
        LineNumber = 1;
        Text = "";
        LineEnding = "";
        ContentSpan = new MarkdownSourceSpan();
        LineSpan = new MarkdownSourceSpan();
    }

    internal static MarkdownLine Create(int lineNumber, string text, string lineEnding, MarkdownSourceSpan contentSpan, MarkdownSourceSpan lineSpan)
    {
        if (lineNumber < 1)
        {
            throw new ArgumentOutOfRangeException("lineNumber");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (lineEnding == null)
        {
            throw new ArgumentNullException("lineEnding");
        }

        if (contentSpan == null)
        {
            throw new ArgumentNullException("contentSpan");
        }

        if (lineSpan == null)
        {
            throw new ArgumentNullException("lineSpan");
        }

        MarkdownLine line = new MarkdownLine();
        line.LineNumber = lineNumber;
        line.Text = text;
        line.LineEnding = lineEnding;
        line.ContentSpan = contentSpan.Copy();
        line.LineSpan = lineSpan.Copy();
        return line;
    }

    internal bool HasLineEnding()
    {
        return LineEnding.Length > 0;
    }

    internal int GetContentLength()
    {
        return Text.Length;
    }

    internal int GetLineLength()
    {
        return Text.Length + LineEnding.Length;
    }
}
