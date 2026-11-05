namespace asn1;

/// <summary>
/// Exposes schema-less BER-family writing through a composed writer implementation.
/// </summary>
public class BerTlvWriter : IBerTlvWriter
{
    internal IBerTlvWriter Implementation;

    /// <summary>
    /// Initializes a TLV-writer facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that writes TLV data.</param>
    public BerTlvWriter(IBerTlvWriter implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Writes one TLV request.
    /// </summary>
    /// <param name="request">The write request.</param>
    /// <returns>The TLV write result.</returns>
    public BerWriteResult Write(BerWriteRequest request)
    {
        return this.Implementation.Write(request);
    }

    /// <summary>
    /// Writes one TLV request into a caller-provided span.
    /// </summary>
    /// <param name="request">The write request.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <returns>The TLV write result.</returns>
    public BerWriteResult WriteSpan(BerWriteRequest request, System.Span<byte> destination)
    {
        return this.Implementation.WriteSpan(request, destination);
    }

    /// <summary>
    /// Writes one TLV request to a stream.
    /// </summary>
    /// <param name="request">The write request.</param>
    /// <param name="output">The output stream.</param>
    /// <returns>The TLV write result.</returns>
    public BerWriteResult WriteStream(BerWriteRequest request, System.IO.Stream output)
    {
        return this.Implementation.WriteStream(request, output);
    }
}
