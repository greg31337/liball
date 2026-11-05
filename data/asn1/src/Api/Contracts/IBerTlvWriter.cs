namespace asn1;

/// <summary>
/// Writes schema-less BER, DER, or CER TLV trees and events.
/// </summary>
public interface IBerTlvWriter
{
    /// <summary>
    /// Writes one TLV request.
    /// </summary>
    /// <param name="request">The complete write request.</param>
    /// <returns>The TLV write result.</returns>
    public BerWriteResult Write(BerWriteRequest request);

    /// <summary>
    /// Writes one TLV request directly into a caller-provided byte span.
    /// </summary>
    /// <param name="request">The complete write request.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <returns>The TLV write result.</returns>
    public BerWriteResult WriteSpan(BerWriteRequest request, System.Span<byte> destination);

    /// <summary>
    /// Writes one TLV request synchronously to a stream.
    /// </summary>
    /// <param name="request">The complete write request.</param>
    /// <param name="output">The non-seekable or seekable output stream.</param>
    /// <returns>The TLV write result.</returns>
    public BerWriteResult WriteStream(BerWriteRequest request, System.IO.Stream output);
}
