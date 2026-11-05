using System;

namespace markdown;

internal class MarkdownTriviaCapture
{
    internal static void Apply(MarkdownDocument document, string source)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        CaptureNode(document.Root, source);
    }

    internal static void CaptureNode(MarkdownNode node, string source)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (!node.Span.IsEmpty())
        {
            string text = GetSourceText(source, node.Span);
            MarkdownTrivia trivia = new MarkdownTrivia(node.Span, text);
            trivia.LineEnding = FindLineEnding(text);
            trivia.OpeningDelimiter = FindOpeningDelimiter(node.Kind, text);
            trivia.ClosingDelimiter = FindClosingDelimiter(node.Kind, text);
            node.SetTrivia(trivia);
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            CaptureNode(node.GetChild(index), source);
            index = index + 1;
        }
    }

    internal static string GetSourceText(string source, MarkdownSourceSpan span)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (span.Start.Offset < 0 || span.Start.Offset > source.Length)
        {
            throw new ArgumentOutOfRangeException("span");
        }

        if (span.End.Offset < span.Start.Offset || span.End.Offset > source.Length)
        {
            throw new ArgumentOutOfRangeException("span");
        }

        return source.Substring(span.Start.Offset, span.GetLength());
    }

    internal static string FindLineEnding(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == '\r')
            {
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    return "\r\n";
                }

                return "\r";
            }

            if (text[index] == '\n')
            {
                return "\n";
            }

            index = index + 1;
        }

        return "";
    }

    internal static string FindOpeningDelimiter(MarkdownNodeKind kind, string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        int start = FindFirstNonSpace(text, 0, FindFirstLineEnd(text));
        if (start < 0)
        {
            return "";
        }

        switch (kind)
        {
            case MarkdownNodeKind.Heading:
                return ReadRepeatedDelimiter(text, start, '#');
            case MarkdownNodeKind.FencedCodeBlock:
            case MarkdownNodeKind.CustomFencedBlock:
            case MarkdownNodeKind.DiagramBlock:
                return ReadFenceDelimiter(text, start);
            case MarkdownNodeKind.MathBlock:
                return ReadRepeatedDelimiter(text, start, '$');
            case MarkdownNodeKind.CustomContainerBlock:
            case MarkdownNodeKind.DetailsBlock:
                return ReadRepeatedDelimiter(text, start, ':');
            case MarkdownNodeKind.AdmonitionBlock:
                return ReadRepeatedDelimiter(text, start, '!');
            case MarkdownNodeKind.BlockQuote:
                return ReadRepeatedDelimiter(text, start, '>');
            case MarkdownNodeKind.ListItem:
            case MarkdownNodeKind.TaskListItem:
                return ReadListDelimiter(text, start);
            case MarkdownNodeKind.ThematicBreak:
                return ReadTrimmedFirstLine(text);
            case MarkdownNodeKind.LinkReferenceDefinition:
            case MarkdownNodeKind.FootnoteDefinition:
                return "[";
            case MarkdownNodeKind.Image:
                return "![";
            case MarkdownNodeKind.Link:
                return "[";
            case MarkdownNodeKind.Emphasis:
                return ReadInlineDelimiter(text, 1);
            case MarkdownNodeKind.StrongEmphasis:
            case MarkdownNodeKind.Strikethrough:
            case MarkdownNodeKind.InsertedText:
            case MarkdownNodeKind.MarkedText:
                return ReadInlineDelimiter(text, 2);
            case MarkdownNodeKind.CodeSpan:
                return ReadRepeatedDelimiter(text, start, '`');
            case MarkdownNodeKind.MathSpan:
                return ReadRepeatedDelimiter(text, start, '$');
            default:
                return "";
        }
    }

    internal static string FindClosingDelimiter(MarkdownNodeKind kind, string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        switch (kind)
        {
            case MarkdownNodeKind.Heading:
                return FindHeadingClosingDelimiter(text);
            case MarkdownNodeKind.FencedCodeBlock:
            case MarkdownNodeKind.CustomFencedBlock:
            case MarkdownNodeKind.DiagramBlock:
            case MarkdownNodeKind.MathBlock:
            case MarkdownNodeKind.CustomContainerBlock:
            case MarkdownNodeKind.DetailsBlock:
                return ReadTrimmedLastLine(text);
            case MarkdownNodeKind.Image:
            case MarkdownNodeKind.Link:
                return ReadLinkClosingDelimiter(text);
            case MarkdownNodeKind.Emphasis:
                return ReadTrailingInlineDelimiter(text, 1);
            case MarkdownNodeKind.StrongEmphasis:
            case MarkdownNodeKind.Strikethrough:
            case MarkdownNodeKind.InsertedText:
            case MarkdownNodeKind.MarkedText:
                return ReadTrailingInlineDelimiter(text, 2);
            case MarkdownNodeKind.CodeSpan:
                return ReadTrailingRepeatedDelimiter(text, '`');
            case MarkdownNodeKind.MathSpan:
                return ReadTrailingRepeatedDelimiter(text, '$');
            default:
                return "";
        }
    }

    internal static int FindFirstLineEnd(string text)
    {
        int index = 0;
        while (index < text.Length && text[index] != '\r' && text[index] != '\n')
        {
            index = index + 1;
        }

        return index;
    }

    internal static int FindFirstNonSpace(string text, int start, int end)
    {
        int index = start;
        while (index < end && (text[index] == ' ' || text[index] == '\t'))
        {
            index = index + 1;
        }

        if (index >= end)
        {
            return -1;
        }

        return index;
    }

    internal static string ReadRepeatedDelimiter(string text, int start, char delimiter)
    {
        int end = start;
        while (end < text.Length && text[end] == delimiter)
        {
            end = end + 1;
        }

        return text.Substring(start, end - start);
    }

    internal static string ReadFenceDelimiter(string text, int start)
    {
        if (text[start] != '`' && text[start] != '~')
        {
            return "";
        }

        return ReadRepeatedDelimiter(text, start, text[start]);
    }

    internal static string ReadListDelimiter(string text, int start)
    {
        if (text[start] == '-' || text[start] == '+' || text[start] == '*')
        {
            return text[start].ToString();
        }

        int index = start;
        while (index < text.Length && char.IsDigit(text[index]))
        {
            index = index + 1;
        }

        if (index > start && index < text.Length && (text[index] == '.' || text[index] == ')'))
        {
            return text.Substring(start, index + 1 - start);
        }

        return "";
    }

    internal static string ReadTrimmedFirstLine(string text)
    {
        int end = FindFirstLineEnd(text);
        int start = FindFirstNonSpace(text, 0, end);
        if (start < 0)
        {
            return "";
        }

        while (end > start && (text[end - 1] == ' ' || text[end - 1] == '\t'))
        {
            end = end - 1;
        }

        return text.Substring(start, end - start);
    }

    internal static string ReadTrimmedLastLine(string text)
    {
        int end = text.Length;
        while (end > 0 && (text[end - 1] == '\r' || text[end - 1] == '\n'))
        {
            end = end - 1;
        }

        int start = end;
        while (start > 0 && text[start - 1] != '\r' && text[start - 1] != '\n')
        {
            start = start - 1;
        }

        while (start < end && (text[start] == ' ' || text[start] == '\t'))
        {
            start = start + 1;
        }

        while (end > start && (text[end - 1] == ' ' || text[end - 1] == '\t'))
        {
            end = end - 1;
        }

        return text.Substring(start, end - start);
    }

    internal static string FindHeadingClosingDelimiter(string text)
    {
        string lastLine = ReadTrimmedLastLine(text);
        if (lastLine.Length > 0 && (lastLine[0] == '=' || lastLine[0] == '-'))
        {
            bool setext = true;
            int index = 1;
            while (index < lastLine.Length)
            {
                if (lastLine[index] != lastLine[0])
                {
                    setext = false;
                }

                index = index + 1;
            }

            if (setext)
            {
                return lastLine;
            }
        }

        int end = FindFirstLineEnd(text);
        while (end > 0 && (text[end - 1] == ' ' || text[end - 1] == '\t'))
        {
            end = end - 1;
        }

        int start = end;
        while (start > 0 && text[start - 1] == '#')
        {
            start = start - 1;
        }

        if (start == end || start == 0 || (text[start - 1] != ' ' && text[start - 1] != '\t'))
        {
            return "";
        }

        return text.Substring(start, end - start);
    }

    internal static string ReadInlineDelimiter(string text, int length)
    {
        if (text.Length < length)
        {
            return "";
        }

        return text.Substring(0, length);
    }

    internal static string ReadTrailingInlineDelimiter(string text, int length)
    {
        if (text.Length < length)
        {
            return "";
        }

        return text.Substring(text.Length - length, length);
    }

    internal static string ReadTrailingRepeatedDelimiter(string text, char delimiter)
    {
        int start = text.Length;
        while (start > 0 && text[start - 1] == delimiter)
        {
            start = start - 1;
        }

        return text.Substring(start, text.Length - start);
    }

    internal static string ReadLinkClosingDelimiter(string text)
    {
        if (text.Length == 0)
        {
            return "";
        }

        if (text[text.Length - 1] == ')')
        {
            return ")";
        }

        if (text[text.Length - 1] == ']')
        {
            return "]";
        }

        return "";
    }
}
