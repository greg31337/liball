namespace svg;

/// <summary>Identifies a source resource and its nested loading provenance.</summary>
/// <remarks>
/// URI text is preserved exactly. URI normalization belongs to resource resolution.
/// The root resource path is <c>0</c>; nested path segments are zero-based resource
/// ordinals padded to ten digits so ordinal string ordering is deterministic.
/// </remarks>
public class SvgSourceUri
{
    /// <summary>The source URI text, or an empty string for an anonymous source.</summary>
    public string Uri;

    /// <summary>The containing resource, or <c>null</c> for the root resource.</summary>
    public SvgSourceUri? Parent;

    /// <summary>The parent span that requested this resource, or <c>null</c> for the root resource.</summary>
    public SvgSourceSpan? ReferenceSpan;

    /// <summary>The stable ordinal path from the root resource.</summary>
    public string ResourcePath;

    /// <summary>The zero-based ordinal within the parent resource.</summary>
    public int ResourceIndex;

    /// <summary>The nesting depth, where the root resource has depth zero.</summary>
    public int Depth;

    /// <summary>Creates a root source resource.</summary>
    /// <param name="uri">The preserved URI text, or an empty string for an anonymous source.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>uri</c> is <c>null</c>.</exception>
    public SvgSourceUri(string uri)
    {
        if (uri == null)
        {
            throw new System.ArgumentNullException("uri");
        }

        Uri = uri;
        Parent = null;
        ReferenceSpan = null;
        ResourcePath = "0";
        ResourceIndex = 0;
        Depth = 0;
    }

    /// <summary>Creates a nested source resource.</summary>
    /// <param name="uri">The preserved nested-resource URI text.</param>
    /// <param name="parent">The resource containing the reference.</param>
    /// <param name="referenceSpan">The exact span that requested the nested resource.</param>
    /// <param name="resourceIndex">The zero-based resource ordinal within <c>parent</c>.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when a required reference is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>resourceIndex</c> is negative.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the reference span does not belong to <c>parent</c>.</exception>
    /// <returns>A new nested source resource.</returns>
    internal static SvgSourceUri CreateNested(string uri, SvgSourceUri? parent, SvgSourceSpan? referenceSpan, int resourceIndex)
    {
        if (uri == null)
        {
            throw new System.ArgumentNullException("uri");
        }
        if (parent == null)
        {
            throw new System.ArgumentNullException("parent");
        }
        if (referenceSpan == null)
        {
            throw new System.ArgumentNullException("referenceSpan");
        }
        if (resourceIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("resourceIndex");
        }
        if (!object.ReferenceEquals(referenceSpan.Source, parent))
        {
            throw new System.ArgumentException("The reference span must belong to the parent resource.", "referenceSpan");
        }

        SvgSourceUri result = new SvgSourceUri(uri);
        result.Parent = parent;
        result.ReferenceSpan = referenceSpan;
        result.ResourceIndex = resourceIndex;
        result.ResourcePath = parent.ResourcePath + "/" + resourceIndex.ToString("D10", System.Globalization.CultureInfo.InvariantCulture);
        result.Depth = parent.Depth + 1;
        return result;
    }

    /// <summary>Tests whether another object identifies this exact source resource.</summary>
    /// <param name="other">The source resource to test.</param>
    /// <returns><c>true</c> when both references identify the same resource object.</returns>
    internal bool IsSameResource(SvgSourceUri other)
    {
        return object.ReferenceEquals(this, other);
    }
}

/// <summary>Represents a position in decoded source text.</summary>
/// <remarks>
/// Offsets are zero-based UTF-16 character offsets. Lines and columns are
/// one-based. An unavailable offset is <c>-1</c>; unavailable line and column
/// values are both zero.
/// </remarks>
public class SvgSourcePosition
{
    /// <summary>The zero-based UTF-16 character offset, or <c>-1</c> when unavailable.</summary>
    public long Offset;

    /// <summary>The one-based line, or zero when unavailable.</summary>
    public int Line;

    /// <summary>The one-based column, or zero when unavailable.</summary>
    public int Column;

    /// <summary>Creates a source position.</summary>
    /// <param name="offset">The zero-based character offset, or <c>-1</c>.</param>
    /// <param name="line">The one-based line, or zero.</param>
    /// <param name="column">The one-based column, or zero.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown for values outside the documented ranges.</exception>
    /// <exception cref="System.ArgumentException">Thrown when only one line-coordinate component is available.</exception>
    public SvgSourcePosition(long offset, int line, int column)
    {
        if (offset < -1)
        {
            throw new System.ArgumentOutOfRangeException("offset");
        }
        if (line < 0)
        {
            throw new System.ArgumentOutOfRangeException("line");
        }
        if (column < 0)
        {
            throw new System.ArgumentOutOfRangeException("column");
        }
        if ((line == 0 && column != 0) || (line != 0 && column == 0))
        {
            throw new System.ArgumentException("Line and column must both be available or unavailable.");
        }

        Offset = offset;
        Line = line;
        Column = column;
    }

    /// <summary>Creates a position for which no coordinates are available.</summary>
    /// <returns>A new unknown position.</returns>
    internal static SvgSourcePosition Unknown()
    {
        return new SvgSourcePosition(-1, 0, 0);
    }

    /// <summary>Tests whether a character offset is available.</summary>
    /// <returns><c>true</c> when <see cref="Offset"/> is available.</returns>
    internal bool HasOffset()
    {
        return Offset >= 0;
    }

    /// <summary>Tests whether line and column coordinates are available.</summary>
    /// <returns><c>true</c> when both coordinates are available.</returns>
    internal bool HasLineColumn()
    {
        return Line > 0;
    }

    /// <summary>Compares two positions using offsets when available and line/column otherwise.</summary>
    /// <param name="other">The position to compare.</param>
    /// <returns>A negative value, zero, or a positive value according to source order.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>other</c> is <c>null</c>.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when the positions have no common coordinate system.</exception>
    internal int CompareTo(SvgSourcePosition other)
    {
        if (other == null)
        {
            throw new System.ArgumentNullException("other");
        }

        if (Offset >= 0 && other.Offset >= 0)
        {
            if (Offset < other.Offset)
            {
                return -1;
            }
            if (Offset > other.Offset)
            {
                return 1;
            }
            return 0;
        }

        if (Line > 0 && other.Line > 0)
        {
            if (Line < other.Line)
            {
                return -1;
            }
            if (Line > other.Line)
            {
                return 1;
            }
            if (Column < other.Column)
            {
                return -1;
            }
            if (Column > other.Column)
            {
                return 1;
            }
            return 0;
        }

        throw new System.InvalidOperationException("The positions have no common coordinates.");
    }
}

/// <summary>Represents a half-open span in one source resource.</summary>
/// <remarks>
/// The start position is inclusive and the end position is exclusive. Empty
/// spans are valid. Both endpoints must use the same available coordinate
/// systems and must be ordered from start to end.
/// </remarks>
public class SvgSourceSpan
{
    /// <summary>The source resource containing this span.</summary>
    public SvgSourceUri Source;

    /// <summary>The zero-based inclusive start offset, or <c>-1</c>.</summary>
    public long StartOffset;

    /// <summary>The zero-based exclusive end offset, or <c>-1</c>.</summary>
    public long EndOffset;

    /// <summary>The one-based start line, or zero.</summary>
    public int StartLine;

    /// <summary>The one-based start column, or zero.</summary>
    public int StartColumn;

    /// <summary>The one-based end line, or zero.</summary>
    public int EndLine;

    /// <summary>The one-based end column, or zero.</summary>
    public int EndColumn;

    /// <summary>Creates a source span from endpoint objects.</summary>
    /// <param name="source">The source resource.</param>
    /// <param name="start">The inclusive start position.</param>
    /// <param name="end">The exclusive end position.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when a required value is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentException">Thrown when endpoint coordinates are inconsistent or reversed.</exception>
    /// <returns>A new source span.</returns>
    internal static SvgSourceSpan FromPositions(SvgSourceUri source, SvgSourcePosition start, SvgSourcePosition end)
    {
        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }
        if (start == null)
        {
            throw new System.ArgumentNullException("start");
        }
        if (end == null)
        {
            throw new System.ArgumentNullException("end");
        }
        if ((start.Offset >= 0) != (end.Offset >= 0))
        {
            throw new System.ArgumentException("Both endpoint offsets must be available or unavailable.");
        }
        if ((start.Line > 0) != (end.Line > 0))
        {
            throw new System.ArgumentException("Both endpoint line coordinates must be available or unavailable.");
        }
        if (start.Offset >= 0 && start.Offset > end.Offset)
        {
            throw new System.ArgumentException("The end position must not precede the start position.");
        }
        if (start.Line > 0 && (start.Line > end.Line || (start.Line == end.Line && start.Column > end.Column)))
        {
            throw new System.ArgumentException("The end line and column must not precede the start line and column.");
        }

        return new SvgSourceSpan(source, start.Offset, start.Line, start.Column, end.Offset, end.Line, end.Column);
    }

    /// <summary>Creates a source span from scalar endpoint coordinates.</summary>
    /// <param name="source">The source resource.</param>
    /// <param name="startOffset">The inclusive zero-based start offset, or <c>-1</c>.</param>
    /// <param name="startLine">The one-based start line, or zero.</param>
    /// <param name="startColumn">The one-based start column, or zero.</param>
    /// <param name="endOffset">The exclusive zero-based end offset, or <c>-1</c>.</param>
    /// <param name="endLine">The one-based end line, or zero.</param>
    /// <param name="endColumn">The one-based end column, or zero.</param>
    public SvgSourceSpan(SvgSourceUri source, long startOffset, int startLine, int startColumn, long endOffset, int endLine, int endColumn)
    {
        SvgSourcePosition start = new SvgSourcePosition(startOffset, startLine, startColumn);
        SvgSourcePosition end = new SvgSourcePosition(endOffset, endLine, endColumn);

        if (source == null)
        {
            throw new System.ArgumentNullException("source");
        }
        if ((start.Offset >= 0) != (end.Offset >= 0))
        {
            throw new System.ArgumentException("Both endpoint offsets must be available or unavailable.");
        }
        if ((start.Line > 0) != (end.Line > 0))
        {
            throw new System.ArgumentException("Both endpoint line coordinates must be available or unavailable.");
        }
        if (start.Offset >= 0 && start.Offset > end.Offset)
        {
            throw new System.ArgumentException("The end position must not precede the start position.");
        }
        if (start.Line > 0 && (start.Line > end.Line || (start.Line == end.Line && start.Column > end.Column)))
        {
            throw new System.ArgumentException("The end line and column must not precede the start line and column.");
        }

        Source = source;
        StartOffset = start.Offset;
        EndOffset = end.Offset;
        StartLine = start.Line;
        StartColumn = start.Column;
        EndLine = end.Line;
        EndColumn = end.Column;
    }

    /// <summary>Creates a span for which no endpoint coordinates are available.</summary>
    /// <param name="source">The source resource associated with the unknown span.</param>
    /// <returns>A new unknown span.</returns>
    internal static SvgSourceSpan Unknown(SvgSourceUri source)
    {
        return new SvgSourceSpan(source, -1, 0, 0, -1, 0, 0);
    }

    /// <summary>Returns the span length in UTF-16 characters.</summary>
    /// <returns>The nonnegative length, or <c>-1</c> when offsets are unavailable.</returns>
    internal long Length()
    {
        if (StartOffset < 0)
        {
            return -1;
        }
        return EndOffset - StartOffset;
    }

    /// <summary>Tests an offset against this half-open span.</summary>
    /// <param name="offset">The zero-based UTF-16 character offset.</param>
    /// <returns><c>true</c> when the offset is within the span.</returns>
    internal bool ContainsOffset(long offset)
    {
        if (StartOffset < 0 || offset < 0)
        {
            return false;
        }
        return offset >= StartOffset && offset < EndOffset;
    }

    /// <summary>Tests whether this span entirely contains another span.</summary>
    /// <param name="other">The candidate contained span.</param>
    /// <returns><c>true</c> when the spans share a source and the candidate is contained.</returns>
    internal bool ContainsSpan(SvgSourceSpan? other)
    {
        if (other == null || !object.ReferenceEquals(Source, other.Source))
        {
            return false;
        }

        if (StartOffset >= 0 && other.StartOffset >= 0)
        {
            return other.StartOffset >= StartOffset && other.EndOffset <= EndOffset;
        }

        if (StartLine > 0 && other.StartLine > 0)
        {
            bool containsStart = StartLine < other.StartLine || (StartLine == other.StartLine && StartColumn <= other.StartColumn);
            bool containsEnd = EndLine > other.EndLine || (EndLine == other.EndLine && EndColumn >= other.EndColumn);
            return containsStart && containsEnd;
        }

        return false;
    }

    /// <summary>Tests whether this half-open span overlaps another span.</summary>
    /// <param name="other">The candidate overlapping span.</param>
    /// <returns><c>true</c> when nonempty portions overlap in the same source.</returns>
    internal bool Overlaps(SvgSourceSpan? other)
    {
        if (other == null || !object.ReferenceEquals(Source, other.Source))
        {
            return false;
        }

        if (StartOffset >= 0 && other.StartOffset >= 0)
        {
            return StartOffset < other.EndOffset && other.StartOffset < EndOffset;
        }

        if (StartLine > 0 && other.StartLine > 0)
        {
            bool startsBeforeOtherEnd = StartLine < other.EndLine || (StartLine == other.EndLine && StartColumn < other.EndColumn);
            bool otherStartsBeforeEnd = other.StartLine < EndLine || (other.StartLine == EndLine && other.StartColumn < EndColumn);
            return startsBeforeOtherEnd && otherStartsBeforeEnd;
        }

        return false;
    }
}
