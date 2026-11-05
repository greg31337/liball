using System;

namespace fix;

/// <summary>Encodes a FIX message into caller-owned storage.</summary>
public interface IMessageEncoder
{
    /// <summary>Encodes one message into the destination span.</summary>
    /// <param name="message">The message to encode.</param>
    /// <param name="dictionary">The dictionary used to order fields and flatten groups.</param>
    /// <param name="destination">The destination storage.</param>
    /// <param name="options">The encode options and resource limits.</param>
    /// <returns>The explicit encode outcome.</returns>
    EncodeResult EncodeMessage(FixMessage message, FixDictionary dictionary, Span<byte> destination, EncodeOptions options);
}
