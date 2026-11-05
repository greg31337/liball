using System;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailContent"/>.
/// </summary>
public interface IEmailContentContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailContentKind GetKind();
    /// <summary>
    /// Gets the ownership.
    /// </summary>
    /// <returns>The current ownership.</returns>
    EmailContentOwnership GetOwnership();
    /// <summary>
    /// Determines whether the current value or state is repeatable.
    /// </summary>
    /// <returns><c>true</c> when the value or state is repeatable; otherwise, <c>false</c>.</returns>
    bool IsRepeatable();
    /// <summary>
    /// Determines whether the current value or state is closed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closed; otherwise, <c>false</c>.</returns>
    bool IsClosed();
    /// <summary>
    /// Gets the clears sensitive bytes.
    /// </summary>
    /// <returns><c>true</c> when the clears sensitive bytes setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetClearsSensitiveBytes();
    /// <summary>
    /// Determines whether the known length is present or available.
    /// </summary>
    /// <returns><c>true</c> when the known length is available; otherwise, <c>false</c>.</returns>
    bool HasKnownLength();
    /// <summary>
    /// Gets the length.
    /// </summary>
    /// <returns>The current length.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    long GetLength();
    /// <summary>
    /// Determines whether the file path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the file path is available; otherwise, <c>false</c>.</returns>
    bool HasFilePath();
    /// <summary>
    /// Gets the file path.
    /// </summary>
    /// <returns>The current file path.</returns>
    string? GetFilePath();
    /// <summary>
    /// Opens a read for caller-controlled reading or access.
    /// </summary>
    /// <returns>The stream produced by the operation.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    Stream? OpenRead();
    /// <summary>
    /// Opens a read range for caller-controlled reading or access.
    /// </summary>
    /// <param name = "offset">The zero-based byte offset.</param>
    /// <param name = "length">The length used by the operation.</param>
    /// <returns>The stream produced by the operation.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    Stream OpenReadRange(long offset, long length);
    /// <summary>
    /// Determines whether the current value or state is range read stream borrowed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is range read stream borrowed; otherwise, <c>false</c>.</returns>
    bool IsRangeReadStreamBorrowed();
    /// <summary>
    /// Materializes the supplied value.
    /// </summary>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    byte[] Materialize(long maximumBytes);
    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    void Close();
}
