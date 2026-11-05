using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Describes a zero-based offset and its one-based line and column within a template source.
/// </summary>
/// <remarks>
/// Offsets are zero-based. Line and column numbers are one-based for diagnostic display.
/// </remarks>
public class TemplateSourcePosition
{
    /// <summary>
    /// Stores the template source associated with this value.
    /// </summary>
    public TemplateSource Source;
    /// <summary>
    /// Specifies the offset for this source position.
    /// </summary>
    public int Offset;
    /// <summary>
    /// Specifies the line number for this source position.
    /// </summary>
    public int LineNumber;
    /// <summary>
    /// Specifies the column number for this source position.
    /// </summary>
    public int ColumnNumber;
    /// <summary>
    /// Specifies the line start for this source position.
    /// </summary>
    public int LineStart;
    /// <summary>
    /// Specifies the line end for this source position.
    /// </summary>
    public int LineEnd;

    /// <summary>
    /// Initializes a new template source position and validates any supplied values.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <param name="offset">The zero-based character offset.</param>
    /// <param name="lineNumber">The one-based line number.</param>
    /// <param name="columnNumber">The one-based column number.</param>
    /// <param name="lineStart">The line start used by the operation.</param>
    /// <param name="lineEnd">The line end used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateSourcePosition(TemplateSource source, int offset, int lineNumber, int columnNumber, int lineStart, int lineEnd)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        Source = source;
        Offset = offset;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
        LineStart = lineStart;
        LineEnd = lineEnd;
    }
}

/// <summary>
/// Describes one line and its range within a template source.
/// </summary>
/// <remarks>
/// The end offset excludes newline characters and can be used to extract visible line text.
/// </remarks>
public class TemplateSourceLine
{
    /// <summary>
    /// Stores the template source associated with this value.
    /// </summary>
    public TemplateSource Source;
    /// <summary>
    /// Specifies the line number for this source line.
    /// </summary>
    public int LineNumber;
    /// <summary>
    /// Stores the zero-based start offset.
    /// </summary>
    public int Start;
    /// <summary>
    /// Stores the exclusive end offset.
    /// </summary>
    public int End;
    /// <summary>
    /// Stores the length in characters.
    /// </summary>
    public int Length;

    /// <summary>
    /// Initializes a new template source line and validates any supplied values.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <param name="lineNumber">The one-based line number.</param>
    /// <param name="start">The zero-based start offset.</param>
    /// <param name="end">The end used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument lies outside the supported range.</exception>
    public TemplateSourceLine(TemplateSource source, int lineNumber, int start, int end)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (lineNumber < 1)
        {
            throw new ArgumentOutOfRangeException("lineNumber");
        }

        if (start < 0)
        {
            throw new ArgumentOutOfRangeException("start");
        }

        if (end < start)
        {
            throw new ArgumentOutOfRangeException("end");
        }

        if (end > source.Length)
        {
            throw new ArgumentOutOfRangeException("end");
        }

        Source = source;
        LineNumber = lineNumber;
        Start = start;
        End = end;
        Length = end - start;
    }

    /// <summary>
    /// Determines whether the requested item exists without returning its contents.
    /// </summary>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool ContainsOffset(int offset)
    {
        return offset >= Start && offset < End;
    }

    /// <summary>
    /// Returns the text.
    /// </summary>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal string GetText()
    {
        return Source.Text.Substring(Start, Length);
    }
}

