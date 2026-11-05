using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownDiagnosticQueries
{
    internal static MarkdownDiagnostic[] FindContaining(
        MarkdownDiagnosticCollection diagnostics,
        int offset)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        List<MarkdownDiagnostic> matches = new List<MarkdownDiagnostic>();
        int index = 0;
        while (index < diagnostics.Items.Count)
        {
            MarkdownDiagnostic diagnostic = diagnostics.Items[index];
            if (diagnostic.Span.ContainsOffset(offset))
            {
                matches.Add(diagnostic.Copy());
            }

            index = index + 1;
        }

        return matches.ToArray();
    }

    internal static MarkdownDiagnostic[] FindIntersecting(
        MarkdownDiagnosticCollection diagnostics,
        MarkdownSourceSpan span)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        ValidateSpan(span);
        List<MarkdownDiagnostic> matches = new List<MarkdownDiagnostic>();
        int index = 0;
        while (index < diagnostics.Items.Count)
        {
            MarkdownDiagnostic diagnostic = diagnostics.Items[index];
            if (SpansIntersect(diagnostic.Span, span))
            {
                matches.Add(diagnostic.Copy());
            }

            index = index + 1;
        }

        return matches.ToArray();
    }

    internal static bool SpansIntersect(MarkdownSourceSpan first, MarkdownSourceSpan second)
    {
        ValidateSpan(first);
        ValidateSpan(second);
        return first.Start.Offset < second.End.Offset
            && second.Start.Offset < first.End.Offset;
    }

    internal static void ValidateSpan(MarkdownSourceSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (span.Start == null || span.End == null)
        {
            throw new ArgumentException("Source span positions must not be null.");
        }

        if (span.Start.Offset < 0
            || span.End.Offset < span.Start.Offset
            || span.Start.Line < 1
            || span.End.Line < 1
            || span.Start.Column < 1
            || span.End.Column < 1)
        {
            throw new ArgumentException("Source span coordinates are invalid.");
        }
    }
}
