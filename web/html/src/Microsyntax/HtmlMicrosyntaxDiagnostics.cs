using System;

namespace html;

internal static class HtmlMicrosyntaxDiagnostics
{
    internal static HtmlDiagnostic Create(string source, int start, int end,
        HtmlDiagnosticCode code, string message)
    {
        return HtmlDiagnostic.Create(code, HtmlDiagnosticSeverity.Error,
            message, CreateSpan(source, start, end));
    }

    internal static HtmlSourceSpan CreateSpan(string source, int start,
        int end)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (start < 0 || start > source.Length || end < start
            || end > source.Length)
        {
            throw new ArgumentOutOfRangeException("start");
        }

        HtmlSourcePosition startPosition = CreatePosition(source, start);
        HtmlSourcePosition endPosition = CreatePosition(source, end);
        return HtmlSourceSpan.Create(startPosition, endPosition);
    }

    internal static HtmlSourcePosition CreatePosition(string source,
        int offset)
    {
        int line = 1;
        int column = 1;
        int index = 0;
        while (index < offset)
        {
            char current = source[index];
            if (current == '\r')
            {
                line = line + 1;
                column = 1;
                if (index + 1 < offset && source[index + 1] == '\n')
                {
                    index = index + 1;
                }
            }
            else if (current == '\n')
            {
                line = line + 1;
                column = 1;
            }
            else
            {
                column = column + 1;
            }

            index = index + 1;
        }

        return HtmlSourcePosition.Create(offset, line, column);
    }
}
