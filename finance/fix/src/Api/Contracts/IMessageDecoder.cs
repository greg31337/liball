using System;

namespace fix;

/// <summary>Decodes a FIX message from caller-owned bytes.</summary>
public interface IMessageDecoder
{
    /// <summary>Decodes one message without retaining the caller's span.</summary>
    /// <param name="source">The source bytes.</param>
    /// <param name="dictionary">The dictionary used to identify fields and groups.</param>
    /// <param name="options">The decode options and resource limits.</param>
    /// <returns>The explicit decode outcome.</returns>
    DecodeResult DecodeMessage(ReadOnlySpan<byte> source, FixDictionary dictionary, DecodeOptions options);
}
