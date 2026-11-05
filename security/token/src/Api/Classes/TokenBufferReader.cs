using System;
namespace token;

/// <summary>
/// Reads bytes sequentially while enforcing a maximum input length.
/// </summary>
public class TokenBufferReader : ITokenBufferReader
{
    internal byte[] buffer;
    internal int maximumLength;
    internal int position;
    internal bool limitExceeded;

    /// <summary>
    /// Initializes a reader for a byte array with a maximum accepted length.
    /// </summary>
    /// <param name="buffer">The byte array to read.</param>
    /// <param name="maximumLength">The maximum accepted input length.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>buffer</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumLength</c> is negative.</exception>
    public TokenBufferReader(byte[] buffer, int maximumLength)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        if (maximumLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLength");
        }

        this.buffer = buffer;
        this.maximumLength = maximumLength;
        position = 0;
        limitExceeded = buffer.Length > maximumLength;
    }

    /// <summary>
    /// Returns the total input length.
    /// </summary>
    /// <returns>The number of bytes in the input.</returns>
    public int GetLength()
    {
        return buffer.Length;
    }

    /// <summary>
    /// Returns the current read position.
    /// </summary>
    /// <returns>The zero-based position of the next byte.</returns>
    public int GetPosition()
    {
        return position;
    }

    /// <summary>
    /// Returns the number of unread bytes.
    /// </summary>
    /// <returns>The number of bytes between the current position and the end of the input.</returns>
    public int GetRemainingLength()
    {
        return buffer.Length - position;
    }

    /// <summary>
    /// Indicates whether the input exceeds the configured maximum length.
    /// </summary>
    /// <returns><c>true</c> when the input is too long; otherwise, <c>false</c>.</returns>
    public bool IsLimitExceeded()
    {
        return limitExceeded;
    }

    /// <summary>
    /// Reads and copies a requested number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>A result containing the copied bytes, or failure details when the read cannot be completed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative.</exception>
    public TokenBytesResult Read(int count)
    {
        TokenBytesResult result = new TokenBytesResult();

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        if (limitExceeded)
        {
            result.Error = new TokenError(
                "buffer_limit_exceeded",
                "The input exceeds the configured maximum length.",
                maximumLength);
            return result;
        }

        if (count > buffer.Length - position)
        {
            result.Error = new TokenError(
                "unexpected_end_of_buffer",
                "The input does not contain enough bytes.",
                position);
            return result;
        }

        result.Bytes = buffer.AsSpan(position, count).ToArray();
        position += count;
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Reads bytes directly into a caller-provided destination span.
    /// </summary>
    /// <param name="destination">The destination whose length is the requested byte count.</param>
    /// <returns>A successful result, or failure details when the read cannot be completed.</returns>
    public TokenResult ReadSpan(Span<byte> destination)
    {
        TokenResult result = new TokenResult();

        if (limitExceeded)
        {
            result.Error = new TokenError(
                "buffer_limit_exceeded",
                "The input exceeds the configured maximum length.",
                maximumLength);
            return result;
        }

        if (destination.Length > buffer.Length - position)
        {
            result.Error = new TokenError(
                "unexpected_end_of_buffer",
                "The input does not contain enough bytes.",
                position);
            return result;
        }

        buffer.AsSpan(position, destination.Length).CopyTo(destination);
        position += destination.Length;
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Advances past a requested number of bytes without copying them.
    /// </summary>
    /// <param name="count">The number of bytes to skip.</param>
    /// <returns>A successful result, or failure details when the skip cannot be completed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> is negative.</exception>
    public TokenResult Skip(int count)
    {
        TokenResult result = new TokenResult();

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        if (limitExceeded)
        {
            result.Error = new TokenError(
                "buffer_limit_exceeded",
                "The input exceeds the configured maximum length.",
                maximumLength);
            return result;
        }

        if (count > buffer.Length - position)
        {
            result.Error = new TokenError(
                "unexpected_end_of_buffer",
                "The input does not contain enough bytes.",
                position);
            return result;
        }

        position += count;
        result.Success = true;
        return result;
    }
}
