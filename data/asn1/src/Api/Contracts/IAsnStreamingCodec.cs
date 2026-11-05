namespace asn1;

/// <summary>
/// Encodes and decodes values through synchronous streams.
/// </summary>
public interface IAsnStreamingCodec
{
    /// <summary>
    /// Encodes one request to a stream without requiring seek support.
    /// </summary>
    /// <param name="request">The encode request.</param>
    /// <param name="output">The output stream.</param>
    /// <returns>The encode result and exact progress.</returns>
    public AsnEncodeResult EncodeStream(AsnEncodeRequest request, System.IO.Stream output);

    /// <summary>
    /// Decodes one value from a stream without requiring seek support.
    /// </summary>
    /// <param name="input">The input stream.</param>
    /// <param name="typeName">The qualified schema type name.</param>
    /// <param name="options">The codec options.</param>
    /// <returns>The decode result and exact progress.</returns>
    public AsnDecodeResult DecodeStream(System.IO.Stream input, string typeName, AsnCodecOptions options);
}
