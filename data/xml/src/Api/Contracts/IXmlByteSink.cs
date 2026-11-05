using System;

namespace xml;

/// <summary>
/// Receives serialized or canonical byte blocks.
/// </summary>
public interface IXmlByteSink
{
    /// <summary>
    /// Receives the next byte block.
    /// </summary>
    /// <param name="bytes">The bytes to receive.</param>
    void WriteXmlBytes(ReadOnlyMemory<byte> bytes);
}

