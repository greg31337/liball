using System;

namespace fix;

/// <summary>Maps an external binary encoding to and from the canonical message model.</summary>
public interface IBinaryMessageCodec
{
    /// <summary>Decodes one binary message without retaining the caller's span.</summary>
    /// <param name="source">The binary source bytes.</param>
    /// <param name="context">The caller-known encoding and mapping metadata. The codec reports effective metadata in the result.</param>
    /// <returns>The explicit binary decode outcome, canonical message, and effective metadata.</returns>
    BinaryDecodeResult DecodeBinary(ReadOnlySpan<byte> source, BinaryMessageContext context);
    /// <summary>Encodes one canonical message into caller-owned storage.</summary>
    /// <param name="message">The canonical message.</param>
    /// <param name="context">The requested encoding and mapping metadata.</param>
    /// <param name="destination">The destination storage.</param>
    /// <returns>The explicit binary encode outcome and effective metadata.</returns>
    BinaryEncodeResult EncodeBinary(FixMessage message, BinaryMessageContext context, Span<byte> destination);
}
