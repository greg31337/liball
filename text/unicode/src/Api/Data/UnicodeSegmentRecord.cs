namespace unicode;

/// <summary>
/// Describes a half-open UTF-16 range returned by a Unicode segmentation operation.
/// </summary>
public class UnicodeSegmentRecord
{
    /// <summary>
    /// The inclusive zero-based UTF-16 code-unit index where the range starts.
    /// </summary>
    public int StartUtf16Index;
    /// <summary>
    /// The exclusive zero-based UTF-16 code-unit index where the range ends.
    /// </summary>
    public int EndUtf16Index;
    /// <summary>
    /// The length of the range in UTF-16 code units.
    /// </summary>
    public int Utf16Length;

    /// <summary>
    /// Initializes a new instance with the supplied values.
    /// </summary>
    /// <param name="startUtf16Index">The inclusive start index in UTF-16 code units.</param>
    /// <param name="endUtf16Index">The exclusive end index in UTF-16 code units.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>startUtf16Index</c> is outside the supported range.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values do not form a valid input.</exception>
    public UnicodeSegmentRecord(int startUtf16Index, int endUtf16Index)
    {
        if (startUtf16Index < 0)
        {
            throw new System.ArgumentOutOfRangeException("startUtf16Index");
        }

        if (endUtf16Index < startUtf16Index)
        {
            throw new System.ArgumentException("Segment end must be greater than or equal to segment start.");
        }

        StartUtf16Index = startUtf16Index;
        EndUtf16Index = endUtf16Index;
        Utf16Length = endUtf16Index - startUtf16Index;
    }

    /// <summary>
    /// Reports whether the specified value lies in this half-open UTF-16 range or inclusive code-point range.
    /// </summary>
    /// <param name="utf16Index">The zero-based UTF-16 code-unit index to test.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool Contains(int utf16Index)
    {
        if (utf16Index < StartUtf16Index)
        {
            return false;
        }

        return utf16Index < EndUtf16Index;
    }
}
