namespace asn1;

/// <summary>
/// Exposes schema-driven encoding operations through a composed codec implementation.
/// </summary>
public class AsnCodec : IAsnCodec
{
    internal IAsnCodec Implementation;

    /// <summary>
    /// Initializes a codec facade over an operation implementation.
    /// </summary>
    /// <param name="implementation">The implementation that performs codec operations.</param>
    public AsnCodec(IAsnCodec implementation)
    {
        this.Implementation = implementation == null ? throw new System.ArgumentNullException("implementation") : implementation;
    }

    /// <summary>
    /// Encodes one runtime value.
    /// </summary>
    /// <param name="request">The encode request.</param>
    /// <returns>The encode result.</returns>
    public AsnEncodeResult Encode(AsnEncodeRequest request)
    {
        return this.Implementation.Encode(request);
    }

    /// <summary>
    /// Decodes one encoded value.
    /// </summary>
    /// <param name="request">The decode request.</param>
    /// <returns>The decode result.</returns>
    public AsnDecodeResult Decode(AsnDecodeRequest request)
    {
        return this.Implementation.Decode(request);
    }

    /// <summary>
    /// Attempts to decode one value from a read-only byte span.
    /// </summary>
    /// <param name="input">The encoded byte span.</param>
    /// <param name="typeName">The qualified schema type name.</param>
    /// <param name="options">The codec options.</param>
    /// <returns>The decode result.</returns>
    public AsnDecodeResult TryDecodeSpan(System.ReadOnlySpan<byte> input, string typeName, AsnCodecOptions options)
    {
        return this.Implementation.TryDecodeSpan(input, typeName, options);
    }

    /// <summary>
    /// Encodes one value into a caller-provided byte span.
    /// </summary>
    /// <param name="request">The encode request.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <returns>The encode result.</returns>
    public AsnEncodeResult EncodeToSpan(AsnEncodeRequest request, System.Span<byte> destination)
    {
        return this.Implementation.EncodeToSpan(request, destination);
    }

    /// <summary>
    /// Checks whether one encoded value is canonical.
    /// </summary>
    /// <param name="request">The canonicality request.</param>
    /// <returns>The canonicality result.</returns>
    public AsnCanonicalityResult CheckCanonicality(AsnDecodeRequest request)
    {
        return this.Implementation.CheckCanonicality(request);
    }

    /// <summary>
    /// Transcodes one encoded value through its normalized runtime representation.
    /// </summary>
    /// <param name="request">The transcode request.</param>
    /// <returns>The transcode result.</returns>
    public AsnTranscodeResult Transcode(AsnTranscodeRequest request)
    {
        return this.Implementation.Transcode(request);
    }

    /// <summary>
    /// Creates an independent stream-operation adapter for this configured codec.
    /// </summary>
    /// <returns>A streaming codec with operation-local buffering.</returns>
    public IAsnStreamingCodec CreateStreamingCodec()
    {
        return this.Implementation.CreateStreamingCodec();
    }

    /// <summary>
    /// Creates an independent incremental decoder state machine.
    /// </summary>
    /// <param name="typeName">The qualified schema type name.</param>
    /// <param name="options">The decoder options snapshot.</param>
    /// <returns>A new incremental decoder with isolated state.</returns>
    public IAsnIncrementalDecoder CreateIncrementalDecoder(
        string typeName,
        AsnCodecOptions options)
    {
        return this.Implementation.CreateIncrementalDecoder(
            typeName,
            options);
    }
}
