using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRange"/>.
/// </summary>
public interface IEmailRangeContract
{
    /// <summary>
    /// Gets the start offset.
    /// </summary>
    /// <returns>The current start offset.</returns>
    long GetStartOffset();
    /// <summary>
    /// Gets the length.
    /// </summary>
    /// <returns>The current length.</returns>
    long GetLength();
    /// <summary>
    /// Gets the end offset exclusive.
    /// </summary>
    /// <returns>The current end offset exclusive.</returns>
    long GetEndOffsetExclusive();
    /// <summary>
    /// Determines whether the current value or state is empty.
    /// </summary>
    /// <returns><c>true</c> when the value or state is empty; otherwise, <c>false</c>.</returns>
    bool IsEmpty();
    /// <summary>
    /// Determines whether the collection contains offset.
    /// </summary>
    /// <param name = "offset">The zero-based byte offset.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool ContainsOffset(long offset);
    /// <summary>
    /// Determines whether the collection contains range.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool ContainsRange(EmailRange? other);
    /// <summary>
    /// Determines whether this range intersects the supplied range.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    bool Intersects(EmailRange? other);
    /// <summary>
    /// Slices the supplied value.
    /// </summary>
    /// <param name = "relativeOffset">The zero-based relative offset.</param>
    /// <param name = "sliceLength">The slice length used by the operation.</param>
    /// <returns>The range produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailRange Slice(long relativeOffset, long sliceLength);
    /// <summary>
    /// Determines whether this value equals the supplied range.
    /// </summary>
    /// <param name = "other">The other used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool EqualsRange(EmailRange other);
}
