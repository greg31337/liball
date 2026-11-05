namespace http;

/// <summary>
/// Represents a requested or normalized byte range.
/// </summary>
public class HttpByteRange
{
    /// <summary>
    /// Indicates that an inclusive first-byte position is present.
    /// </summary>
    public bool HasStart;

    /// <summary>
    /// The inclusive first-byte position.
    /// </summary>
    public long Start;

    /// <summary>
    /// Indicates that an inclusive last-byte position is present.
    /// </summary>
    public bool HasEnd;

    /// <summary>
    /// The inclusive last-byte position.
    /// </summary>
    public long End;

    /// <summary>
    /// Indicates that the range is expressed as a suffix length.
    /// </summary>
    public bool IsSuffix;

    /// <summary>
    /// The requested suffix length when <see cref="IsSuffix"/> is set.
    /// </summary>
    public long SuffixLength;

    /// <summary>
    /// Indicates that the range has been resolved against a representation length.
    /// </summary>
    public bool IsNormalized;
}
