namespace asn1;

/// <summary>
/// Exposes stream codec operations through a composed streaming implementation.
/// </summary>
public class AsnStreamingCodec : IAsnStreamingCodec
{
    internal IAsnStreamingCodec Implementation;

    /// <summary>
    /// Initializes a streaming-codec facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that performs stream codec operations.</param>
    public AsnStreamingCodec(IAsnStreamingCodec implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Encodes one request to a stream.
    /// </summary>
    /// <param name="request">The encode request.</param>
    /// <param name="output">The output stream.</param>
    /// <returns>The encode result.</returns>
    public AsnEncodeResult EncodeStream(AsnEncodeRequest request, System.IO.Stream output)
    {
        return this.Implementation.EncodeStream(request, output);
    }

    /// <summary>
    /// Decodes one value from a stream.
    /// </summary>
    /// <param name="input">The input stream.</param>
    /// <param name="typeName">The qualified schema type name.</param>
    /// <param name="options">The codec options.</param>
    /// <returns>The decode result.</returns>
    public AsnDecodeResult DecodeStream(System.IO.Stream input, string typeName, AsnCodecOptions options)
    {
        return this.Implementation.DecodeStream(input, typeName, options);
    }
}
