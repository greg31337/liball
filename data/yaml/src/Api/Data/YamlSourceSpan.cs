namespace yaml;

/// <summary>
/// Identifies a half-open source range by absolute index and one-based line and column positions.
/// </summary>
public class YamlSourceSpan
{
    /// <summary>
    /// The zero-based index of the first source character in the range.
    /// </summary>
    public int StartIndex;
    /// <summary>
    /// The one-based line number at which the range starts.
    /// </summary>
    public int StartLine;
    /// <summary>
    /// The one-based column number at which the range starts.
    /// </summary>
    public int StartColumn;
    /// <summary>
    /// The zero-based index immediately after the last source character in the range.
    /// </summary>
    public int EndIndex;
    /// <summary>
    /// The one-based line number at which the range ends.
    /// </summary>
    public int EndLine;
    /// <summary>
    /// The one-based column number at which the range ends.
    /// </summary>
    public int EndColumn;

    /// <summary>
    /// Initializes an empty source range at index, line, and column zero.
    /// </summary>
    public YamlSourceSpan()
    {
        StartIndex = 0;
        StartLine = 0;
        StartColumn = 0;
        EndIndex = 0;
        EndLine = 0;
        EndColumn = 0;
    }

    /// <summary>
    /// Creates an empty source range at index, line, and column zero.
    /// </summary>
    /// <returns>The resulting YAML source span.</returns>
    internal static YamlSourceSpan Empty()
    {
        return new YamlSourceSpan();
    }

    /// <summary>
    /// Creates a source range from explicit inclusive start and exclusive end coordinates.
    /// </summary>
    /// <param name="startIndex">The zero-based start index.</param>
    /// <param name="startLine">The one-based start line.</param>
    /// <param name="startColumn">The one-based start column.</param>
    /// <param name="endIndex">The zero-based exclusive end index.</param>
    /// <param name="endLine">The one-based end line.</param>
    /// <param name="endColumn">The one-based end column.</param>
    /// <returns>The resulting YAML source span.</returns>
    internal static YamlSourceSpan FromBounds(int startIndex, int startLine, int startColumn, int endIndex, int endLine, int endColumn)
    {
        YamlSourceSpan span = new YamlSourceSpan();
        span.Set(startIndex, startLine, startColumn, endIndex, endLine, endColumn);
        return span;
    }

    /// <summary>
    /// Replaces this instance's stored values with the supplied values.
    /// </summary>
    /// <param name="startIndex">The zero-based start index.</param>
    /// <param name="startLine">The one-based start line.</param>
    /// <param name="startColumn">The one-based start column.</param>
    /// <param name="endIndex">The zero-based exclusive end index.</param>
    /// <param name="endLine">The one-based end line.</param>
    /// <param name="endColumn">The one-based end column.</param>
    internal void Set(int startIndex, int startLine, int startColumn, int endIndex, int endLine, int endColumn)
    {
        StartIndex = startIndex;
        StartLine = startLine;
        StartColumn = startColumn;
        EndIndex = endIndex;
        EndLine = endLine;
        EndColumn = endColumn;
    }

    /// <summary>
    /// Returns the non-negative number of UTF-16 characters covered by this range.
    /// </summary>
    /// <returns>The number of matching, changed, or stored items.</returns>
    internal int Length()
    {
        if (EndIndex <= StartIndex)
        {
            return 0;
        }

        long length = (long)EndIndex - (long)StartIndex;
        if (length > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)length;
    }

    /// <summary>
    /// Determines whether the range covers zero source characters.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool IsEmpty()
    {
        return StartIndex == EndIndex
            && StartLine == EndLine
            && StartColumn == EndColumn;
    }

    /// <summary>
    /// Determines whether a matching value is present.
    /// </summary>
    /// <param name="index">The zero-based source or sequence index.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool ContainsIndex(int index)
    {
        return index >= StartIndex && index < EndIndex;
    }

    /// <summary>
    /// Compares start indexes, then lines and columns when indexes are equal.
    /// </summary>
    /// <param name="other">The source range to compare.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool StartsBefore(YamlSourceSpan? other)
    {
        if (other == null)
        {
            return false;
        }

        if (StartIndex < other.StartIndex)
        {
            return true;
        }

        return StartIndex == other.StartIndex && EndIndex < other.EndIndex;
    }

    /// <summary>
    /// Replaces all coordinates with those from another source range.
    /// </summary>
    /// <param name="other">The source range to compare.</param>
    internal void CopyFrom(YamlSourceSpan? other)
    {
        if (other == null)
        {
            Set(0, 0, 0, 0, 0, 0);
            return;
        }

        Set(other.StartIndex, other.StartLine, other.StartColumn, other.EndIndex, other.EndLine, other.EndColumn);
    }

    /// <summary>
    /// Creates an independent copy of this value and its mutable collections.
    /// </summary>
    /// <returns>The resulting YAML source span.</returns>
    internal YamlSourceSpan Copy()
    {
        return FromBounds(StartIndex, StartLine, StartColumn, EndIndex, EndLine, EndColumn);
    }

    /// <summary>
    /// Creates the smallest source range that covers both supplied ranges.
    /// </summary>
    /// <param name="first">The first source range.</param>
    /// <param name="second">The second source range.</param>
    /// <returns>The resulting YAML source span.</returns>
    internal static YamlSourceSpan Cover(YamlSourceSpan? first, YamlSourceSpan? second)
    {
        if (first == null || first.IsEmpty())
        {
            return second == null ? Empty() : second.Copy();
        }

        if (second == null || second.IsEmpty())
        {
            return first.Copy();
        }

        int startIndex = first.StartIndex;
        int startLine = first.StartLine;
        int startColumn = first.StartColumn;
        if (SecondStartsBeforeFirst(first, second))
        {
            startIndex = second.StartIndex;
            startLine = second.StartLine;
            startColumn = second.StartColumn;
        }

        int endIndex = first.EndIndex;
        int endLine = first.EndLine;
        int endColumn = first.EndColumn;
        if (SecondEndsAfterFirst(first, second))
        {
            endIndex = second.EndIndex;
            endLine = second.EndLine;
            endColumn = second.EndColumn;
        }

        return FromBounds(startIndex, startLine, startColumn, endIndex, endLine, endColumn);
    }

    internal static bool SecondStartsBeforeFirst(YamlSourceSpan first, YamlSourceSpan second)
    {
        if (second.StartIndex < first.StartIndex)
        {
            return true;
        }

        return second.StartIndex == first.StartIndex && second.EndIndex < first.EndIndex;
    }

    internal static bool SecondEndsAfterFirst(YamlSourceSpan first, YamlSourceSpan second)
    {
        if (second.EndIndex > first.EndIndex)
        {
            return true;
        }

        return second.EndIndex == first.EndIndex && second.StartIndex > first.StartIndex;
    }

    /// <summary>
    /// Formats this value as plain text for diagnostics or display.
    /// </summary>
    /// <returns>The emitted or formatted text.</returns>
    internal string Format()
    {
        return StartLine.ToString()
            + ":"
            + StartColumn.ToString()
            + "-"
            + EndLine.ToString()
            + ":"
            + EndColumn.ToString();
    }
}
