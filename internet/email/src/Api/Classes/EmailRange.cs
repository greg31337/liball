using System;

namespace email;
/// <summary>
/// Represents a range.
/// </summary>
public class EmailRange : IEmailRangeContract
{
    long startOffset;
    long length;
    /// <summary>
    /// Creates a new range.
    /// </summary>
    /// <param name = "startOffset">The zero-based start offset.</param>
    /// <param name = "length">The length used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    public EmailRange(long startOffset, long length)
    {
        if (startOffset < 0)
        {
            throw new ArgumentOutOfRangeException("startOffset");
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (length > long.MaxValue - startOffset)
        {
            throw new OverflowException("The source range end exceeds Int64.MaxValue.");
        }

        this.startOffset = startOffset;
        this.length = length;
    }

    /// <summary>
    /// Gets the start offset.
    /// </summary>
    /// <returns>The current start offset.</returns>
    public long GetStartOffset()
    {
        return startOffset;
    }

    /// <summary>
    /// Gets the length.
    /// </summary>
    /// <returns>The current length.</returns>
    public long GetLength()
    {
        return length;
    }

    /// <summary>
    /// Gets the end offset exclusive.
    /// </summary>
    /// <returns>The current end offset exclusive.</returns>
    public long GetEndOffsetExclusive()
    {
        return startOffset + length;
    }

    /// <summary>
    /// Determines whether the current value or state is empty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is empty; otherwise, <c>false</c>.</returns>
    public bool IsEmpty()
    {
        return length == 0;
    }

    /// <summary>
    /// Determines whether the collection contains offset.
    /// </summary>
    /// <param name = "offset">The zero-based byte offset.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool ContainsOffset(long offset)
    {
        if (offset < 0)
        {
            return false;
        }

        return offset >= startOffset && offset < startOffset + length;
    }

    /// <summary>
    /// Determines whether the collection contains range.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool ContainsRange(EmailRange? other)
    {
        if (other == null)
        {
            throw new ArgumentNullException("other");
        }

        return other.GetStartOffset() >= startOffset && other.GetEndOffsetExclusive() <= startOffset + length;
    }

    /// <summary>
    /// Determines whether this range intersects the supplied range.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public bool Intersects(EmailRange? other)
    {
        if (other == null)
        {
            throw new ArgumentNullException("other");
        }

        if (length == 0 || other.GetLength() == 0)
        {
            return false;
        }

        return startOffset < other.GetEndOffsetExclusive() && other.GetStartOffset() < startOffset + length;
    }

    /// <summary>
    /// Slices the supplied value.
    /// </summary>
    /// <param name = "relativeOffset">The zero-based relative offset.</param>
    /// <param name = "sliceLength">The slice length used by the operation.</param>
    /// <returns>The range produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRange Slice(long relativeOffset, long sliceLength)
    {
        if (relativeOffset < 0)
        {
            throw new ArgumentOutOfRangeException("relativeOffset");
        }

        if (sliceLength < 0)
        {
            throw new ArgumentOutOfRangeException("sliceLength");
        }

        if (relativeOffset > length || sliceLength > length - relativeOffset)
        {
            throw new ArgumentOutOfRangeException("sliceLength");
        }

        return new EmailRange(startOffset + relativeOffset, sliceLength);
    }

    /// <summary>
    /// Determines whether this value equals the supplied range.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool EqualsRange(EmailRange other)
    {
        if (other == null)
        {
            return false;
        }

        return startOffset == other.GetStartOffset() && length == other.GetLength();
    }
}
