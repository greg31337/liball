using System;
using System.IO;

namespace avif;

/// <summary>Defines the common one-call AVIF operations for substitutable application services.</summary>
/// <remarks>
/// Use this contract when application code needs to replace <see cref="AvifCodec"/> with a test
/// double.
/// </remarks>
public interface IAvifCodec
{
    /// <summary>Probes source bytes for AVIF file-type brands.</summary>
    /// <param name="source">The source bytes, beginning with a FileTypeBox.</param>
    /// <returns>The bounded probe result.</returns>
    AvifProbeResult Probe(ReadOnlySpan<byte> source);

    /// <summary>Inspects a complete AVIF source with caller-selected validation.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    AvifReadResult Read(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options);

    /// <summary>Validates a complete AVIF source with caller-selected options.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    AvifValidationResult Validate(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options);

    /// <summary>Decodes the selected image from a complete AVIF source.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The decoded image result.</returns>
    AvifDecodeResult Decode(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions options);

    /// <summary>Encodes one planar image with caller-selected options.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encoded AVIF result.</returns>
    AvifEncodeResult Encode(
        AvifImage image,
        AvifEncoderOptions options);

    /// <summary>Encodes and writes one image without closing the destination stream.</summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="image">The source image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encode or write outcome.</returns>
    AvifResultCode Write(
        Stream destination,
        AvifImage image,
        AvifEncoderOptions options);

    /// <summary>Probes a byte array for AVIF file-type brands.</summary>
    /// <param name="source">The source bytes, beginning with a FileTypeBox.</param>
    /// <returns>The bounded probe result.</returns>
    AvifProbeResult ProbeBytes(byte[]? source);

    /// <summary>Probes a byte span for AVIF file-type brands.</summary>
    /// <param name="source">The source bytes, beginning with a FileTypeBox.</param>
    /// <returns>The bounded probe result.</returns>
    AvifProbeResult ProbeSpan(ReadOnlySpan<byte> source);

    /// <summary>Probes a readable stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <returns>The bounded probe result.</returns>
    AvifProbeResult ProbeStream(Stream? source);

    /// <summary>Probes a file and closes the file handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>The bounded probe result.</returns>
    AvifProbeResult ProbeFile(string? path);

    /// <summary>Inspects a complete byte array with caller-selected validation.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    AvifReadResult InspectBytes(
        byte[]? source,
        AvifValidationOptions options);

    /// <summary>Inspects a complete byte span with caller-selected validation.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    AvifReadResult InspectSpan(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options);

    /// <summary>Inspects a complete readable stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    AvifReadResult InspectStream(
        Stream? source,
        AvifValidationOptions options);

    /// <summary>Inspects a complete file and closes the file handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    AvifReadResult InspectFile(
        string? path,
        AvifValidationOptions options);

    /// <summary>Validates a complete byte array with caller-selected options.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    AvifValidationResult ValidateBytes(
        byte[]? source,
        AvifValidationOptions options);

    /// <summary>Validates a complete byte span with caller-selected options.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    AvifValidationResult ValidateSpan(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options);

    /// <summary>Validates a complete readable stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    AvifValidationResult ValidateStream(
        Stream? source,
        AvifValidationOptions options);

    /// <summary>Validates a complete file and closes the file handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    AvifValidationResult ValidateFile(
        string? path,
        AvifValidationOptions options);

    /// <summary>Decodes a planar image from a complete byte array.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The planar decode result.</returns>
    AvifDecodeResult DecodePlanarBytes(
        byte[]? source,
        AvifDecoderOptions options);

    /// <summary>Decodes a planar image from a complete byte span.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The planar decode result.</returns>
    AvifDecodeResult DecodePlanarSpan(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions options);

    /// <summary>Decodes a planar image from a readable stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The planar decode result.</returns>
    AvifDecodeResult DecodePlanarStream(
        Stream? source,
        AvifDecoderOptions options);

    /// <summary>Decodes a planar image from a file and closes the file handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The planar decode result.</returns>
    AvifDecodeResult DecodePlanarFile(
        string? path,
        AvifDecoderOptions options);

    /// <summary>Encodes one planar image into independently owned bytes.</summary>
    /// <param name="image">The source planar image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encoded AVIF result.</returns>
    AvifEncodeResult EncodePlanar(
        AvifImage image,
        AvifEncoderOptions options);

    /// <summary>Encodes one planar image to a stream without closing it.</summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="image">The source planar image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encode or write outcome.</returns>
    AvifResultCode WritePlanarStream(
        Stream? destination,
        AvifImage image,
        AvifEncoderOptions options);

    /// <summary>Encodes one planar image to a file and closes the file handle before returning.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="image">The source planar image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encode or write outcome.</returns>
    AvifResultCode WritePlanarFile(
        string? path,
        AvifImage image,
        AvifEncoderOptions options);

    /// <summary>Decodes and re-encodes a complete AVIF byte array.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="decoderOptions">The decoder options.</param>
    /// <param name="encoderOptions">The encoder options.</param>
    /// <returns>The independently owned transcoded bytes.</returns>
    AvifEncodeResult TranscodeBytes(
        byte[]? source,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions);

    /// <summary>Decodes and re-encodes a complete AVIF byte span.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="decoderOptions">The decoder options.</param>
    /// <param name="encoderOptions">The encoder options.</param>
    /// <returns>The independently owned transcoded bytes.</returns>
    AvifEncodeResult TranscodeSpan(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions);

    /// <summary>Decodes and re-encodes a complete stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <param name="decoderOptions">The decoder options.</param>
    /// <param name="encoderOptions">The encoder options.</param>
    /// <returns>The independently owned transcoded bytes.</returns>
    AvifEncodeResult TranscodeStream(
        Stream? source,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions);

    /// <summary>Decodes and re-encodes a file and closes its file handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="decoderOptions">The decoder options.</param>
    /// <param name="encoderOptions">The encoder options.</param>
    /// <returns>The independently owned transcoded bytes.</returns>
    AvifEncodeResult TranscodeFile(
        string? path,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions);

    /// <summary>Decodes a byte array directly into caller-selected packed pixels.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The packed output, rendering, limit, and cancellation settings.</param>
    /// <returns>An owning packed buffer or a buffer carrying a stable error code.</returns>
    AvifPixelBuffer DecodePackedBytes(
        byte[]? source,
        AvifPackedDecodeOptions options);

    /// <summary>Decodes a byte span directly into caller-selected packed pixels.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The packed output, rendering, limit, and cancellation settings.</param>
    /// <returns>An owning packed buffer or a buffer carrying a stable error code.</returns>
    AvifPixelBuffer DecodePackedSpan(
        ReadOnlySpan<byte> source,
        AvifPackedDecodeOptions options);

    /// <summary>Decodes a stream directly into packed pixels without closing it.</summary>
    /// <param name="source">The complete readable source stream.</param>
    /// <param name="options">The packed output, rendering, limit, and cancellation settings.</param>
    /// <returns>An owning packed buffer or a buffer carrying a stable error code.</returns>
    AvifPixelBuffer DecodePackedStream(
        Stream? source,
        AvifPackedDecodeOptions options);

    /// <summary>Decodes a file directly into packed pixels and closes its handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">The packed output, rendering, limit, and cancellation settings.</param>
    /// <returns>An owning packed buffer or a buffer carrying a stable error code.</returns>
    AvifPixelBuffer DecodePackedFile(
        string? path,
        AvifPackedDecodeOptions options);

    /// <summary>Converts packed pixels to planar samples and encodes AVIF bytes.</summary>
    /// <param name="pixels">The source packed pixels.</param>
    /// <param name="options">The color, alpha, encoder, limit, and cancellation settings.</param>
    /// <returns>The independently owned encoded AVIF result.</returns>
    AvifEncodeResult EncodePacked(
        AvifPixelBuffer pixels,
        AvifPackedEncodeOptions options);

    /// <summary>Encodes packed pixels to a stream without closing it.</summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="pixels">The source packed pixels.</param>
    /// <param name="options">The color, alpha, encoder, limit, and cancellation settings.</param>
    /// <returns>The conversion, encode, or write outcome.</returns>
    AvifResultCode WritePackedStream(
        Stream? destination,
        AvifPixelBuffer pixels,
        AvifPackedEncodeOptions options);

    /// <summary>Encodes packed pixels to a file and closes its handle before returning.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="pixels">The source packed pixels.</param>
    /// <param name="options">The color, alpha, encoder, limit, and cancellation settings.</param>
    /// <returns>The conversion, encode, or write outcome.</returns>
    AvifResultCode WritePackedFile(
        string? path,
        AvifPixelBuffer pixels,
        AvifPackedEncodeOptions options);

    /// <summary>Creates a stateful reader over a caller-owned seekable stream.</summary>
    /// <param name="source">The readable seekable stream at its desired initial position.</param>
    /// <param name="options">The decoder options to snapshot.</param>
    /// <returns>The initialized reader or a reader carrying an argument error.</returns>
    AvifReader CreateSeekableStreamReader(
        Stream? source,
        AvifDecoderOptions options);

    /// <summary>Creates a stateful reader over a caller-owned non-seekable stream.</summary>
    /// <param name="source">The readable non-seekable stream.</param>
    /// <param name="options">The decoder options to snapshot.</param>
    /// <returns>The initialized reader or a reader carrying an argument error.</returns>
    AvifReader CreateNonSeekableStreamReader(
        Stream? source,
        AvifDecoderOptions options);

    /// <summary>Creates a stateful reader over a caller-owned exact-range source.</summary>
    /// <param name="source">The random-access source.</param>
    /// <param name="options">The decoder options to snapshot.</param>
    /// <returns>The initialized range reader or a reader carrying an argument error.</returns>
    AvifReader CreateRangeReader(
        IAvifRandomAccessSource? source,
        AvifDecoderOptions options);
}
