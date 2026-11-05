namespace asn1;

/// <summary>
/// Reads schema-less BER TLV events.
/// </summary>
public interface IBerTlvReader
{
    /// <summary>
    /// Reads a TLV tree and events from one request.
    /// </summary>
    /// <param name="request">The complete read request.</param>
    /// <returns>The TLV read result.</returns>
    public BerReadResult Read(BerReadRequest request);

    /// <summary>
    /// Reads a TLV tree and events directly from a read-only byte span.
    /// </summary>
    /// <param name="input">The encoded byte span.</param>
    /// <param name="rules">The BER-family rules.</param>
    /// <returns>The TLV read result.</returns>
    public BerReadResult ReadSpan(System.ReadOnlySpan<byte> input, AsnEncodingRules rules);

    /// <summary>
    /// Reads a TLV tree and events synchronously from a stream.
    /// </summary>
    /// <param name="input">The non-seekable or seekable input stream.</param>
    /// <param name="rules">The BER-family rules.</param>
    /// <returns>The TLV read result.</returns>
    public BerReadResult ReadStream(System.IO.Stream input, AsnEncodingRules rules);
}
