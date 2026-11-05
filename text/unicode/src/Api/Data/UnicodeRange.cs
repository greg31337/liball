namespace unicode;

/// <summary>
/// Represents an inclusive range of Unicode code points.
/// </summary>
public class UnicodeRange
{
    /// <summary>
    /// The inclusive first code point in the range.
    /// </summary>
    public int Start;
    /// <summary>
    /// The inclusive last code point in the range.
    /// </summary>
    public int End;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="start">The inclusive first code point.</param>
    /// <param name="end">The inclusive last code point.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a bound is outside U+0000 through U+10FFFF.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values do not form a valid input.</exception>
    public UnicodeRange(int start, int end)
    {
        if (start < 0 || start > 0x10FFFF)
        {
            throw new System.ArgumentOutOfRangeException("start");
        }

        if (end < 0 || end > 0x10FFFF)
        {
            throw new System.ArgumentOutOfRangeException("end");
        }

        if (end < start)
        {
            throw new System.ArgumentException("Range end must be greater than or equal to start.");
        }

        Start = start;
        End = end;
    }

    /// <summary>
    /// Reports whether the specified value lies in this half-open UTF-16 range or inclusive code-point range.
    /// </summary>
    /// <param name="codePoint">The Unicode code point to inspect.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool Contains(int codePoint)
    {
        if (codePoint < Start)
        {
            return false;
        }

        return codePoint <= End;
    }

    /// <summary>
    /// Returns the number of code points, mappings, scalars, or UTF-16 positions represented by this value.
    /// </summary>
    /// <returns>The resulting integer value.</returns>
    internal int Count()
    {
        return End - Start + 1;
    }

    internal static UnicodeRange[] FromArrays(int[] starts, int[] ends)
    {
        System.ArgumentNullException.ThrowIfNull(starts);
        System.ArgumentNullException.ThrowIfNull(ends);

        if (starts.Length != ends.Length)
        {
            throw new System.ArgumentException("Range arrays must have the same length.");
        }

        UnicodeRange[] ranges = new UnicodeRange[starts.Length];
        int index = 0;
        while (index < starts.Length)
        {
            ranges[index] = new UnicodeRange(starts[index], ends[index]);
            index = index + 1;
        }

        return ranges;
    }
}
