using System;

namespace sql;

/// <summary>Describes a source span using optional file identity and one-based start and exclusive-end coordinates.</summary>
public class SqlFileLocation
{
    /// <summary>The optional file name associated with the source snapshot.</summary>
    public string? FileName;
    /// <summary>The half-open source span associated with this location.</summary>
    public SqlTextSpan Span;
    /// <summary>The one-based line number at the start of the span.</summary>
    public int StartLine;
    /// <summary>The one-based column number at the start of the span.</summary>
    public int StartColumn;
    /// <summary>The one-based line number at the exclusive end of the span.</summary>
    public int EndLine;
    /// <summary>The one-based column number at the exclusive end of the span.</summary>
    public int EndColumn;

    /// <summary>Initializes a source file location.</summary>
    /// <param name="fileName">The optional file name.</param>
    /// <param name="span">The half-open source span.</param>
    /// <param name="startLine">The one-based start line.</param>
    /// <param name="startColumn">The one-based start column.</param>
    /// <param name="endLine">The one-based exclusive-end line.</param>
    /// <param name="endColumn">The one-based exclusive-end column.</param>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A line or column number is less than one.</exception>
    public SqlFileLocation(string? fileName, SqlTextSpan span, int startLine, int startColumn, int endLine, int endColumn)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        if (startLine < 1)
        {
            throw new ArgumentOutOfRangeException("startLine");
        }

        if (startColumn < 1)
        {
            throw new ArgumentOutOfRangeException("startColumn");
        }

        if (endLine < 1)
        {
            throw new ArgumentOutOfRangeException("endLine");
        }

        if (endColumn < 1)
        {
            throw new ArgumentOutOfRangeException("endColumn");
        }

        FileName = fileName;
        Span = span;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }
}
