using System;
namespace token;

/// <summary>Writes bytes to a growing buffer while enforcing a fixed output-size limit.</summary>
public interface ITokenBufferWriter
{
    /// <summary>Returns the number of bytes written.</summary>
    /// <returns>The current output length.</returns>
    int GetLength();

    /// <summary>Returns the remaining capacity.</summary>
    /// <returns>The number of additional bytes that may be written.</returns>
    int GetRemainingCapacity();

    /// <summary>Appends one byte.</summary>
    /// <param name="value">The byte to append.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult Write(byte value);

    /// <summary>Appends a byte array.</summary>
    /// <param name="source">The bytes to append.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult WriteBytes(byte[] source);

    /// <summary>Appends a byte span without an intermediate array.</summary>
    /// <param name="source">The bytes to append.</param>
    /// <returns>A successful result, or stable failure details.</returns>
    TokenResult WriteSpan(ReadOnlySpan<byte> source);

    /// <summary>Returns an independent copy of the written bytes.</summary>
    /// <returns>A new array containing the output.</returns>
    byte[] ToArray();
}
