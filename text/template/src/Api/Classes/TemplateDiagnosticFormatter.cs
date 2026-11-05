using System;
using System.Text;

namespace template;

/// <summary>
/// Formats diagnostics with source names, line and column numbers, source text, and a marker line.
/// </summary>
public class TemplateDiagnosticFormatter : ITemplateDiagnosticFormatter
{
    /// <summary>
    /// Initializes a formatter for source-aware diagnostic text.
    /// </summary>
    public TemplateDiagnosticFormatter()
    {
    }

    /// <summary>
    /// Formats this value into a stable, human-readable form.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to add or format.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string Format(TemplateDiagnostic diagnostic)
    {
        if (diagnostic == null)
        {
            throw new ArgumentNullException("diagnostic");
        }

        if (diagnostic.Span == null)
        {
            return FormatWithoutSource(diagnostic);
        }

        TemplateSourceSpan span = diagnostic.Span;
        TemplateSourceLineMap lineMap = span.Source.CreateLineMap();
        TemplateSourcePosition position = lineMap.GetPosition(span.Start);
        TemplateSourceLine line = lineMap.GetLine(position.LineNumber);
        int markerLength = GetMarkerLength(span, line, position.ColumnNumber);

        StringBuilder builder = new StringBuilder();
        builder.Append(span.Source.GetDisplayName());
        builder.Append(":");
        builder.Append(position.LineNumber.ToString());
        builder.Append(":");
        builder.Append(position.ColumnNumber.ToString());
        builder.Append(": ");
        builder.Append(diagnostic.GetSeverityName());
        builder.Append(" ");
        builder.Append(diagnostic.Code);
        builder.Append(": ");
        builder.Append(diagnostic.Message);
        builder.Append("\n");
        builder.Append(line.GetText());
        builder.Append("\n");
        builder.Append(GetMarkerIndent(line, position.ColumnNumber));
        builder.Append(RepeatChar('^', markerLength));

        if (!string.IsNullOrEmpty(diagnostic.Hint))
        {
            builder.Append("\n");
            builder.Append("hint: ");
            builder.Append(diagnostic.Hint);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns the line number.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public int GetLineNumber(string text, int offset)
    {
        TemplateSource source = TemplateSource.FromString(text);
        TemplateSourceLineMap lineMap = source.CreateLineMap();
        TemplateSourcePosition position = lineMap.GetPosition(offset);
        return position.LineNumber;
    }

    /// <summary>
    /// Returns the column number.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public int GetColumnNumber(string text, int offset)
    {
        TemplateSource source = TemplateSource.FromString(text);
        TemplateSourceLineMap lineMap = source.CreateLineMap();
        TemplateSourcePosition position = lineMap.GetPosition(offset);
        return position.ColumnNumber;
    }

    /// <summary>
    /// Returns the line text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public string GetLineText(string text, int offset)
    {
        TemplateSource source = TemplateSource.FromString(text);
        TemplateSourceLineMap lineMap = source.CreateLineMap();
        TemplateSourcePosition position = lineMap.GetPosition(offset);
        TemplateSourceLine line = lineMap.GetLine(position.LineNumber);
        return line.GetText();
    }

    internal string FormatWithoutSource(TemplateDiagnostic diagnostic)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(diagnostic.GetSeverityName());
        builder.Append(" ");
        builder.Append(diagnostic.Code);
        builder.Append(": ");
        builder.Append(diagnostic.Message);

        if (!string.IsNullOrEmpty(diagnostic.Hint))
        {
            builder.Append("\n");
            builder.Append("hint: ");
            builder.Append(diagnostic.Hint);
        }

        return builder.ToString();
    }

    internal int GetMarkerLength(TemplateSourceSpan span, TemplateSourceLine line, int columnNumber)
    {
        int lineRemaining = line.Length - columnNumber + 1;
        int markerLength = span.Length;
        if (markerLength < 1)
        {
            markerLength = 1;
        }

        if (markerLength > lineRemaining)
        {
            markerLength = lineRemaining;
        }

        if (markerLength < 1)
        {
            markerLength = 1;
        }

        return markerLength;
    }

    internal string GetMarkerIndent(TemplateSourceLine line, int columnNumber)
    {
        string lineText = line.GetText();
        StringBuilder builder = new StringBuilder();
        int indentLength = columnNumber - 1;
        int index = 0;
        while (index < indentLength)
        {
            if (index < lineText.Length && lineText[index] == '\t')
            {
                builder.Append('\t');
            }
            else
            {
                builder.Append(' ');
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal string RepeatChar(char value, int count)
    {
        if (count <= 0)
        {
            return "";
        }

        return new string(value, count);
    }
}
