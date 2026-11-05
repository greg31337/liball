using System;
using System.Collections.Generic;

namespace sql;

/// <summary>Maps zero-based source positions to zero-based line and column numbers.</summary>
public class SqlLineMap
{
    /// <summary>The text associated with this value.</summary>
    public string Text;
    /// <summary>The line starts in source or insertion order.</summary>
    public int[] LineStarts;

    /// <summary>Initializes a new instance of <see cref="SqlLineMap"/> from the supplied values.</summary>
    /// <param name="text">The source text to process.</param>
    /// <exception cref="ArgumentNullException">text is <c>null</c>.</exception>
    public SqlLineMap(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Text = text;
        LineStarts = BuildLineStarts(text);
    }

    /// <summary>Returns the number of line items.</summary>
    /// <returns>The requested count.</returns>
    internal int GetLineCount()
    {
        return LineStarts.Length;
    }

    /// <summary>Returns the line number associated with this value.</summary>
    /// <param name="position">The zero-based source or stream position.</param>
    /// <returns>The requested zero-based position.</returns>
    internal int GetLineNumber(int position)
    {
        ValidatePosition(position);
        return FindLineIndex(position) + 1;
    }

    /// <summary>Returns the column number associated with this value.</summary>
    /// <param name="position">The zero-based source or stream position.</param>
    /// <returns>The requested zero-based position.</returns>
    internal int GetColumnNumber(int position)
    {
        int lineIndex = 0;

        ValidatePosition(position);
        lineIndex = FindLineIndex(position);
        return position - LineStarts[lineIndex] + 1;
    }

    /// <summary>Returns the line start position associated with this value.</summary>
    /// <param name="lineNumber">The line number to use.</param>
    /// <returns>The requested zero-based position.</returns>
    internal int GetLineStartPosition(int lineNumber)
    {
        ValidateLineNumber(lineNumber);
        return LineStarts[lineNumber - 1];
    }

    /// <summary>Returns the line number for span associated with this value.</summary>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The requested zero-based position.</returns>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    internal int GetLineNumberForSpan(SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        return GetLineNumber(span.Start);
    }

    /// <summary>Returns the column number for span associated with this value.</summary>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns>The requested zero-based position.</returns>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    internal int GetColumnNumberForSpan(SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        return GetColumnNumber(span.Start);
    }

    /// <summary>Returns whether valid position.</summary>
    /// <param name="position">The zero-based source or stream position.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsValidPosition(int position)
    {
        return position >= 0 && position <= Text.Length;
    }

    internal static int[] BuildLineStarts(string text)
    {
        List<int> starts = new List<int>();
        int index = 0;

        starts.Add(0);
        while (index < text.Length)
        {
            if (text[index] == '\r')
            {
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index = index + 2;
                }
                else
                {
                    index = index + 1;
                }

                starts.Add(index);
            }
            else if (text[index] == '\n')
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

    internal int FindLineIndex(int position)
    {
        int low = 0;
        int high = LineStarts.Length - 1;
        int result = 0;

        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            if (LineStarts[middle] <= position)
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

    internal void ValidatePosition(int position)
    {
        if (!IsValidPosition(position))
        {
            throw new ArgumentOutOfRangeException("position");
        }
    }

    internal void ValidateLineNumber(int lineNumber)
    {
        if (lineNumber < 1 || lineNumber > LineStarts.Length)
        {
            throw new ArgumentOutOfRangeException("lineNumber");
        }
    }
}
