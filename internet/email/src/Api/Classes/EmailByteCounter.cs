using System;

namespace email;
/// <summary>
/// Represents a byte counter.
/// </summary>
public class EmailByteCounter : IEmailByteCounterContract
{
    long byteCount;
    /// <summary>
    /// Creates a byte counter with an initial value.
    /// </summary>
    public EmailByteCounter()
    {
        byteCount = 0;
    }

    internal static EmailByteCounter CreateImplementationWith()
    {
        return new EmailByteCounter();
    }

    internal static EmailByteCounter CreateImplementationWithInitialByteCount(long initialByteCount)
    {
        if (initialByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("initialByteCount");
        }

        EmailByteCounter value = new EmailByteCounter();
        value.byteCount = initialByteCount;
        return value;
    }

    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    public long GetByteCount()
    {
        return byteCount;
    }

    /// <summary>
    /// Determines whether the instance can add.
    /// </summary>
    /// <param name = "additionalByteCount">The number of items or bytes to process.</param>
    /// <returns><c>true</c> when the requested operation is supported; otherwise, <c>false</c>.</returns>
    public bool CanAdd(long additionalByteCount)
    {
        if (additionalByteCount < 0)
        {
            return false;
        }

        return additionalByteCount <= long.MaxValue - byteCount;
    }

    /// <summary>
    /// Adds a value to the current collection or model.
    /// </summary>
    /// <param name = "additionalByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    public void Add(long additionalByteCount)
    {
        if (additionalByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("additionalByteCount");
        }

        if (additionalByteCount > long.MaxValue - byteCount)
        {
            throw new OverflowException("The byte count exceeds Int64.MaxValue.");
        }

        byteCount += additionalByteCount;
    }

    /// <summary>
    /// Adds a bytes to the current collection or model.
    /// </summary>
    /// <param name = "bytes">The source bytes or data to process.</param>
    public void AddBytes(ReadOnlySpan<byte> bytes)
    {
        Add(bytes.Length);
    }

    /// <summary>
    /// Resets the supplied value.
    /// </summary>
    public void Reset()
    {
        byteCount = 0;
    }
}
