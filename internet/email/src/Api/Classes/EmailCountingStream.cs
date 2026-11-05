using System;

namespace email;
/// <summary>
/// Represents a counting stream.
/// </summary>
public class EmailCountingStream : IEmailCountingStreamContract
{
    long byteCount;
    byte previousByte;
    byte lastByte;
    bool hasPreviousByte;
    bool hasLastByte;
    /// <summary>
    /// Creates a new counting stream.
    /// </summary>
    public EmailCountingStream()
    {
        byteCount = 0;
        previousByte = 0;
        lastByte = 0;
        hasPreviousByte = false;
        hasLastByte = false;
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "OverflowException">Thrown when the operation encounters an overflow.</exception>
    public void Write(ReadOnlySpan<byte> value)
    {
        if (value.Length > long.MaxValue - byteCount)
        {
            throw new OverflowException("The counted output exceeds Int64.MaxValue.");
        }

        int index = 0;
        while (index < value.Length)
        {
            if (hasLastByte)
            {
                previousByte = lastByte;
                hasPreviousByte = true;
            }

            lastByte = value[index];
            hasLastByte = true;
            index++;
        }

        byteCount += value.Length;
    }

    /// <summary>
    /// Writes the supplied value to the requested destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "offset">The zero-based byte offset.</param>
    /// <param name = "count">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void WriteSegment(byte[] value, int offset, int count)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (offset < 0 || count < 0 || offset > value.Length || count > value.Length - offset)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        Write(new ReadOnlySpan<byte>(value, offset, count));
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
    /// Determines whether the current output ends with CRLF.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool EndsWithCrlf()
    {
        return hasPreviousByte && hasLastByte && previousByte == 13 && lastByte == 10;
    }

    /// <summary>
    /// Determines whether the current output ends with carriage return.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool EndsWithCarriageReturn()
    {
        return hasLastByte && lastByte == 13;
    }
}
