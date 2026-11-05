using System;
using System.Buffers;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBuffer"/>.
/// </summary>
public interface IEmailBufferContract
{
    /// <summary>
    /// Gets the length.
    /// </summary>
    /// <returns>The current length.</returns>
    int GetLength();
    /// <summary>
    /// Gets the capacity.
    /// </summary>
    /// <returns>The current capacity.</returns>
    int GetCapacity();
    /// <summary>
    /// Gets the maximum capacity.
    /// </summary>
    /// <returns>The current maximum capacity.</returns>
    int GetMaximumCapacity();
    /// <summary>
    /// Gets the clears sensitive bytes.
    /// </summary>
    /// <returns><c>true</c> when the clears sensitive bytes setting is enabled; otherwise, <c>false</c>.</returns>
    bool GetClearsSensitiveBytes();
    /// <summary>
    /// Determines whether the current value or state is closed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closed; otherwise, <c>false</c>.</returns>
    bool IsClosed();
    /// <summary>
    /// Ensures the capacity.
    /// </summary>
    /// <param name = "requiredCapacity">The required capacity used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    void EnsureCapacity(int requiredCapacity);
    /// <summary>
    /// Appends the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    void Append(ReadOnlySpan<byte> value);
    /// <summary>
    /// Appends the byte.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    void AppendByte(byte value);
    /// <summary>
    /// Gets the byte.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current byte.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    byte GetByte(int index);
    /// <summary>
    /// Copies the current output or content into a new byte array.
    /// </summary>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] ToArray();
    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "destination">The destination that receives the generated data.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    void CopyTo(Span<byte> destination);
    /// <summary>
    /// Discards the prefix.
    /// </summary>
    /// <param name = "byteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void DiscardPrefix(int byteCount);
    /// <summary>
    /// Clears all stored items or state.
    /// </summary>
    void Clear();
    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    void Close();
}
