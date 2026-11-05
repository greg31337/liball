using System;
using System.IO;

namespace avif;

/// <summary>Provides bounded one-call AVIF probe, inspection, validation, decode, and encode operations.</summary>
internal static class Avif
{
    /// <summary>Probes a byte array for AVIF file-type brands.</summary>
    /// <param name="source">The source bytes, beginning with a FileTypeBox.</param>
    /// <returns>The bounded probe result.</returns>
    internal static AvifProbeResult Probe(byte[]? source)
    {
        if (source == null)
        {
            return AvifProbeResult.Create(AvifResultCode.InvalidArgument, null);
        }
        return Probe(source.AsSpan());
    }

    /// <summary>Probes a byte span for AVIF file-type brands.</summary>
    /// <param name="source">The source bytes, beginning with a FileTypeBox.</param>
    /// <returns>The bounded probe result.</returns>
    internal static AvifProbeResult Probe(ReadOnlySpan<byte> source)
    {
        FileTypeProbeResult probe = FileTypeProbe.Probe(
            source,
            new AvifSecurityLimits());
        AvifResultCode code = probe.Code;
        if (code == AvifResultCode.Success && !probe.Capabilities.IsAvif)
        {
            code = AvifResultCode.NotFound;
        }
        return AvifProbeResult.Create(code, probe.Capabilities);
    }

    /// <summary>Inspects a complete AVIF byte array using strict defaults.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <returns>The parsed document result.</returns>
    internal static AvifReadResult Read(byte[]? source)
    {
        return Read(source, new AvifValidationOptions());
    }

    /// <summary>Inspects a complete AVIF byte array with caller-selected validation.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    internal static AvifReadResult Read(byte[]? source, AvifValidationOptions options)
    {
        AvifDocument document = AvifDocument.Parse(source, options);
        return new AvifReadResult(document.GetCode(), document);
    }

    /// <summary>Inspects a complete AVIF byte span using strict defaults.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <returns>The parsed document result.</returns>
    internal static AvifReadResult Read(ReadOnlySpan<byte> source)
    {
        return Read(source, new AvifValidationOptions());
    }

    /// <summary>Inspects a complete AVIF byte span with caller-selected validation.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    internal static AvifReadResult Read(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        AvifDocument document = AvifDocument.Parse(source, options);
        return new AvifReadResult(document.GetCode(), document);
    }

    /// <summary>Reads and inspects a complete stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <returns>The parsed document result.</returns>
    internal static AvifReadResult Read(Stream source)
    {
        AvifValidationOptions options = new AvifValidationOptions();
        AvifOneCallStreamResult read = AvifOneCallCodec.ReadStream(
            source,
            options.Limits.MaxSourceBytes,
            null);
        if (read.Code != AvifResultCode.Success)
        {
            return new AvifReadResult(read.Code, null);
        }
        return Read(read.Data, options);
    }

    /// <summary>Inspects a complete AVIF byte array using strict defaults.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <returns>The parsed document result.</returns>
    internal static AvifReadResult Inspect(byte[] source)
    {
        return Read(source);
    }

    /// <summary>Inspects a complete AVIF byte span using strict defaults.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <returns>The parsed document result.</returns>
    internal static AvifReadResult Inspect(ReadOnlySpan<byte> source)
    {
        return Read(source);
    }

    /// <summary>Reads and inspects a complete stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <returns>The parsed document result.</returns>
    internal static AvifReadResult Inspect(Stream source)
    {
        return Read(source);
    }

    /// <summary>Validates a complete AVIF byte array using strict defaults.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <returns>The validation result and findings.</returns>
    internal static AvifValidationResult Validate(byte[] source)
    {
        return Validate(source, new AvifValidationOptions());
    }

    /// <summary>Validates a complete AVIF byte array with caller-selected options.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    internal static AvifValidationResult Validate(
        byte[] source,
        AvifValidationOptions options)
    {
        AvifDocument document = AvifDocument.Parse(source, options);
        return new AvifValidationResult(document.GetCode(), document);
    }

    /// <summary>Validates a complete AVIF byte span using strict defaults.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <returns>The validation result and findings.</returns>
    internal static AvifValidationResult Validate(ReadOnlySpan<byte> source)
    {
        return Validate(source, new AvifValidationOptions());
    }

    /// <summary>Validates a complete AVIF byte span with caller-selected options.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    internal static AvifValidationResult Validate(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        AvifDocument document = AvifDocument.Parse(source, options);
        return new AvifValidationResult(document.GetCode(), document);
    }

    /// <summary>Decodes the primary image from a complete byte array using strict defaults.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <returns>The decoded image result.</returns>
    internal static AvifDecodeResult Decode(byte[] source)
    {
        return Decode(source, new AvifDecoderOptions());
    }

    /// <summary>Decodes the selected image from a complete byte array.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The decoded image result.</returns>
    internal static AvifDecodeResult Decode(byte[] source, AvifDecoderOptions options)
    {
        if (source == null)
        {
            return new AvifDecodeResult(
                AvifResultCode.InvalidArgument,
                null,
                AvifDocument.Parse(source));
        }
        return AvifOneCallCodec.Decode(source, options);
    }

    /// <summary>Decodes the primary image from a complete byte span using strict defaults.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <returns>The decoded image result.</returns>
    internal static AvifDecodeResult Decode(ReadOnlySpan<byte> source)
    {
        return Decode(source, new AvifDecoderOptions());
    }

    /// <summary>Decodes the selected image from a complete byte span.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The decoded image result.</returns>
    internal static AvifDecodeResult Decode(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions options)
    {
        return AvifOneCallCodec.Decode(source, options);
    }

    /// <summary>Decodes the primary image from a complete stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <returns>The decoded image result.</returns>
    internal static AvifDecodeResult Decode(Stream source)
    {
        return Decode(source, new AvifDecoderOptions());
    }

    /// <summary>Decodes the selected image from a complete stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The decoded image result.</returns>
    internal static AvifDecodeResult Decode(Stream source, AvifDecoderOptions options)
    {
        if (options == null || options.Limits == null)
        {
            return new AvifDecodeResult(AvifResultCode.InvalidArgument, null, null);
        }
        AvifOneCallStreamResult read = AvifOneCallCodec.ReadStream(
            source,
            options.Limits.MaxSourceBytes,
            options.Cancellation);
        if (read.Code != AvifResultCode.Success)
        {
            return new AvifDecodeResult(read.Code, null, null);
        }
        return Decode(read.Data, options);
    }

    /// <summary>Encodes one planar image using deterministic defaults.</summary>
    /// <param name="image">The source image.</param>
    /// <returns>The encoded AVIF result.</returns>
    internal static AvifEncodeResult Encode(AvifImage image)
    {
        return Encode(image, new AvifEncoderOptions());
    }

    /// <summary>Encodes one planar image with caller-selected options.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encoded AVIF result.</returns>
    internal static AvifEncodeResult Encode(
        AvifImage? image,
        AvifEncoderOptions options)
    {
        return AvifOneCallCodec.Encode(image, options);
    }

    /// <summary>Encodes and writes one image without closing the destination stream.</summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="image">The source image.</param>
    /// <returns>The write outcome.</returns>
    internal static AvifResultCode Write(Stream destination, AvifImage image)
    {
        return Write(destination, image, new AvifEncoderOptions());
    }

    /// <summary>Encodes and writes one image without closing the destination stream.</summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="image">The source image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encode or write outcome.</returns>
    internal static AvifResultCode Write(
        Stream destination,
        AvifImage image,
        AvifEncoderOptions options)
    {
        if (destination == null || !destination.CanWrite)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifEncodeResult encoded = Encode(image, options);
        if (encoded.GetCode() != AvifResultCode.Success)
        {
            return encoded.GetCode();
        }
        StreamOutput output = new StreamOutput(destination, true);
        AvifResultCode code = output.Write(encoded.GetData());
        if (code == AvifResultCode.Success)
        {
            code = output.Flush();
        }
        output.Close();
        return code;
    }
}
