using System;
using System.IO;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTemporaryStore"/>.
/// </summary>
public interface IEmailTemporaryStoreContract
{
    /// <summary>
    /// Gets the length.
    /// </summary>
    /// <returns>The current length.</returns>
    long GetLength();
    /// <summary>
    /// Determines whether the current value or state is using temporary file.
    /// </summary>
    /// <returns><c>true</c> when the value or state is using temporary file; otherwise, <c>false</c>.</returns>
    bool IsUsingTemporaryFile();
    /// <summary>
    /// Determines whether the temporary path is present or available.
    /// </summary>
    /// <returns><c>true</c> when the temporary path is available; otherwise, <c>false</c>.</returns>
    bool HasTemporaryPath();
    /// <summary>
    /// Gets the temporary path.
    /// </summary>
    /// <returns>The current temporary path.</returns>
    string? GetTemporaryPath();
    /// <summary>
    /// Determines whether the current value or state is completed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is completed; otherwise, <c>false</c>.</returns>
    bool IsCompleted();
    /// <summary>
    /// Determines whether the current value or state is closed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closed; otherwise, <c>false</c>.</returns>
    bool IsClosed();
    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    void Write(ReadOnlySpan<byte> value);
    /// <summary>
    /// Writes the supplied byte to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    void WriteByte(byte value);
    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>The content produced by the operation.</returns>
    EmailContent Complete();
    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    void Close();
}
