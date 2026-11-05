using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownLineReader
{
    internal static MarkdownLine[] ReadLines(string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        source = MarkdownInputNormalizer.Normalize(source);

        List<MarkdownLine> lines = new List<MarkdownLine>();
        int index = 0;
        int lineNumber = 1;

        while (index < source.Length)
        {
            int lineStart = index;
            int contentEnd = index;

            while (contentEnd < source.Length && !MarkdownChar.IsLineEnding(source[contentEnd]))
            {
                contentEnd = contentEnd + 1;
            }

            string text = source.Substring(lineStart, contentEnd - lineStart);
            string lineEnding = "";
            int lineEnd = contentEnd;

            int lineEndingLength = MarkdownChar.GetLineEndingLength(source, contentEnd);
            if (lineEndingLength > 0)
            {
                lineEnding = source.Substring(contentEnd, lineEndingLength);
                lineEnd = contentEnd + lineEndingLength;
            }

            MarkdownSourceSpan contentSpan = MarkdownSourceSpan.Create(
                lineStart,
                lineNumber,
                1,
                contentEnd,
                lineNumber,
                contentEnd - lineStart + 1);

            MarkdownSourcePosition lineEndPosition;
            if (lineEnding.Length > 0)
            {
                lineEndPosition = MarkdownSourcePosition.Create(lineEnd, lineNumber + 1, 1);
            }
            else
            {
                lineEndPosition = MarkdownSourcePosition.Create(lineEnd, lineNumber, lineEnd - lineStart + 1);
            }

            MarkdownSourceSpan lineSpan = MarkdownSourceSpan.Create(
                MarkdownSourcePosition.Create(lineStart, lineNumber, 1),
                lineEndPosition);

            lines.Add(MarkdownLine.Create(lineNumber, text, lineEnding, contentSpan, lineSpan));

            index = lineEnd;
            lineNumber = lineNumber + 1;
        }

        return lines.ToArray();
    }
}
