using System;
namespace token;

/// <summary>Reads sequential bytes while enforcing a fixed input-size limit.</summary>
public interface ITokenBufferReader
{
    /// <summary>Returns the total input length.</summary>
    /// <returns>The number of input bytes.</returns>
    int GetLength();

    /// <summary>Returns the current read position.</summary>
    /// <returns>The zero-based position of the next byte.</returns>
    int GetPosition();

    /// <summary>Returns the number of unread bytes.</summary>
    /// <returns>The number of remaining bytes.</returns>
    int GetRemainingLength();

    /// <summary>Indicates whether the input exceeded the configured limit.</summary>
    /// <returns><c>true</c> when the input is too long; otherwise, <c>false</c>.</returns>
    bool IsLimitExceeded();

    /// <summary>Reads and copies a requested number of bytes.</summary>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>The copied bytes, or stable failure details.</returns>
    TokenBytesResult Read(int count);

    /// <summary>Reads directly into a caller-provided destination span.</summary>
    /// <param name="destination">The destination whose length is the byte count.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult ReadSpan(Span<byte> destination);

    /// <summary>Advances past a requested number of bytes.</summary>
    /// <param name="count">The number of bytes to skip.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult Skip(int count);
}
