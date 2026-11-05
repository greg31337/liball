using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Maps offsets, lines, and columns for a template source with support for common newline sequences.
/// </summary>
public class TemplateSourceLineMap : ITemplateSourceLineMap
{
    /// <summary>
    /// Stores the template source associated with this value.
    /// </summary>
    internal TemplateSource Source;
    /// <summary>
    /// Specifies the line starts for this source line map.
    /// </summary>
    internal int[] LineStarts;
    /// <summary>
    /// Stores the current line count.
    /// </summary>
    internal int LineCount;

    /// <summary>
    /// Initializes a new template source line map and validates any supplied values.
    /// </summary>
    /// <param name="source">The template source and its identity metadata.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public TemplateSourceLineMap(TemplateSource source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        source.Validate();
        Source = source;
        LineStarts = BuildLineStarts(source);
        LineCount = LineStarts.Length;
    }

    /// <summary>
    /// Gets the number of source lines.
    /// </summary>
    /// <returns>The source line count.</returns>
    public int GetLineCount()
    {
        return LineCount;
    }

    /// <summary>
    /// Returns the position.
    /// </summary>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public TemplateSourcePosition GetPosition(int offset)
    {
        ValidateOffset(offset);

        int lineIndex = FindLineIndex(offset);
        int lineStart = LineStarts[lineIndex];
        int lineEnd = GetLineEnd(lineStart);
        int lineNumber = lineIndex + 1;
        int columnOffset = offset - lineStart;
        if (columnOffset == 2147483647)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        int columnNumber = columnOffset + 1;
        return new TemplateSourcePosition(Source, offset, lineNumber, columnNumber, lineStart, lineEnd);
    }

    /// <summary>
    /// Returns the line.
    /// </summary>
    /// <param name="lineNumber">The one-based line number.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public TemplateSourceLine GetLine(int lineNumber)
    {
        ValidateLineNumber(lineNumber);

        int lineIndex = lineNumber - 1;
        int lineStart = LineStarts[lineIndex];
        int lineEnd = GetLineEnd(lineStart);
        return new TemplateSourceLine(Source, lineNumber, lineStart, lineEnd);
    }

    /// <summary>
    /// Returns the line text.
    /// </summary>
    /// <param name="lineNumber">The one-based line number.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    public string GetLineText(int lineNumber)
    {
        TemplateSourceLine line = GetLine(lineNumber);
        return line.GetText();
    }

    /// <summary>
    /// Returns the offset.
    /// </summary>
    /// <param name="lineNumber">The one-based line number.</param>
    /// <param name="columnNumber">The one-based column number.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument lies outside the supported range.</exception>
    public int GetOffset(int lineNumber, int columnNumber)
    {
        TemplateSourceLine line = GetLine(lineNumber);
        if (columnNumber < 1)
        {
            throw new ArgumentOutOfRangeException("columnNumber");
        }

        int columnOffset = columnNumber - 1;
        if (columnOffset > line.Length)
        {
            throw new ArgumentOutOfRangeException("columnNumber");
        }

        return line.Start + columnOffset;
    }

    /// <summary>
    /// Determines whether the offset at line start condition is satisfied.
    /// </summary>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsOffsetAtLineStart(int offset)
    {
        TemplateSourcePosition position = GetPosition(offset);
        return position.Offset == position.LineStart;
    }

    /// <summary>
    /// Determines whether the offset at line end condition is satisfied.
    /// </summary>
    /// <param name="offset">The zero-based character offset.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public bool IsOffsetAtLineEnd(int offset)
    {
        TemplateSourcePosition position = GetPosition(offset);
        return position.Offset == position.LineEnd;
    }

    internal static int[] BuildLineStarts(TemplateSource source)
    {
        List<int> starts = new List<int>();
        starts.Add(0);

        int index = 0;
        while (index < source.Length)
        {
            if (source.Text[index] == '\r')
            {
                index = index + 1;
                if (index < source.Length && source.Text[index] == '\n')
                {
                    index = index + 1;
                }

                starts.Add(index);
            }
            else if (source.Text[index] == '\n')
            {
                index = index + 1;
                starts.Add(index);
            }
            else
            {
                index = index + 1;
            }
        }

        return starts.ToArray();
    }

    internal int FindLineIndex(int offset)
    {
        int low = 0;
        int high = LineStarts.Length - 1;
        int result = 0;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int lineStart = LineStarts[middle];
            if (lineStart <= offset)
            {
                result = middle;
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return result;
    }

    internal int GetLineEnd(int lineStart)
    {
        int index = lineStart;
        while (index < Source.Length && Source.Text[index] != '\r' && Source.Text[index] != '\n')
        {
            index = index + 1;
        }

        return index;
    }

    internal void ValidateOffset(int offset)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (offset > Source.Length)
        {
            throw new ArgumentOutOfRangeException("offset");
        }
    }

    internal void ValidateLineNumber(int lineNumber)
    {
        if (lineNumber < 1)
        {
            throw new ArgumentOutOfRangeException("lineNumber");
        }

        if (lineNumber > LineCount)
        {
            throw new ArgumentOutOfRangeException("lineNumber");
        }
    }
}
