namespace whois;

/// <summary>
/// Defines decoding of raw WHOIS response bytes into a derived text view.
/// </summary>
public interface IWhoisResponseDecoder
{
    /// <summary>
    /// Decodes a raw response using explicit or detected encoding behavior.
    /// </summary>
    /// <param name="response">The raw response whose bytes remain the source of truth.</param>
    /// <param name="options">The response decoding options.</param>
    /// <returns>The decoded response view and status metadata.</returns>
    WhoisTextResponse DecodeResponse(WhoisRawResponse response, WhoisEncodingOptions options);
}
