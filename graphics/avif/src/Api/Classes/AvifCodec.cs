using System;
using System.IO;

namespace avif;

/// <summary>Provides the public one-call AVIF probe, inspection, validation, decode, and encode API.</summary>
public class AvifCodec : IAvifCodec
{
    /// <summary>Creates a stateless one-call codec facade.</summary>
    public AvifCodec()
    {
    }

    /// <summary>Probes source bytes for AVIF file-type brands.</summary>
    /// <param name="source">The source bytes, beginning with a FileTypeBox.</param>
    /// <returns>The bounded probe result.</returns>
    public AvifProbeResult Probe(ReadOnlySpan<byte> source)
    {
        return Avif.Probe(source);
    }

    /// <summary>Inspects a complete AVIF source with caller-selected validation.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    public AvifReadResult Read(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        return Avif.Read(source, options);
    }

    /// <summary>Validates a complete AVIF source with caller-selected options.</summary>
    /// <param name="source">The complete source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    public AvifValidationResult Validate(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        return Avif.Validate(source, options);
    }

    /// <summary>Decodes the selected image from a complete AVIF source.</summary>
    /// <param name="source">The complete AVIF source.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The decoded image result.</returns>
    public AvifDecodeResult Decode(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions options)
    {
        return Avif.Decode(source, options);
    }

    /// <summary>Encodes one planar image with caller-selected options.</summary>
    /// <param name="image">The source image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encoded AVIF result.</returns>
    public AvifEncodeResult Encode(
        AvifImage image,
        AvifEncoderOptions options)
    {
        return Avif.Encode(image, options);
    }

    /// <summary>Encodes and writes one image without closing the destination stream.</summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="image">The source image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encode or write outcome.</returns>
    public AvifResultCode Write(
        Stream destination,
        AvifImage image,
        AvifEncoderOptions options)
    {
        return Avif.Write(destination, image, options);
    }

    /// <summary>Probes a byte array for AVIF file-type brands.</summary>
    /// <param name="source">The source bytes, beginning with a FileTypeBox.</param>
    /// <returns>The bounded probe result.</returns>
    public AvifProbeResult ProbeBytes(byte[]? source)
    {
        return Avif.Probe(source);
    }

    /// <summary>Probes a byte span for AVIF file-type brands.</summary>
    /// <param name="source">The source bytes, beginning with a FileTypeBox.</param>
    /// <returns>The bounded probe result.</returns>
    public AvifProbeResult ProbeSpan(ReadOnlySpan<byte> source)
    {
        return Avif.Probe(source);
    }

    /// <summary>Probes a readable stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <returns>The bounded probe result.</returns>
    public AvifProbeResult ProbeStream(Stream? source)
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        AvifOneCallStreamResult read = AvifOneCallCodec.ReadStream(
            source,
            limits.MaxSourceBytes,
            null);
        if (read.Code != AvifResultCode.Success)
        {
            return new AvifProbeResult(read.Code);
        }
        return Avif.Probe(read.Data);
    }

    /// <summary>Probes a file and closes the file handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>The bounded probe result.</returns>
    public AvifProbeResult ProbeFile(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new AvifProbeResult(AvifResultCode.InvalidArgument);
        }
        if (!File.Exists(path))
        {
            return new AvifProbeResult(AvifResultCode.IoError);
        }
        using (FileStream source = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read))
        {
            return ProbeStream(source);
        }
    }

    /// <summary>Inspects a complete byte array with caller-selected validation.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    public AvifReadResult InspectBytes(
        byte[]? source,
        AvifValidationOptions options)
    {
        return Avif.Read(source, options);
    }

    /// <summary>Inspects a complete byte span with caller-selected validation.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    public AvifReadResult InspectSpan(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        return Avif.Read(source, options);
    }

    /// <summary>Inspects a complete readable stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    public AvifReadResult InspectStream(
        Stream? source,
        AvifValidationOptions options)
    {
        if (options == null || options.Limits == null)
        {
            return new AvifReadResult(AvifResultCode.InvalidArgument, null);
        }
        AvifOneCallStreamResult read = AvifOneCallCodec.ReadStream(
            source,
            options.Limits.MaxSourceBytes,
            null);
        if (read.Code != AvifResultCode.Success)
        {
            return new AvifReadResult(read.Code, null);
        }
        return Avif.Read(read.Data, options);
    }

    /// <summary>Inspects a complete file and closes the file handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The parsed document result.</returns>
    public AvifReadResult InspectFile(
        string? path,
        AvifValidationOptions options)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new AvifReadResult(AvifResultCode.InvalidArgument, null);
        }
        if (!File.Exists(path))
        {
            return new AvifReadResult(AvifResultCode.IoError, null);
        }
        using (FileStream source = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read))
        {
            return InspectStream(source, options);
        }
    }

    /// <summary>Validates a complete byte array with caller-selected options.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    public AvifValidationResult ValidateBytes(
        byte[]? source,
        AvifValidationOptions options)
    {
        if (source == null)
        {
            return new AvifValidationResult(
                AvifResultCode.InvalidArgument,
                null);
        }
        return Avif.Validate(source, options);
    }

    /// <summary>Validates a complete byte span with caller-selected options.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    public AvifValidationResult ValidateSpan(
        ReadOnlySpan<byte> source,
        AvifValidationOptions options)
    {
        return Avif.Validate(source, options);
    }

    /// <summary>Validates a complete readable stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    public AvifValidationResult ValidateStream(
        Stream? source,
        AvifValidationOptions options)
    {
        if (options == null || options.Limits == null)
        {
            return new AvifValidationResult(
                AvifResultCode.InvalidArgument,
                null);
        }
        AvifOneCallStreamResult read = AvifOneCallCodec.ReadStream(
            source,
            options.Limits.MaxSourceBytes,
            null);
        if (read.Code != AvifResultCode.Success)
        {
            return new AvifValidationResult(read.Code, null);
        }
        return Avif.Validate(read.Data, options);
    }

    /// <summary>Validates a complete file and closes the file handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">The validation options.</param>
    /// <returns>The validation result and findings.</returns>
    public AvifValidationResult ValidateFile(
        string? path,
        AvifValidationOptions options)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new AvifValidationResult(
                AvifResultCode.InvalidArgument,
                null);
        }
        if (!File.Exists(path))
        {
            return new AvifValidationResult(AvifResultCode.IoError, null);
        }
        using (FileStream source = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read))
        {
            return ValidateStream(source, options);
        }
    }

    /// <summary>Decodes a planar image from a complete byte array.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The planar decode result.</returns>
    public AvifDecodeResult DecodePlanarBytes(
        byte[]? source,
        AvifDecoderOptions options)
    {
        if (source == null)
        {
            return new AvifDecodeResult(
                AvifResultCode.InvalidArgument,
                null,
                null);
        }
        return Avif.Decode(source, options);
    }

    /// <summary>Decodes a planar image from a complete byte span.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The planar decode result.</returns>
    public AvifDecodeResult DecodePlanarSpan(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions options)
    {
        return Avif.Decode(source, options);
    }

    /// <summary>Decodes a planar image from a readable stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The planar decode result.</returns>
    public AvifDecodeResult DecodePlanarStream(
        Stream? source,
        AvifDecoderOptions options)
    {
        if (source == null)
        {
            return new AvifDecodeResult(
                AvifResultCode.InvalidArgument,
                null,
                null);
        }
        return Avif.Decode(source, options);
    }

    /// <summary>Decodes a planar image from a file and closes the file handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">The decoder options.</param>
    /// <returns>The planar decode result.</returns>
    public AvifDecodeResult DecodePlanarFile(
        string? path,
        AvifDecoderOptions options)
    {
        if (string.IsNullOrEmpty(path))
        {
            return new AvifDecodeResult(
                AvifResultCode.InvalidArgument,
                null,
                null);
        }
        if (!File.Exists(path))
        {
            return new AvifDecodeResult(AvifResultCode.IoError, null, null);
        }
        using (FileStream source = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read))
        {
            return DecodePlanarStream(source, options);
        }
    }

    /// <summary>Encodes one planar image into independently owned bytes.</summary>
    /// <param name="image">The source planar image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encoded AVIF result.</returns>
    public AvifEncodeResult EncodePlanar(
        AvifImage image,
        AvifEncoderOptions options)
    {
        return Avif.Encode(image, options);
    }

    /// <summary>Encodes one planar image to a stream without closing it.</summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="image">The source planar image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encode or write outcome.</returns>
    public AvifResultCode WritePlanarStream(
        Stream? destination,
        AvifImage image,
        AvifEncoderOptions options)
    {
        if (destination == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        return Avif.Write(destination, image, options);
    }

    /// <summary>Encodes one planar image to a file and closes the file handle before returning.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="image">The source planar image.</param>
    /// <param name="options">The encoder options.</param>
    /// <returns>The encode or write outcome.</returns>
    public AvifResultCode WritePlanarFile(
        string? path,
        AvifImage image,
        AvifEncoderOptions options)
    {
        if (string.IsNullOrEmpty(path))
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifEncodeResult encoded = Avif.Encode(image, options);
        if (encoded.GetCode() != AvifResultCode.Success)
        {
            return encoded.GetCode();
        }
        using (FileStream destination = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None))
        {
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

    /// <summary>Decodes and re-encodes a complete AVIF byte array.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="decoderOptions">The decoder options.</param>
    /// <param name="encoderOptions">The encoder options.</param>
    /// <returns>The independently owned transcoded bytes.</returns>
    public AvifEncodeResult TranscodeBytes(
        byte[]? source,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions)
    {
        AvifDecodeResult decoded = DecodePlanarBytes(source, decoderOptions);
        return EncodeDecoded(decoded, encoderOptions);
    }

    /// <summary>Decodes and re-encodes a complete AVIF byte span.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="decoderOptions">The decoder options.</param>
    /// <param name="encoderOptions">The encoder options.</param>
    /// <returns>The independently owned transcoded bytes.</returns>
    public AvifEncodeResult TranscodeSpan(
        ReadOnlySpan<byte> source,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions)
    {
        AvifDecodeResult decoded = Avif.Decode(source, decoderOptions);
        return EncodeDecoded(decoded, encoderOptions);
    }

    /// <summary>Decodes and re-encodes a complete stream without closing it.</summary>
    /// <param name="source">The readable source stream.</param>
    /// <param name="decoderOptions">The decoder options.</param>
    /// <param name="encoderOptions">The encoder options.</param>
    /// <returns>The independently owned transcoded bytes.</returns>
    public AvifEncodeResult TranscodeStream(
        Stream? source,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions)
    {
        AvifDecodeResult decoded = DecodePlanarStream(source, decoderOptions);
        return EncodeDecoded(decoded, encoderOptions);
    }

    /// <summary>Decodes and re-encodes a file and closes its file handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="decoderOptions">The decoder options.</param>
    /// <param name="encoderOptions">The encoder options.</param>
    /// <returns>The independently owned transcoded bytes.</returns>
    public AvifEncodeResult TranscodeFile(
        string? path,
        AvifDecoderOptions decoderOptions,
        AvifEncoderOptions encoderOptions)
    {
        AvifDecodeResult decoded = DecodePlanarFile(path, decoderOptions);
        return EncodeDecoded(decoded, encoderOptions);
    }

    /// <summary>Decodes a byte array directly into caller-selected packed pixels.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The packed output, rendering, limit, and cancellation settings.</param>
    /// <returns>An owning packed buffer or a buffer carrying a stable error code.</returns>
    public AvifPixelBuffer DecodePackedBytes(
        byte[]? source,
        AvifPackedDecodeOptions options)
    {
        AvifResultCode optionsCode = ValidatePackedDecodeOptions(options);
        if (optionsCode != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(optionsCode);
        }
        if (source == null)
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.InvalidArgument);
        }
        AvifDecoderOptions decoderOptions = CreatePackedDecoderOptions(
            options);
        AvifDecodeResult decoded = Avif.Decode(source, decoderOptions);
        return ConvertDecodedToPacked(decoded, options);
    }

    /// <summary>Decodes a byte span directly into caller-selected packed pixels.</summary>
    /// <param name="source">The complete AVIF source bytes.</param>
    /// <param name="options">The packed output, rendering, limit, and cancellation settings.</param>
    /// <returns>An owning packed buffer or a buffer carrying a stable error code.</returns>
    public AvifPixelBuffer DecodePackedSpan(
        ReadOnlySpan<byte> source,
        AvifPackedDecodeOptions options)
    {
        AvifResultCode optionsCode = ValidatePackedDecodeOptions(options);
        if (optionsCode != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(optionsCode);
        }
        AvifDecoderOptions decoderOptions = CreatePackedDecoderOptions(
            options);
        AvifDecodeResult decoded = Avif.Decode(source, decoderOptions);
        return ConvertDecodedToPacked(decoded, options);
    }

    /// <summary>Decodes a stream directly into packed pixels without closing it.</summary>
    /// <param name="source">The complete readable source stream.</param>
    /// <param name="options">The packed output, rendering, limit, and cancellation settings.</param>
    /// <returns>An owning packed buffer or a buffer carrying a stable error code.</returns>
    public AvifPixelBuffer DecodePackedStream(
        Stream? source,
        AvifPackedDecodeOptions options)
    {
        AvifResultCode optionsCode = ValidatePackedDecodeOptions(options);
        if (optionsCode != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(optionsCode);
        }
        if (source == null)
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.InvalidArgument);
        }
        AvifDecoderOptions decoderOptions = CreatePackedDecoderOptions(
            options);
        AvifDecodeResult decoded = Avif.Decode(source, decoderOptions);
        return ConvertDecodedToPacked(decoded, options);
    }

    /// <summary>Decodes a file directly into packed pixels and closes its handle before returning.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">The packed output, rendering, limit, and cancellation settings.</param>
    /// <returns>An owning packed buffer or a buffer carrying a stable error code.</returns>
    public AvifPixelBuffer DecodePackedFile(
        string? path,
        AvifPackedDecodeOptions options)
    {
        AvifResultCode optionsCode = ValidatePackedDecodeOptions(options);
        if (optionsCode != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(optionsCode);
        }
        if (string.IsNullOrEmpty(path))
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.InvalidArgument);
        }
        if (!File.Exists(path))
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.IoError);
        }
        using (FileStream source = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read))
        {
            return DecodePackedStream(source, options);
        }
    }

    /// <summary>Converts packed pixels to planar samples and encodes AVIF bytes.</summary>
    /// <param name="pixels">The source packed pixels.</param>
    /// <param name="options">The color, alpha, encoder, limit, and cancellation settings.</param>
    /// <returns>The independently owned encoded AVIF result.</returns>
    public AvifEncodeResult EncodePacked(
        AvifPixelBuffer pixels,
        AvifPackedEncodeOptions options)
    {
        AvifResultCode validation = ValidatePackedEncodeOptions(
            pixels,
            options);
        if (validation != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                validation,
                System.Array.Empty<byte>());
        }
        AvifImage image = AvifImage.Create(
            pixels.GetWidth(),
            pixels.GetHeight(),
            options.BitDepth,
            options.ChromaFormat,
            options.ColorInfo.Range,
            options.ChromaSamplePosition,
            options.AlphaMode,
            1,
            options.Limits);
        AvifResultCode code = image.GetCode();
        if (code == AvifResultCode.Success)
        {
            code = image.SetColorInfo(options.ColorInfo);
        }
        if (code == AvifResultCode.Success)
        {
            AvifColorConverter converter = new AvifColorConverter(
                options.ColorConversion);
            code = converter.ConvertToImage(pixels, image);
        }
        if (code == AvifResultCode.Success
            && options.Cancellation.IsCancellationRequested())
        {
            code = AvifResultCode.Cancelled;
        }
        if (code != AvifResultCode.Success)
        {
            image.Close();
            return new AvifEncodeResult(code, System.Array.Empty<byte>());
        }
        AvifEncoderOptions encoderOptions = CopyPackedEncoderOptions(
            options.EncoderOptions,
            options.Limits);
        AvifEncodeResult encoded = Avif.Encode(image, encoderOptions);
        image.Close();
        if (encoded.GetCode() == AvifResultCode.Success
            && options.Cancellation.IsCancellationRequested())
        {
            return new AvifEncodeResult(
                AvifResultCode.Cancelled,
                System.Array.Empty<byte>());
        }
        return encoded;
    }

    /// <summary>Encodes packed pixels to a stream without closing it.</summary>
    /// <param name="destination">The writable destination stream.</param>
    /// <param name="pixels">The source packed pixels.</param>
    /// <param name="options">The color, alpha, encoder, limit, and cancellation settings.</param>
    /// <returns>The conversion, encode, or write outcome.</returns>
    public AvifResultCode WritePackedStream(
        Stream? destination,
        AvifPixelBuffer pixels,
        AvifPackedEncodeOptions options)
    {
        if (destination == null || !destination.CanWrite)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifEncodeResult encoded = EncodePacked(pixels, options);
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

    /// <summary>Encodes packed pixels to a file and closes its handle before returning.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="pixels">The source packed pixels.</param>
    /// <param name="options">The color, alpha, encoder, limit, and cancellation settings.</param>
    /// <returns>The conversion, encode, or write outcome.</returns>
    public AvifResultCode WritePackedFile(
        string? path,
        AvifPixelBuffer pixels,
        AvifPackedEncodeOptions options)
    {
        if (string.IsNullOrEmpty(path))
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifEncodeResult encoded = EncodePacked(pixels, options);
        if (encoded.GetCode() != AvifResultCode.Success)
        {
            return encoded.GetCode();
        }
        using (FileStream destination = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None))
        {
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

    /// <summary>Creates a stateful reader over a caller-owned seekable stream.</summary>
    /// <param name="source">The readable seekable stream at its desired initial position.</param>
    /// <param name="options">The decoder options to snapshot.</param>
    /// <returns>The initialized reader or a reader carrying an argument error.</returns>
    public AvifReader CreateSeekableStreamReader(
        Stream? source,
        AvifDecoderOptions options)
    {
        if (source == null || !source.CanRead || !source.CanSeek)
        {
            return new AvifReader(null, options);
        }
        return AvifReader.FromStream(source, options);
    }

    /// <summary>Creates a stateful reader over a caller-owned non-seekable stream.</summary>
    /// <param name="source">The readable non-seekable stream.</param>
    /// <param name="options">The decoder options to snapshot.</param>
    /// <returns>The initialized reader or a reader carrying an argument error.</returns>
    public AvifReader CreateNonSeekableStreamReader(
        Stream? source,
        AvifDecoderOptions options)
    {
        if (source == null || !source.CanRead || source.CanSeek)
        {
            return new AvifReader(null, options);
        }
        return AvifReader.FromStream(source, options);
    }

    /// <summary>Creates a stateful reader over a caller-owned exact-range source.</summary>
    /// <param name="source">The random-access source.</param>
    /// <param name="options">The decoder options to snapshot.</param>
    /// <returns>The initialized range reader or a reader carrying an argument error.</returns>
    public AvifReader CreateRangeReader(
        IAvifRandomAccessSource? source,
        AvifDecoderOptions options)
    {
        return AvifReader.FromRandomAccessSource(source, options);
    }

    internal AvifPixelBuffer ConvertDecodedToPacked(
        AvifDecodeResult decoded,
        AvifPackedDecodeOptions options)
    {
        if (decoded.GetCode() != AvifResultCode.Success)
        {
            return AvifPixelBuffer.FromCode(decoded.GetCode());
        }
        AvifImage? image = decoded.GetImage();
        if (image == null)
        {
            return AvifPixelBuffer.FromCode(AvifResultCode.InvalidState);
        }
        AvifPixelBuffer packed = AvifPixelBuffer.Create(
            image.GetWidth(),
            image.GetHeight(),
            options.PixelFormat,
            options.BitDepth,
            options.FloatingPoint,
            1,
            options.Limits);
        AvifResultCode code = packed.GetCode();
        if (code == AvifResultCode.Success)
        {
            AvifRenderOptions renderOptions = CopyPackedRenderOptions(
                options.RenderOptions,
                options.Limits);
            AvifColorConverter converter = new AvifColorConverter(
                renderOptions.ColorConversion);
            code = converter.ConvertToPixelBuffer(image, packed);
            if (code == AvifResultCode.Success)
            {
                AvifDocument? document = decoded.GetDocument();
                AvifImageSource? selected = document == null
                    ? null : document.SelectSource(
                        AvifSourceSelection.Automatic);
                AvifImageItem? item = selected == null
                    ? null : selected.GetPrimaryItem();
                if (item != null)
                {
                    AvifPixelBuffer transformed = item.GetTransform().Apply(
                        packed,
                        renderOptions);
                    packed.Close();
                    packed = transformed;
                    code = packed.GetCode();
                }
            }
        }
        image.Close();
        if (code == AvifResultCode.Success
            && options.Cancellation.IsCancellationRequested())
        {
            packed.Close();
            return AvifPixelBuffer.FromCode(AvifResultCode.Cancelled);
        }
        if (code != AvifResultCode.Success)
        {
            packed.Close();
            return AvifPixelBuffer.FromCode(code);
        }
        return packed;
    }

    internal AvifDecoderOptions CreatePackedDecoderOptions(
        AvifPackedDecodeOptions options)
    {
        AvifDecoderOptions decoderOptions = new AvifDecoderOptions();
        decoderOptions.RenderOptions = CopyPackedRenderOptions(
            options.RenderOptions,
            options.Limits);
        decoderOptions.Limits = options.Limits;
        decoderOptions.Cancellation = options.Cancellation;
        return decoderOptions;
    }

    internal AvifRenderOptions CopyPackedRenderOptions(
        AvifRenderOptions source,
        AvifSecurityLimits limits)
    {
        AvifRenderOptions copy = new AvifRenderOptions();
        copy.ApplyCleanAperture = source.ApplyCleanAperture;
        copy.ApplyRotation = source.ApplyRotation;
        copy.ApplyMirror = source.ApplyMirror;
        copy.ApplyExifOrientation = source.ApplyExifOrientation;
        copy.ToneMapMode = source.ToneMapMode;
        copy.DisplayHeadroom = source.DisplayHeadroom;
        copy.PreserveAlpha = source.PreserveAlpha;
        copy.ColorConversion = CopyPackedColorConversion(
            source.ColorConversion,
            source.PreserveAlpha);
        copy.Limits = limits;
        return copy;
    }

    internal AvifColorConversionOptions CopyPackedColorConversion(
        AvifColorConversionOptions source,
        bool preserveAlpha)
    {
        AvifColorConversionOptions copy =
            new AvifColorConversionOptions();
        copy.ResamplingFilter = source.ResamplingFilter;
        copy.DitherMode = source.DitherMode;
        copy.UseEmbeddedIccProfile = source.UseEmbeddedIccProfile;
        copy.ClampOutOfGamut = source.ClampOutOfGamut;
        copy.PreserveAlpha = source.PreserveAlpha && preserveAlpha;
        copy.OutputPremultipliedAlpha =
            source.OutputPremultipliedAlpha;
        copy.WorkerCount = source.WorkerCount;
        return copy;
    }

    internal AvifEncoderOptions CopyPackedEncoderOptions(
        AvifEncoderOptions source,
        AvifSecurityLimits limits)
    {
        AvifEncoderOptions copy = new AvifEncoderOptions();
        copy.Mode = source.Mode;
        copy.Preset = source.Preset;
        copy.HeaderFormat = source.HeaderFormat;
        copy.Quality = source.Quality;
        copy.AlphaQuality = source.AlphaQuality;
        copy.GainMapQuality = source.GainMapQuality;
        copy.AuxiliaryQuality = source.AuxiliaryQuality;
        copy.GridCellQuality = source.GridCellQuality;
        copy.LayerQuality = source.LayerQuality;
        copy.Quantizer = source.Quantizer;
        copy.TargetSizeBytes = source.TargetSizeBytes;
        copy.TargetBitrate = source.TargetBitrate;
        copy.KeyframeInterval = source.KeyframeInterval;
        copy.SequencePolicy = source.SequencePolicy;
        copy.FrameLag = source.FrameLag;
        copy.EnableShowExistingFrames = source.EnableShowExistingFrames;
        copy.EnableGlobalMotion = source.EnableGlobalMotion;
        copy.EnableWarpedMotion = source.EnableWarpedMotion;
        copy.FilmGrainParametersPresent = source.FilmGrainParametersPresent;
        copy.TemporalLayerCount = source.TemporalLayerCount;
        copy.SpatialLayers = CopyPackedSpatialLayers(source.SpatialLayers);
        copy.OperatingPoints = CopyPackedOperatingPoints(source.OperatingPoints);
        copy.ProgressiveOutput = source.ProgressiveOutput;
        copy.AutoTiling = source.AutoTiling;
        copy.TileRowsLog2 = source.TileRowsLog2;
        copy.TileColumnsLog2 = source.TileColumnsLog2;
        copy.WorkerCount = source.WorkerCount;
        copy.Deterministic = source.Deterministic;
        copy.ValidateOutput = source.ValidateOutput;
        copy.Limits = limits;
        return copy;
    }

    internal Av1SpatialLayerOptions[] CopyPackedSpatialLayers(
        Av1SpatialLayerOptions[] source)
    {
        if (source == null)
        {
            return System.Array.Empty<Av1SpatialLayerOptions>();
        }
        Av1SpatialLayerOptions[] result =
            new Av1SpatialLayerOptions[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Av1SpatialLayerOptions copy = new Av1SpatialLayerOptions();
            Av1SpatialLayerOptions value = source[index];
            if (value != null)
            {
                copy.Width = value.Width;
                copy.Height = value.Height;
                copy.Quality = value.Quality;
            }
            result[index] = copy;
            index++;
        }
        return result;
    }

    internal Av1OperatingPointOptions[] CopyPackedOperatingPoints(
        Av1OperatingPointOptions[] source)
    {
        if (source == null)
        {
            return System.Array.Empty<Av1OperatingPointOptions>();
        }
        Av1OperatingPointOptions[] result =
            new Av1OperatingPointOptions[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            Av1OperatingPointOptions copy = new Av1OperatingPointOptions();
            Av1OperatingPointOptions value = source[index];
            if (value != null)
            {
                copy.Idc = value.Idc;
                copy.Level = value.Level;
                copy.Tier = value.Tier;
            }
            result[index] = copy;
            index++;
        }
        return result;
    }

    internal AvifResultCode ValidatePackedDecodeOptions(
        AvifPackedDecodeOptions? options)
    {
        if (options == null
            || options.RenderOptions == null
            || options.RenderOptions.ColorConversion == null
            || options.Limits == null
            || options.Cancellation == null
            || ResourceGuard.ValidateLimits(options.Limits)
                != AvifResultCode.Success
            || options.RenderOptions.ColorConversion.WorkerCount < 1
            || options.RenderOptions.ColorConversion.WorkerCount
                > options.Limits.MaxWorkerCount
            || options.RenderOptions.ToneMapMode < AvifToneMapMode.Automatic
            || options.RenderOptions.ToneMapMode > AvifToneMapMode.ApplyGainMap
            || !float.IsFinite(options.RenderOptions.DisplayHeadroom)
            || options.RenderOptions.DisplayHeadroom < 1)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifPixelBuffer formatProbe = AvifPixelBuffer.Create(
            1,
            1,
            options.PixelFormat,
            options.BitDepth,
            options.FloatingPoint,
            1,
            options.Limits);
        AvifResultCode code = formatProbe.GetCode();
        formatProbe.Close();
        if (code != AvifResultCode.Success)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (options.Cancellation.IsCancellationRequested())
        {
            return AvifResultCode.Cancelled;
        }
        return AvifResultCode.Success;
    }

    internal AvifResultCode ValidatePackedEncodeOptions(
        AvifPixelBuffer? pixels,
        AvifPackedEncodeOptions? options)
    {
        if (pixels == null || options == null
            || options.ColorInfo == null
            || options.ColorConversion == null
            || options.EncoderOptions == null
            || options.Limits == null
            || options.Cancellation == null
            || ResourceGuard.ValidateLimits(options.Limits)
                != AvifResultCode.Success
            || options.BitDepth != 8
                && options.BitDepth != 10
                && options.BitDepth != 12
            || options.ChromaFormat < AvifChromaFormat.Monochrome
            || options.ChromaFormat > AvifChromaFormat.Yuv444
            || options.ChromaSamplePosition
                < AvifChromaSamplePosition.Unknown
            || options.ChromaSamplePosition
                > AvifChromaSamplePosition.Colocated
            || options.AlphaMode < AvifAlphaMode.None
            || options.AlphaMode > AvifAlphaMode.Premultiplied
            || options.ColorConversion.WorkerCount < 1
            || options.ColorConversion.WorkerCount
                > options.Limits.MaxWorkerCount
            || options.EncoderOptions.WorkerCount < 1
            || options.EncoderOptions.WorkerCount
                > options.Limits.MaxWorkerCount)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode code = pixels.GetCode();
        if (code != AvifResultCode.Success)
        {
            return code;
        }
        if (pixels.GetWidth() > options.Limits.MaxWidth
            || pixels.GetHeight() > options.Limits.MaxHeight
            || (long)pixels.GetWidth() * pixels.GetHeight()
                > options.Limits.MaxPixels)
        {
            return AvifResultCode.LimitExceeded;
        }
        if (options.Cancellation.IsCancellationRequested())
        {
            return AvifResultCode.Cancelled;
        }
        return AvifResultCode.Success;
    }

    internal AvifEncodeResult EncodeDecoded(
        AvifDecodeResult decoded,
        AvifEncoderOptions options)
    {
        if (decoded.GetCode() != AvifResultCode.Success)
        {
            return new AvifEncodeResult(
                decoded.GetCode(),
                System.Array.Empty<byte>());
        }
        AvifImage? image = decoded.GetImage();
        if (image == null)
        {
            return new AvifEncodeResult(
                AvifResultCode.InvalidState,
                System.Array.Empty<byte>());
        }
        AvifEncodeResult encoded = Avif.Encode(image, options);
        image.Close();
        return encoded;
    }
}
