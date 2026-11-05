namespace uri;

/// <summary>
/// Describes a zero-based range in an operation's input or output text.
/// </summary>
public class TextRange
{
    /// <summary>The zero-based offset at which the range starts.</summary>
    public int Offset;

    /// <summary>The number of UTF-16 code units in the range.</summary>
    public int Length;

    /// <summary>
    /// Initializes a text range.
    /// </summary>
    /// <param name="offset">The zero-based start offset.</param>
    /// <param name="length">The number of UTF-16 code units in the range.</param>
    public TextRange(int offset, int length)
    {
        Offset = offset;
        Length = length;
    }
}
