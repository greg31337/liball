using System;
using System.Text;

namespace token;

/// <summary>
/// Describes the segment boundaries of a compact JWS, JWT, or JWE token.
/// </summary>
public class JoseCompactSegments
{
    /// <summary>Contains the exact compact token text supplied to the scanner.</summary>
    public string Text;

    /// <summary>Contains segment starts; the first <see cref="SegmentCount"/> entries are active.</summary>
    public int[] SegmentStarts;

    /// <summary>Contains segment lengths; the first <see cref="SegmentCount"/> entries are active.</summary>
    public int[] SegmentLengths;

    /// <summary>
    /// Contains the number of compact token segments.
    /// </summary>
    public int SegmentCount;

    internal JoseCompactSegments(string text, int[] starts, int[] lengths, int segmentCount)
    {
        Text = text;
        SegmentStarts = starts;
        SegmentLengths = lengths;
        SegmentCount = segmentCount;
    }

    /// <summary>
    /// Returns the original compact token text.
    /// </summary>
    /// <returns>The exact string supplied to the scanner.</returns>
    internal string GetText()
    {
        return Text;
    }

    /// <summary>
    /// Returns a segment as a view over the original compact token text.
    /// </summary>
    /// <param name="index">The zero-based segment index.</param>
    /// <returns>A read-only span containing the requested segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>index</c> does not identify a segment.</exception>
    internal ReadOnlySpan<char> GetSegment(int index)
    {
        if (index < 0 || index >= SegmentCount)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return Text.AsSpan(SegmentStarts[index], SegmentLengths[index]);
    }

    /// <summary>
    /// Returns the start position of a segment in the original token.
    /// </summary>
    /// <param name="index">The zero-based segment index.</param>
    /// <returns>The zero-based start position of the segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>index</c> does not identify a segment.</exception>
    internal int GetSegmentStart(int index)
    {
        if (index < 0 || index >= SegmentCount)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return SegmentStarts[index];
    }

    /// <summary>
    /// Returns the length of a segment.
    /// </summary>
    /// <param name="index">The zero-based segment index.</param>
    /// <returns>The number of characters in the segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>index</c> does not identify a segment.</exception>
    internal int GetSegmentLength(int index)
    {
        if (index < 0 || index >= SegmentCount)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return SegmentLengths[index];
    }
}
