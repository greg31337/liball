using System;

namespace sql;

/// <summary>Identifies a zero-based half-open range within SQL source text.</summary>
public class SqlTextSpan
{
    /// <summary>The start associated with this value.</summary>
    public int Start;
    /// <summary>The length associated with this value.</summary>
    public int Length;

    /// <summary>Initializes a new instance of <see cref="SqlTextSpan"/> from the supplied values.</summary>
    /// <param name="start">The zero-based start position.</param>
    /// <param name="length">The number of source characters in the span.</param>
    /// <exception cref="ArgumentOutOfRangeException">start is outside the supported range or otherwise invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">length is outside the supported range or otherwise invalid.</exception>
    public SqlTextSpan(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException("start");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (start > int.MaxValue - length)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        Start = start;
        Length = length;
    }

    /// <summary>Creates a half-open span from a start position and exclusive end position.</summary>
    /// <param name="start">The zero-based start position.</param>
    /// <param name="end">The zero-based exclusive end position.</param>
    /// <returns>The resulting text span.</returns>
    /// <exception cref="ArgumentOutOfRangeException">start is outside the supported range or otherwise invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">end is outside the supported range or otherwise invalid.</exception>
    internal static SqlTextSpan FromBounds(int start, int end)
    {
        if (start < 0)
        {
            throw new ArgumentOutOfRangeException("start");
        }

        if (end < start)
        {
            throw new ArgumentOutOfRangeException("end");
        }

        return new SqlTextSpan(start, end - start);
    }

    /// <summary>Returns the zero-based exclusive end of this span.</summary>
    /// <returns>The requested zero-based position.</returns>
    internal int GetEnd()
    {
        return Start + Length;
    }

    /// <summary>Returns whether this value contains no elements or source characters.</summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsEmpty()
    {
        return Length == 0;
    }

    /// <summary>Returns whether a position lies inside the half-open span.</summary>
    /// <param name="position">The zero-based source or stream position.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool Contains(int position)
    {
        return position >= Start && position < GetEnd();
    }

    /// <summary>Returns whether a position lies inside the span when its end boundary is included.</summary>
    /// <param name="position">The zero-based source or stream position.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool ContainsInclusiveEnd(int position)
    {
        return position >= Start && position <= GetEnd();
    }

    /// <summary>Returns whether the entire supplied span lies within this span.</summary>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    internal bool ContainsSpan(SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        return span.Start >= Start && span.GetEnd() <= GetEnd();
    }

    /// <summary>Returns whether this span and the supplied span share at least one position.</summary>
    /// <param name="span">The source span associated with the value.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">span is <c>null</c>.</exception>
    internal bool OverlapsWith(SqlTextSpan span)
    {
        if (span == null)
        {
            throw new ArgumentNullException("span");
        }

        return Length > 0 && span.Length > 0 && Start < span.GetEnd() && span.Start < GetEnd();
    }
}
