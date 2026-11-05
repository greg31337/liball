using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailByteCounter"/>.
/// </summary>
public interface IEmailByteCounterContract
{
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    long GetByteCount();
    /// <summary>
    /// Determines whether the instance can add.
    /// </summary>
    /// <param name = "additionalByteCount">The number of items or bytes to process.</param>
    /// <returns><c>true</c> when the requested operation is supported; otherwise, <c>false</c>.</returns>
    bool CanAdd(long additionalByteCount);
    /// <summary>
    /// Adds a value to the current collection or model.
    /// </summary>
    /// <param name = "additionalByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    void Add(long additionalByteCount);
    /// <summary>
    /// Adds a bytes to the current collection or model.
    /// </summary>
    /// <param name = "bytes">The source bytes or data to process.</param>
    void AddBytes(ReadOnlySpan<byte> bytes);
    /// <summary>
    /// Resets the supplied value.
    /// </summary>
    void Reset();
}
