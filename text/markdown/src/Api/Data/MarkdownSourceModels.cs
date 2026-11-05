using System;

namespace markdown;

/// <summary>
/// Identifies an absolute offset and its one-based line and column in Markdown source.
/// </summary>
public class MarkdownSourcePosition
{
    /// <summary>
    /// Stores the offset value used by this instance.
    /// </summary>
    public int Offset;
    /// <summary>
    /// Stores the line value used by this instance.
    /// </summary>
    public int Line;
    /// <summary>
    /// Stores the column value used by this instance.
    /// </summary>
    public int Column;

    /// <summary>
    /// Initializes a new <see cref="MarkdownSourcePosition"/> instance.
    /// </summary>
    public MarkdownSourcePosition()
    {
        Offset = 0;
        Line = 1;
        Column = 1;
    }

    /// <summary>
    /// Creates a source position with explicit coordinates.
    /// </summary>
    /// <param name="offset">The zero-based absolute source offset.</param>
    /// <param name="line">The line value.</param>
    /// <param name="column">The column value.</param>
    internal static MarkdownSourcePosition Create(int offset, int line, int column)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (line < 1)
        {
            throw new ArgumentOutOfRangeException("line");
        }

        if (column < 1)
        {
            throw new ArgumentOutOfRangeException("column");
        }

        MarkdownSourcePosition position = new MarkdownSourcePosition();
        position.Offset = offset;
        position.Line = line;
        position.Column = column;
        return position;
    }

    /// <summary>
    /// Creates the first source position at offset zero, line one, and column one.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownSourcePosition"/> value.</returns>
    internal static MarkdownSourcePosition Start()
    {
        return new MarkdownSourcePosition();
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownSourcePosition"/> value.</returns>
    internal MarkdownSourcePosition Copy()
    {
        return Create(Offset, Line, Column);
    }

    /// <summary>
    /// Reports whether this position is not after the other position.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsBeforeOrEqual(MarkdownSourcePosition? other)
    {
        if (other == null)
        {
            return false;
        }

        return Offset <= other.Offset;
    }

    /// <summary>
    /// Compares the offset, line, and column with another source position.
    /// </summary>
    /// <param name="other">The other value.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool EqualsPosition(MarkdownSourcePosition? other)
    {
        if (other == null)
        {
            return false;
        }

        return Offset == other.Offset && Line == other.Line && Column == other.Column;
    }

    /// <summary>
    /// Converts the value to display string.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string ToDisplayString()
    {
        return Line.ToString() + ":" + Column.ToString();
    }
}

/// <summary>
/// Identifies a half-open range between two source positions.
/// </summary>
public class MarkdownSourceSpan
{
    /// <summary>
    /// Stores the start value used by this instance.
    /// </summary>
    public MarkdownSourcePosition Start;
    /// <summary>
    /// Stores the end value used by this instance.
    /// </summary>
    public MarkdownSourcePosition End;

    /// <summary>
    /// Initializes a new <see cref="MarkdownSourceSpan"/> instance.
    /// </summary>
    public MarkdownSourceSpan()
    {
        Start = new MarkdownSourcePosition();
        End = new MarkdownSourcePosition();
    }

    /// <summary>
    /// Creates a source span from two positions.
    /// </summary>
    /// <param name="start">The start value.</param>
    /// <param name="end">The end value.</param>
    internal static MarkdownSourceSpan Create(MarkdownSourcePosition start, MarkdownSourcePosition end)
    {
        if (start == null)
        {
            throw new ArgumentNullException("start");
        }

        if (end == null)
        {
            throw new ArgumentNullException("end");
        }

        if (!start.IsBeforeOrEqual(end))
        {
            throw new ArgumentException("End position must not be before start position.");
        }

        MarkdownSourceSpan span = new MarkdownSourceSpan();
        span.Start = start.Copy();
        span.End = end.Copy();
        return span;
    }

    /// <summary>
    /// Creates a source span from explicit coordinates.
    /// </summary>
    /// <param name="startOffset">The start offset value.</param>
    /// <param name="startLine">The start line value.</param>
    /// <param name="startColumn">The start column value.</param>
    /// <param name="endOffset">The end offset value.</param>
    /// <param name="endLine">The end line value.</param>
    /// <param name="endColumn">The end column value.</param>
    internal static MarkdownSourceSpan Create(int startOffset, int startLine, int startColumn, int endOffset, int endLine, int endColumn)
    {
        MarkdownSourcePosition start = MarkdownSourcePosition.Create(startOffset, startLine, startColumn);
        MarkdownSourcePosition end = MarkdownSourcePosition.Create(endOffset, endLine, endColumn);

        if (!start.IsBeforeOrEqual(end))
        {
            throw new ArgumentException("End position must not be before start position.");
        }

        MarkdownSourceSpan span = new MarkdownSourceSpan();
        span.Start = start;
        span.End = end;
        return span;
    }

    /// <summary>
    /// Creates a zero-length span at the start of an empty source.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownSourceSpan"/> value.</returns>
    internal static MarkdownSourceSpan Empty()
    {
        return new MarkdownSourceSpan();
    }

    /// <summary>
    /// Returns length.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetLength()
    {
        return End.Offset - Start.Offset;
    }

    /// <summary>
    /// Reports whether the span contains no source characters.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEmpty()
    {
        return Start.Offset == End.Offset;
    }

    /// <summary>
    /// Checks whether the value contains offset.
    /// </summary>
    /// <param name="offset">The zero-based absolute source offset.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsOffset(int offset)
    {
        return offset >= Start.Offset && offset < End.Offset;
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownSourceSpan"/> value.</returns>
    internal MarkdownSourceSpan Copy()
    {
        return Create(Start, End);
    }
}

/// <summary>
/// Describes a parser, transform, or rendering diagnostic at a source span.
/// </summary>
public class MarkdownDiagnostic
{
    /// <summary>
    /// Stores the severity value used by this instance.
    /// </summary>
    public MarkdownDiagnosticSeverity Severity;
    /// <summary>
    /// Stores the code value used by this instance.
    /// </summary>
    public string Code;
    /// <summary>
    /// Stores the message value used by this instance.
    /// </summary>
    public string Message;
    /// <summary>
    /// Stores the span value used by this instance.
    /// </summary>
    public MarkdownSourceSpan Span;
    /// <summary>
    /// The canonical source name associated with the diagnostic, or an empty string when it is not source-specific.
    /// </summary>
    public string SourceName;

    /// <summary>
    /// Initializes a new <see cref="MarkdownDiagnostic"/> instance.
    /// </summary>
    public MarkdownDiagnostic()
    {
        Severity = MarkdownDiagnosticSeverity.Info;
        Code = "";
        Message = "";
        Span = new MarkdownSourceSpan();
        SourceName = "";
    }

    /// <summary>
    /// Creates a diagnostic with explicit values.
    /// </summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The developer-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    internal static MarkdownDiagnostic Create(MarkdownDiagnosticSeverity severity, string code, string message, MarkdownSourceSpan span)
    {
        ValidateSeverity(severity);
        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        MarkdownDiagnostic diagnostic = new MarkdownDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Span = span.Copy();
        diagnostic.SourceName = "";
        return diagnostic;
    }

    internal static void ValidateSeverity(MarkdownDiagnosticSeverity severity)
    {
        if (severity != MarkdownDiagnosticSeverity.Info
            && severity != MarkdownDiagnosticSeverity.Warning
            && severity != MarkdownDiagnosticSeverity.Error)
        {
            throw new ArgumentOutOfRangeException("severity");
        }
    }

    internal static MarkdownDiagnostic CreateWithSourceName(
        MarkdownDiagnosticSeverity severity,
        string code,
        string message,
        MarkdownSourceSpan span,
        string sourceName)
    {
        if (sourceName == null)
        {
            throw new ArgumentNullException("sourceName");
        }

        MarkdownDiagnostic diagnostic = Create(severity, code, message, span);
        diagnostic.SourceName = sourceName;
        return diagnostic;
    }

    /// <summary>
    /// Creates a diagnostic with info severity.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The developer-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting <see cref="MarkdownDiagnostic"/> value.</returns>
    internal static MarkdownDiagnostic Info(string code, string message, MarkdownSourceSpan span)
    {
        return Create(MarkdownDiagnosticSeverity.Info, code, message, span);
    }

    /// <summary>
    /// Creates a diagnostic with warning severity.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The developer-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting <see cref="MarkdownDiagnostic"/> value.</returns>
    internal static MarkdownDiagnostic Warning(string code, string message, MarkdownSourceSpan span)
    {
        return Create(MarkdownDiagnosticSeverity.Warning, code, message, span);
    }

    /// <summary>
    /// Creates a diagnostic with error severity.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The developer-readable diagnostic message.</param>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The resulting <see cref="MarkdownDiagnostic"/> value.</returns>
    internal static MarkdownDiagnostic Error(string code, string message, MarkdownSourceSpan span)
    {
        return Create(MarkdownDiagnosticSeverity.Error, code, message, span);
    }

    /// <summary>
    /// Creates a detached copy of the value.
    /// </summary>
    /// <returns>The resulting <see cref="MarkdownDiagnostic"/> value.</returns>
    internal MarkdownDiagnostic Copy()
    {
        return CreateWithSourceName(Severity, Code, Message, Span, SourceName);
    }
}
