namespace asn1;

/// <summary>
/// Encodes and decodes values and checks canonical encodings under selected encoding rules.
/// </summary>
public interface IAsnCodec
{
    /// <summary>
    /// Encodes one runtime value.
    /// </summary>
    /// <param name="request">The complete encode request.</param>
    /// <returns>The encode result.</returns>
    public AsnEncodeResult Encode(AsnEncodeRequest request);

    /// <summary>
    /// Decodes one encoded value.
    /// </summary>
    /// <param name="request">The complete decode request.</param>
    /// <returns>The decode result.</returns>
    public AsnDecodeResult Decode(AsnDecodeRequest request);

    /// <summary>
    /// Attempts to decode one value directly from a read-only byte span.
    /// </summary>
    /// <param name="input">The encoded byte span.</param>
    /// <param name="typeName">The qualified schema type name.</param>
    /// <param name="options">The codec options.</param>
    /// <returns>A Done, NeedMoreData, or Error decode result.</returns>
    public AsnDecodeResult TryDecodeSpan(System.ReadOnlySpan<byte> input, string typeName, AsnCodecOptions options);

    /// <summary>
    /// Encodes one value directly into a caller-provided byte span.
    /// </summary>
    /// <param name="request">The encode request.</param>
    /// <param name="destination">The destination byte span.</param>
    /// <returns>The encode result including the exact written count.</returns>
    public AsnEncodeResult EncodeToSpan(AsnEncodeRequest request, System.Span<byte> destination);

    /// <summary>
    /// Checks whether one encoded value is canonical under the requested rules.
    /// </summary>
    /// <param name="request">The complete canonicality request.</param>
    /// <returns>The canonicality result.</returns>
    public AsnCanonicalityResult CheckCanonicality(AsnDecodeRequest request);

    /// <summary>
    /// Transcodes one encoded value through its normalized runtime representation.
    /// </summary>
    /// <param name="request">The complete transcode request.</param>
    /// <returns>The transcode result.</returns>
    public AsnTranscodeResult Transcode(AsnTranscodeRequest request);

    /// <summary>
    /// Creates an independent stream-operation adapter for this configured codec.
    /// </summary>
    /// <returns>A streaming codec with operation-local buffering.</returns>
    public IAsnStreamingCodec CreateStreamingCodec();

    /// <summary>
    /// Creates an independent incremental decoder state machine.
    /// </summary>
    /// <param name="typeName">The qualified schema type name.</param>
    /// <param name="options">The decoder options snapshot.</param>
    /// <returns>A new incremental decoder with isolated state.</returns>
    public IAsnIncrementalDecoder CreateIncrementalDecoder(
        string typeName,
        AsnCodecOptions options);

}
