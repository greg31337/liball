namespace asn1;

/// <summary>
/// Exposes schema-less BER-family reading through a composed reader implementation.
/// </summary>
public class BerTlvReader : IBerTlvReader
{
    internal IBerTlvReader Implementation;

    /// <summary>
    /// Initializes a TLV-reader facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that reads TLV data.</param>
    public BerTlvReader(IBerTlvReader implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Reads a TLV tree and events from one request.
    /// </summary>
    /// <param name="request">The read request.</param>
    /// <returns>The TLV read result.</returns>
    public BerReadResult Read(BerReadRequest request)
    {
        return this.Implementation.Read(request);
    }

    /// <summary>
    /// Reads TLV data from a read-only byte span.
    /// </summary>
    /// <param name="input">The encoded byte span.</param>
    /// <param name="rules">The BER-family rules.</param>
    /// <returns>The TLV read result.</returns>
    public BerReadResult ReadSpan(System.ReadOnlySpan<byte> input, AsnEncodingRules rules)
    {
        return this.Implementation.ReadSpan(input, rules);
    }

    /// <summary>
    /// Reads TLV data from a stream.
    /// </summary>
    /// <param name="input">The input stream.</param>
    /// <param name="rules">The BER-family rules.</param>
    /// <returns>The TLV read result.</returns>
    public BerReadResult ReadStream(System.IO.Stream input, AsnEncodingRules rules)
    {
        return this.Implementation.ReadStream(input, rules);
    }
}
