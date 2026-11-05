using System;
namespace token;

/// <summary>
/// Writes bytes to a growing buffer while enforcing a maximum output length.
/// </summary>
public class TokenBufferWriter : ITokenBufferWriter
{
    internal byte[] buffer;
    internal int maximumLength;
    internal int length;

    /// <summary>
    /// Initializes an empty writer with a maximum output length.
    /// </summary>
    /// <param name="maximumLength">The maximum number of bytes that may be written.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumLength</c> is negative.</exception>
    public TokenBufferWriter(int maximumLength)
    {
        if (maximumLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLength");
        }

        this.maximumLength = maximumLength;
        buffer = Array.Empty<byte>();
        length = 0;
    }

    /// <summary>
    /// Returns the number of bytes written.
    /// </summary>
    /// <returns>The current output length.</returns>
    public int GetLength()
    {
        return length;
    }

    /// <summary>
    /// Returns the number of bytes that may still be written.
    /// </summary>
    /// <returns>The remaining capacity under the configured limit.</returns>
    public int GetRemainingCapacity()
    {
        return maximumLength - length;
    }

    /// <summary>
    /// Appends one byte to the output.
    /// </summary>
    /// <param name="value">The byte to append.</param>
    /// <returns>A successful result, or failure details when the configured limit would be exceeded.</returns>
    public TokenResult Write(byte value)
    {
        TokenResult result = new TokenResult();

        if (length == maximumLength)
        {
            result.Error = CreateLimitError();
            return result;
        }

        EnsureCapacity(length + 1);
        buffer[length] = value;
        length += 1;
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Appends a byte array to the output.
    /// </summary>
    /// <param name="source">The bytes to append.</param>
    /// <returns>A successful result, or failure details when the configured limit would be exceeded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>source</c> is null.</exception>
    public TokenResult WriteBytes(byte[] source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        return WriteSpan(source.AsSpan());
    }

    /// <summary>
    /// Appends a byte span to the output.
    /// </summary>
    /// <param name="source">The bytes to append.</param>
    /// <returns>A successful result, or failure details when the configured limit would be exceeded.</returns>
    public TokenResult WriteSpan(ReadOnlySpan<byte> source)
    {
        TokenResult result = new TokenResult();

        if (source.Length > maximumLength - length)
        {
            result.Error = CreateLimitError();
            return result;
        }

        EnsureCapacity(length + source.Length);
        source.CopyTo(buffer.AsSpan(length));
        length += source.Length;
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Copies the written bytes into a new array.
    /// </summary>
    /// <returns>A new array containing all bytes written so far.</returns>
    public byte[] ToArray()
    {
        return buffer.AsSpan(0, length).ToArray();
    }

    internal TokenError CreateLimitError()
    {
        return new TokenError(
            "buffer_limit_exceeded",
            "The output would exceed the configured maximum length.",
            length);
    }

    internal void EnsureCapacity(int requiredLength)
    {
        int newLength;
        byte[] expanded;

        if (requiredLength <= buffer.Length)
        {
            return;
        }

        newLength = buffer.Length == 0 ? 16 : buffer.Length * 2;
        if (newLength < requiredLength)
        {
            newLength = requiredLength;
        }

        if (newLength > maximumLength || newLength < 0)
        {
            newLength = maximumLength;
        }

        expanded = new byte[newLength];
        buffer.AsSpan(0, length).CopyTo(expanded);
        buffer = expanded;
    }
}
