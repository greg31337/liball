using System;
using System.Collections.Generic;
using System.IO;

namespace webp;

/// <summary>Provides the injectable instance API for WebP detection, decoding, encoding, inspection, validation, pixel processing, and container editing.</summary>
/// <remarks>The class is stateless and may be shared. Each interface is deliberately small so consumers can depend only on the capability they use.</remarks>
public class WebpCodec : IWebpFormatDetector, IWebpDecoder, IWebpEncoder, IWebpInfoReader, IWebpValidator, IWebpPixelUtilities, IWebpMux
{
    /// <summary>Creates a stateless WebP codec.</summary>
    public WebpCodec()
    {
    }

    /// <summary>Copies straight-alpha RGBA pixels to compact BGRA storage.</summary>
    /// <param name="source">The source RGBA pixel buffer.</param>
    /// <returns>Compact blue, green, red, alpha bytes with no row padding.</returns>
    public byte[] CopyRgbaToBgra(WebpPixelBuffer source)
    {
        return WebpPixelUtilities.CopyRgbaToBgra(source);
    }

    /// <summary>Converts straight-alpha RGBA pixels to premultiplied-alpha RGBA pixels.</summary>
    /// <param name="source">The straight-alpha source pixels.</param>
    /// <returns>An independent premultiplied-alpha buffer with the same dimensions and stride.</returns>
    public WebpPremultipliedPixelBuffer ConvertStraightToPremultipliedAlpha(WebpPixelBuffer source)
    {
        return WebpPixelUtilities.ConvertStraightToPremultipliedAlpha(source);
    }

    /// <summary>Converts premultiplied-alpha RGBA pixels to straight-alpha RGBA pixels.</summary>
    /// <param name="source">The premultiplied-alpha source pixels.</param>
    /// <returns>An independent straight-alpha buffer with the same dimensions and stride.</returns>
    public WebpPixelBuffer ConvertPremultipliedToStraightAlpha(WebpPremultipliedPixelBuffer source)
    {
        return WebpPixelUtilities.ConvertPremultipliedToStraightAlpha(source);
    }

    /// <summary>Checks whether any pixel is not fully opaque.</summary>
    /// <param name="source">The straight-alpha RGBA pixels to inspect.</param>
    /// <returns><c>true</c> when at least one alpha byte is below 255; otherwise <c>false</c>.</returns>
    public bool HasNonOpaqueAlpha(WebpPixelBuffer source)
    {
        return WebpPixelUtilities.HasNonOpaqueAlpha(source);
    }

    /// <summary>Composites straight-alpha RGBA pixels over a solid background.</summary>
    /// <param name="source">The foreground straight-alpha RGBA pixels.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>An independent straight-alpha RGBA buffer containing the composited pixels.</returns>
    public WebpPixelBuffer BlendOverBackground(WebpPixelBuffer source, uint backgroundColorBgra)
    {
        return WebpPixelUtilities.BlendOverBackground(source, backgroundColorBgra);
    }

    /// <summary>Flattens decoded animation rectangles into independent full-canvas frames.</summary>
    /// <param name="image">The decoded image whose frames are in WebP display order.</param>
    /// <returns>Independent full-canvas frames after applying each frame's blend and disposal modes.</returns>
    public List<WebpFrame> FlattenAnimationFrames(WebpImage image)
    {
        return WebpPixelUtilities.FlattenAnimationFrames(image);
    }

    /// <summary>Determines whether a byte array starts with the RIFF WEBP signature.</summary>
    /// <param name="data">The bytes to inspect. Only the twelve-byte RIFF header is required.</param>
    /// <returns>True when the bytes start with <c>RIFF....WEBP</c>; otherwise, false.</returns>
    public bool HasRiffWebpSignature(byte[] data)
    {
        return WebpSignature.HasRiffWebpSignature(data);
    }

    /// <summary>Determines whether a byte span starts with the RIFF WEBP signature.</summary>
    /// <param name="data">The bytes to inspect. Only the twelve-byte RIFF header is required.</param>
    /// <returns>True when the bytes start with <c>RIFF....WEBP</c>; otherwise, false.</returns>
    public bool HasRiffWebpSignatureSpan(ReadOnlySpan<byte> data)
    {
        return WebpSignature.HasRiffWebpSignature(data);
    }

    /// <summary>Determines whether a MIME type is the WebP MIME type.</summary>
    /// <param name="mimeType">The MIME type to inspect.</param>
    /// <returns>True for <c>image/webp</c>, compared without case sensitivity; otherwise, false.</returns>
    public bool IsWebpMimeType(string mimeType)
    {
        return WebpSignature.IsWebpMimeType(mimeType);
    }

    /// <summary>Determines whether a file extension is a WebP extension.</summary>
    /// <param name="extension">The extension to inspect, with or without a leading period.</param>
    /// <returns>True for <c>.webp</c> or <c>webp</c>, compared without case sensitivity; otherwise, false.</returns>
    public bool IsWebpFileExtension(string extension)
    {
        return WebpSignature.IsWebpFileExtension(extension);
    }

    /// <summary>Decodes a complete RIFF WebP file using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    public WebpDecodeResult Decode(byte[] data, WebpDecodeOptions options)
    {
        return WebpDecoder.Decode(data, options);
    }

    /// <summary>Decodes a complete RIFF WebP file from a read-only span using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    public WebpDecodeResult DecodeSpan(ReadOnlySpan<byte> data, WebpDecodeOptions options)
    {
        return WebpDecoder.Decode(data, options);
    }

    /// <summary>Decodes a complete RIFF WebP file from the current position of a readable stream.</summary>
    /// <remarks>The stream may be seekable or non-seekable, is advanced to its end, and remains open.</remarks>
    /// <param name="stream">The caller-owned stream to read.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    public WebpDecodeResult DecodeStream(Stream stream, WebpDecodeOptions options)
    {
        return WebpDecoder.Decode(stream, options);
    }

    /// <summary>Reads and decodes a complete RIFF WebP file from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">The settings that control parsing and decoded output.</param>
    /// <returns>The decoded image and any format diagnostics.</returns>
    public WebpDecodeResult DecodeFile(string path, WebpDecodeOptions options)
    {
        return WebpDecoder.Decode(path, options);
    }

    /// <summary>Encodes one RGBA image using the supplied options.</summary>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    /// <returns>A complete RIFF WebP file.</returns>
    public byte[] Encode(WebpPixelBuffer buffer, WebpEncodeOptions options)
    {
        return WebpEncoder.Encode(buffer, options);
    }

    /// <summary>Encodes a still or animated image model using the supplied options.</summary>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    /// <returns>A complete RIFF WebP file.</returns>
    public byte[] EncodeImage(WebpImage image, WebpEncodeOptions options)
    {
        return WebpEncoder.Encode(image, options);
    }

    /// <summary>Encodes one RGBA image at the current position of a caller-owned stream.</summary>
    /// <remarks>The stream remains open and its position advances by the encoded file length.</remarks>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    public void EncodeStream(WebpPixelBuffer buffer, Stream stream, WebpEncodeOptions options)
    {
        WebpEncoder.Encode(buffer, stream, options);
    }

    /// <summary>Encodes one RGBA image to a file, replacing any existing file contents.</summary>
    /// <param name="buffer">The RGBA pixels to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    public void EncodeFile(WebpPixelBuffer buffer, string path, WebpEncodeOptions options)
    {
        WebpEncoder.Encode(buffer, path, options);
    }

    /// <summary>Encodes a still or animated image model at the current position of a caller-owned stream.</summary>
    /// <remarks>The stream remains open and its position advances by the encoded file length.</remarks>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    public void EncodeImageStream(WebpImage image, Stream stream, WebpEncodeOptions options)
    {
        WebpEncoder.Encode(image, stream, options);
    }

    /// <summary>Encodes a still or animated image model to a file, replacing any existing file contents.</summary>
    /// <param name="image">The image model containing at least one frame.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The compression, alpha, preservation, and safety settings.</param>
    public void EncodeImageFile(WebpImage image, string path, WebpEncodeOptions options)
    {
        WebpEncoder.Encode(image, path, options);
    }

    /// <summary>Identifies a complete RIFF WebP file using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls format diagnostics.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    public WebpInfo Read(byte[] data, WebpDecodeOptions options)
    {
        return WebpInfoReader.Read(data, options);
    }

    /// <summary>Identifies a complete RIFF WebP file from a read-only span using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls format diagnostics.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    public WebpInfo ReadSpan(ReadOnlySpan<byte> data, WebpDecodeOptions options)
    {
        return WebpInfoReader.Read(data, options);
    }

    /// <summary>Identifies a complete RIFF WebP file from the current position of a readable stream.</summary>
    /// <remarks>The stream may be seekable or non-seekable, is advanced to its end, and remains open.</remarks>
    /// <param name="stream">The caller-owned stream to read.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls format diagnostics.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    public WebpInfo ReadStream(Stream stream, WebpDecodeOptions options)
    {
        WebpPublicModelValidator.ValidateInspectionOptions(options);
        return WebpInfoReader.Read(WebpInputReader.ReadStreamWithLimit(stream, WebpSecurityLimitValidator.GetInputByteLimit(options.Limits)), options);
    }

    /// <summary>Reads and identifies a complete RIFF WebP file from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls format diagnostics.</param>
    /// <returns>Container information and any format diagnostics.</returns>
    public WebpInfo ReadFile(string path, WebpDecodeOptions options)
    {
        WebpPublicModelValidator.ValidateInspectionOptions(options);
        return WebpInfoReader.Read(WebpInputReader.ReadFileWithLimit(path, WebpSecurityLimitValidator.GetInputByteLimit(options.Limits)), options);
    }

    /// <summary>Validates a complete RIFF WebP file using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls warnings and errors.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    public WebpValidationResult Validate(byte[] data, WebpDecodeOptions options)
    {
        return WebpValidator.Validate(data, options);
    }

    /// <summary>Validates a complete RIFF WebP file from a read-only span using the supplied options.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls warnings and errors.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    public WebpValidationResult ValidateSpan(ReadOnlySpan<byte> data, WebpDecodeOptions options)
    {
        return WebpValidator.Validate(data, options);
    }

    /// <summary>Validates a complete RIFF WebP file from the current position of a readable stream.</summary>
    /// <remarks>The stream may be seekable or non-seekable, is advanced to its end, and remains open.</remarks>
    /// <param name="stream">The caller-owned stream to read.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls warnings and errors.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    public WebpValidationResult ValidateStream(Stream stream, WebpDecodeOptions options)
    {
        WebpPublicModelValidator.ValidateInspectionOptions(options);
        return WebpValidator.Validate(WebpInputReader.ReadStreamWithLimit(stream, WebpSecurityLimitValidator.GetInputByteLimit(options.Limits)), options);
    }

    /// <summary>Reads and validates a complete RIFF WebP file from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="options">Decode options whose <c>Strictness</c> setting controls warnings and errors.</param>
    /// <returns>A validity flag and all discovered diagnostics.</returns>
    public WebpValidationResult ValidateFile(string path, WebpDecodeOptions options)
    {
        WebpPublicModelValidator.ValidateInspectionOptions(options);
        return WebpValidator.Validate(WebpInputReader.ReadFileWithLimit(path, WebpSecurityLimitValidator.GetInputByteLimit(options.Limits)), options);
    }

    /// <summary>Reads raw ICC, Exif, and XMP payloads from a WebP container.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Independent copies of the metadata payloads.</returns>
    public WebpMetadata GetMetadata(byte[] data)
    {
        return WebpMux.GetMetadata(data);
    }

    /// <summary>Reads raw ICC, Exif, and XMP payloads from a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Independent copies of the metadata payloads.</returns>
    public WebpMetadata GetMetadataSpan(ReadOnlySpan<byte> data)
    {
        return WebpMux.GetMetadataSpan(data);
    }

    /// <summary>Reads metadata from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>Independent copies of the metadata payloads.</returns>
    public WebpMetadata GetMetadataStream(Stream stream)
    {
        return WebpMux.GetMetadataSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream));
    }

    /// <summary>Reads metadata from a WebP file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>Independent copies of the metadata payloads.</returns>
    public WebpMetadata GetMetadataFile(string path)
    {
        return WebpMux.GetMetadataSpan(WebpInputReader.ReadFileWithDefaultLimit(path));
    }

    /// <summary>Replaces all supported metadata payloads.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="metadata">The replacement metadata.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] ReplaceMetadata(byte[] data, WebpMetadata metadata)
    {
        return WebpMux.ReplaceMetadata(data, metadata);
    }

    /// <summary>Replaces all supported metadata payloads in a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="metadata">The replacement metadata.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] ReplaceMetadataSpan(ReadOnlySpan<byte> data, WebpMetadata metadata)
    {
        return WebpMux.ReplaceMetadataSpan(data, metadata);
    }

    /// <summary>Replaces metadata in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="metadata">The replacement metadata.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] ReplaceMetadataStream(Stream stream, WebpMetadata metadata)
    {
        WebpMetadataMux.ValidateReplacementMetadata(metadata);
        return WebpMux.ReplaceMetadataSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), metadata);
    }

    /// <summary>Replaces metadata in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="metadata">The replacement metadata.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] ReplaceMetadataFile(string path, WebpMetadata metadata)
    {
        WebpMetadataMux.ValidateReplacementMetadata(metadata);
        return WebpMux.ReplaceMetadataSpan(WebpInputReader.ReadFileWithDefaultLimit(path), metadata);
    }

    /// <summary>Removes all supported metadata payloads.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A newly allocated WebP file without ICC, Exif, or XMP payloads.</returns>
    public byte[] StripMetadata(byte[] data)
    {
        return WebpMux.StripMetadata(data);
    }

    /// <summary>Removes all supported metadata payloads from a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A newly allocated WebP file without ICC, Exif, or XMP payloads.</returns>
    public byte[] StripMetadataSpan(ReadOnlySpan<byte> data)
    {
        return WebpMux.StripMetadataSpan(data);
    }

    /// <summary>Strips metadata from WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>A newly allocated WebP file without ICC, Exif, or XMP payloads.</returns>
    public byte[] StripMetadataStream(Stream stream)
    {
        return WebpMux.StripMetadataSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream));
    }

    /// <summary>Strips metadata from WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>A newly allocated WebP file without ICC, Exif, or XMP payloads.</returns>
    public byte[] StripMetadataFile(string path)
    {
        return WebpMux.StripMetadataSpan(WebpInputReader.ReadFileWithDefaultLimit(path));
    }

    /// <summary>Replaces or removes the ICC color profile.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="iccProfile">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetIccProfile(byte[] data, byte[]? iccProfile)
    {
        return WebpMux.SetIccProfile(data, iccProfile);
    }

    /// <summary>Replaces or removes the ICC color profile in a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="iccProfile">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetIccProfileSpan(ReadOnlySpan<byte> data, byte[]? iccProfile)
    {
        return WebpMux.SetIccProfileSpan(data, iccProfile);
    }

    /// <summary>Sets the ICC profile in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="iccProfile">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetIccProfileStream(Stream stream, byte[]? iccProfile)
    {
        WebpMetadataMux.ValidateOptionalMetadataPayload(iccProfile, "iccProfile");
        return WebpMux.SetIccProfileSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), iccProfile);
    }

    /// <summary>Sets the ICC profile in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="iccProfile">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetIccProfileFile(string path, byte[]? iccProfile)
    {
        WebpMetadataMux.ValidateOptionalMetadataPayload(iccProfile, "iccProfile");
        return WebpMux.SetIccProfileSpan(WebpInputReader.ReadFileWithDefaultLimit(path), iccProfile);
    }

    /// <summary>Replaces or removes the Exif payload.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="exif">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetExif(byte[] data, byte[]? exif)
    {
        return WebpMux.SetExif(data, exif);
    }

    /// <summary>Replaces or removes the Exif payload in a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="exif">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetExifSpan(ReadOnlySpan<byte> data, byte[]? exif)
    {
        return WebpMux.SetExifSpan(data, exif);
    }

    /// <summary>Sets Exif data in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="exif">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetExifStream(Stream stream, byte[]? exif)
    {
        WebpMetadataMux.ValidateOptionalMetadataPayload(exif, "exif");
        return WebpMux.SetExifSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), exif);
    }

    /// <summary>Sets Exif data in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="exif">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetExifFile(string path, byte[]? exif)
    {
        WebpMetadataMux.ValidateOptionalMetadataPayload(exif, "exif");
        return WebpMux.SetExifSpan(WebpInputReader.ReadFileWithDefaultLimit(path), exif);
    }

    /// <summary>Replaces or removes the XMP payload.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="xmp">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetXmp(byte[] data, byte[]? xmp)
    {
        return WebpMux.SetXmp(data, xmp);
    }

    /// <summary>Replaces or removes the XMP payload in a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="xmp">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetXmpSpan(ReadOnlySpan<byte> data, byte[]? xmp)
    {
        return WebpMux.SetXmpSpan(data, xmp);
    }

    /// <summary>Sets XMP data in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="xmp">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetXmpStream(Stream stream, byte[]? xmp)
    {
        WebpMetadataMux.ValidateOptionalMetadataPayload(xmp, "xmp");
        return WebpMux.SetXmpSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), xmp);
    }

    /// <summary>Sets XMP data in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="xmp">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetXmpFile(string path, byte[]? xmp)
    {
        WebpMetadataMux.ValidateOptionalMetadataPayload(xmp, "xmp");
        return WebpMux.SetXmpSpan(WebpInputReader.ReadFileWithDefaultLimit(path), xmp);
    }

    /// <summary>Enumerates top-level unrecognized chunks in source order.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Independent copies of the unknown chunks.</returns>
    public List<WebpUnknownChunk> EnumerateUnknownChunks(byte[] data)
    {
        return WebpMux.EnumerateUnknownChunks(data);
    }

    /// <summary>Enumerates top-level unrecognized chunks from a read-only span in source order.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Independent copies of the unknown chunks.</returns>
    public List<WebpUnknownChunk> EnumerateUnknownChunksSpan(ReadOnlySpan<byte> data)
    {
        return WebpMux.EnumerateUnknownChunksSpan(data);
    }

    /// <summary>Enumerates top-level unrecognized chunks from the current position of a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>Independent copies of the unknown chunks.</returns>
    public List<WebpUnknownChunk> EnumerateUnknownChunksStream(Stream stream)
    {
        return WebpMux.EnumerateUnknownChunksSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream));
    }

    /// <summary>Enumerates top-level unrecognized chunks from a WebP file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>Independent copies of the unknown chunks.</returns>
    public List<WebpUnknownChunk> EnumerateUnknownChunksFile(string path)
    {
        return WebpMux.EnumerateUnknownChunksSpan(WebpInputReader.ReadFileWithDefaultLimit(path));
    }

    /// <summary>Replaces every top-level unknown chunk while preserving replacement order.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="unknownChunks">The replacement unknown chunks in desired output order.</param>
    /// <param name="placement">Where replacements are positioned relative to recognized chunks.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] ReplaceUnknownChunks(byte[] data, List<WebpUnknownChunk> unknownChunks, WebpUnknownChunkPlacement placement)
    {
        return WebpMux.ReplaceUnknownChunks(data, unknownChunks, placement);
    }

    /// <summary>Replaces every top-level unknown chunk in a container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="unknownChunks">The replacement unknown chunks in desired output order.</param>
    /// <param name="placement">Where replacements are positioned relative to recognized chunks.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] ReplaceUnknownChunksSpan(ReadOnlySpan<byte> data, List<WebpUnknownChunk> unknownChunks, WebpUnknownChunkPlacement placement)
    {
        return WebpMux.ReplaceUnknownChunksSpan(data, unknownChunks, placement);
    }

    /// <summary>Replaces top-level unknown chunks in data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="unknownChunks">The replacement unknown chunks in desired output order.</param>
    /// <param name="placement">Where replacements are positioned relative to recognized chunks.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] ReplaceUnknownChunksStream(Stream stream, List<WebpUnknownChunk> unknownChunks, WebpUnknownChunkPlacement placement)
    {
        WebpUnknownChunkMux.ValidateReplacementUnknownChunks(unknownChunks);
        WebpUnknownChunkMux.ValidatePlacement(placement);
        return WebpMux.ReplaceUnknownChunksSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), unknownChunks, placement);
    }

    /// <summary>Replaces top-level unknown chunks in a WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="unknownChunks">The replacement unknown chunks in desired output order.</param>
    /// <param name="placement">Where replacements are positioned relative to recognized chunks.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] ReplaceUnknownChunksFile(string path, List<WebpUnknownChunk> unknownChunks, WebpUnknownChunkPlacement placement)
    {
        WebpUnknownChunkMux.ValidateReplacementUnknownChunks(unknownChunks);
        WebpUnknownChunkMux.ValidatePlacement(placement);
        return WebpMux.ReplaceUnknownChunksSpan(WebpInputReader.ReadFileWithDefaultLimit(path), unknownChunks, placement);
    }

    /// <summary>Removes every top-level unknown chunk.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A newly allocated WebP file containing only recognized top-level chunks.</returns>
    public byte[] StripUnknownChunks(byte[] data)
    {
        return WebpMux.StripUnknownChunks(data);
    }

    /// <summary>Removes every top-level unknown chunk from a container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A newly allocated WebP file containing only recognized top-level chunks.</returns>
    public byte[] StripUnknownChunksSpan(ReadOnlySpan<byte> data)
    {
        return WebpMux.StripUnknownChunksSpan(data);
    }

    /// <summary>Removes top-level unknown chunks from data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>A newly allocated WebP file containing only recognized top-level chunks.</returns>
    public byte[] StripUnknownChunksStream(Stream stream)
    {
        return WebpMux.StripUnknownChunksSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream));
    }

    /// <summary>Removes top-level unknown chunks from a WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>A newly allocated WebP file containing only recognized top-level chunks.</returns>
    public byte[] StripUnknownChunksFile(string path)
    {
        return WebpMux.StripUnknownChunksSpan(WebpInputReader.ReadFileWithDefaultLimit(path));
    }

    /// <summary>Extracts independent copies of every encoded animation frame.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <returns>The encoded frames in display order.</returns>
    public List<WebpFrame> ExtractFrames(byte[] data)
    {
        return WebpMux.ExtractFrames(data);
    }

    /// <summary>Extracts independent copies of every encoded animation frame from a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <returns>The encoded frames in display order.</returns>
    public List<WebpFrame> ExtractFramesSpan(ReadOnlySpan<byte> data)
    {
        return WebpMux.ExtractFramesSpan(data);
    }

    /// <summary>Extracts frames from WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>The encoded frames in display order.</returns>
    public List<WebpFrame> ExtractFramesStream(Stream stream)
    {
        return WebpMux.ExtractFramesSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream));
    }

    /// <summary>Extracts frames from animated WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>The encoded frames in display order.</returns>
    public List<WebpFrame> ExtractFramesFile(string path)
    {
        return WebpMux.ExtractFramesSpan(WebpInputReader.ReadFileWithDefaultLimit(path));
    }

    /// <summary>Extracts one encoded animation frame.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>An independent copy of the selected frame.</returns>
    public WebpFrame ExtractFrame(byte[] data, int frameIndex)
    {
        return WebpMux.ExtractFrame(data, frameIndex);
    }

    /// <summary>Extracts one encoded animation frame from a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>An independent copy of the selected frame.</returns>
    public WebpFrame ExtractFrameSpan(ReadOnlySpan<byte> data, int frameIndex)
    {
        return WebpMux.ExtractFrameSpan(data, frameIndex);
    }

    /// <summary>Extracts one frame from WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>An independent copy of the selected frame.</returns>
    public WebpFrame ExtractFrameStream(Stream stream, int frameIndex)
    {
        WebpMux.ValidateNonNegativeFrameIndex(frameIndex);
        return WebpMux.ExtractFrameSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), frameIndex);
    }

    /// <summary>Extracts one frame from animated WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>An independent copy of the selected frame.</returns>
    public WebpFrame ExtractFrameFile(string path, int frameIndex)
    {
        WebpMux.ValidateNonNegativeFrameIndex(frameIndex);
        return WebpMux.ExtractFrameSpan(WebpInputReader.ReadFileWithDefaultLimit(path), frameIndex);
    }

    /// <summary>Inserts an encoded frame before a playback position without decoding image pixels.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The insertion index from zero through the current frame count.</param>
    /// <param name="frame">The encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] InsertAnimationFrame(byte[] data, int frameIndex, WebpFrame frame)
    {
        return WebpMux.InsertAnimationFrame(data, frameIndex, frame);
    }

    /// <summary>Inserts an encoded frame into an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The insertion index from zero through the current frame count.</param>
    /// <param name="frame">The encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] InsertAnimationFrameSpan(ReadOnlySpan<byte> data, int frameIndex, WebpFrame frame)
    {
        return WebpMux.InsertAnimationFrameSpan(data, frameIndex, frame);
    }

    /// <summary>Inserts an encoded frame into animation data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="frameIndex">The insertion index from zero through the current frame count.</param>
    /// <param name="frame">The encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] InsertAnimationFrameStream(Stream stream, int frameIndex, WebpFrame frame)
    {
        WebpMux.ValidateNonNegativeFrameIndex(frameIndex);
        ArgumentNullException.ThrowIfNull(frame);
        return WebpMux.InsertAnimationFrameSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), frameIndex, frame);
    }

    /// <summary>Inserts an encoded frame into an animated WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="frameIndex">The insertion index from zero through the current frame count.</param>
    /// <param name="frame">The encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] InsertAnimationFrameFile(string path, int frameIndex, WebpFrame frame)
    {
        WebpMux.ValidateNonNegativeFrameIndex(frameIndex);
        ArgumentNullException.ThrowIfNull(frame);
        return WebpMux.InsertAnimationFrameSpan(WebpInputReader.ReadFileWithDefaultLimit(path), frameIndex, frame);
    }

    /// <summary>Replaces one encoded animation frame without decoding image pixels.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame to replace.</param>
    /// <param name="frame">The replacement encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] ReplaceAnimationFrame(byte[] data, int frameIndex, WebpFrame frame)
    {
        return WebpMux.ReplaceAnimationFrame(data, frameIndex, frame);
    }

    /// <summary>Replaces one encoded frame in an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame to replace.</param>
    /// <param name="frame">The replacement encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] ReplaceAnimationFrameSpan(ReadOnlySpan<byte> data, int frameIndex, WebpFrame frame)
    {
        return WebpMux.ReplaceAnimationFrameSpan(data, frameIndex, frame);
    }

    /// <summary>Replaces one encoded frame in animation data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="frameIndex">The zero-based frame to replace.</param>
    /// <param name="frame">The replacement encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] ReplaceAnimationFrameStream(Stream stream, int frameIndex, WebpFrame frame)
    {
        WebpMux.ValidateNonNegativeFrameIndex(frameIndex);
        ArgumentNullException.ThrowIfNull(frame);
        return WebpMux.ReplaceAnimationFrameSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), frameIndex, frame);
    }

    /// <summary>Replaces one encoded frame in an animated WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="frameIndex">The zero-based frame to replace.</param>
    /// <param name="frame">The replacement encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] ReplaceAnimationFrameFile(string path, int frameIndex, WebpFrame frame)
    {
        WebpMux.ValidateNonNegativeFrameIndex(frameIndex);
        ArgumentNullException.ThrowIfNull(frame);
        return WebpMux.ReplaceAnimationFrameSpan(WebpInputReader.ReadFileWithDefaultLimit(path), frameIndex, frame);
    }

    /// <summary>Removes one encoded animation frame without decoding any remaining frame.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame to remove.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] RemoveAnimationFrame(byte[] data, int frameIndex)
    {
        return WebpMux.RemoveAnimationFrame(data, frameIndex);
    }

    /// <summary>Removes one frame from an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame to remove.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] RemoveAnimationFrameSpan(ReadOnlySpan<byte> data, int frameIndex)
    {
        return WebpMux.RemoveAnimationFrameSpan(data, frameIndex);
    }

    /// <summary>Removes one frame from animation data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="frameIndex">The zero-based frame to remove.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] RemoveAnimationFrameStream(Stream stream, int frameIndex)
    {
        WebpMux.ValidateNonNegativeFrameIndex(frameIndex);
        return WebpMux.RemoveAnimationFrameSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), frameIndex);
    }

    /// <summary>Removes one frame from an animated WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="frameIndex">The zero-based frame to remove.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    public byte[] RemoveAnimationFrameFile(string path, int frameIndex)
    {
        WebpMux.ValidateNonNegativeFrameIndex(frameIndex);
        return WebpMux.RemoveAnimationFrameSpan(WebpInputReader.ReadFileWithDefaultLimit(path), frameIndex);
    }

    /// <summary>Normalizes a WebP container without recompressing valid image bitstreams.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Canonical bytes when safe, or the original bytes and diagnostics when repair is ambiguous.</returns>
    public WebpNormalizationResult NormalizeContainer(byte[] data)
    {
        return WebpContainerNormalizer.Normalize(data);
    }

    /// <summary>Normalizes a WebP container supplied as a read-only span without recompressing image data.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Canonical bytes when safe, or the original bytes and diagnostics when repair is ambiguous.</returns>
    public WebpNormalizationResult NormalizeContainerSpan(ReadOnlySpan<byte> data)
    {
        return WebpContainerNormalizer.NormalizeSpan(data);
    }

    /// <summary>Normalizes WebP data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>Canonical bytes when safe, or the original bytes and diagnostics when repair is ambiguous.</returns>
    public WebpNormalizationResult NormalizeContainerStream(Stream stream)
    {
        return WebpContainerNormalizer.NormalizeSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream));
    }

    /// <summary>Normalizes a WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>Canonical bytes when safe, or the original bytes and diagnostics when repair is ambiguous.</returns>
    public WebpNormalizationResult NormalizeContainerFile(string path)
    {
        return WebpContainerNormalizer.NormalizeSpan(WebpInputReader.ReadFileWithDefaultLimit(path));
    }

    /// <summary>Changes an animation's loop count.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationLoopCount(byte[] data, ushort loopCount)
    {
        return WebpMux.SetAnimationLoopCount(data, loopCount);
    }

    /// <summary>Changes the loop count of an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationLoopCountSpan(ReadOnlySpan<byte> data, ushort loopCount)
    {
        return WebpMux.SetAnimationLoopCountSpan(data, loopCount);
    }

    /// <summary>Sets the animation loop count in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationLoopCountStream(Stream stream, ushort loopCount)
    {
        return WebpMux.SetAnimationLoopCountSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), loopCount);
    }

    /// <summary>Sets the animation loop count in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationLoopCountFile(string path, ushort loopCount)
    {
        return WebpMux.SetAnimationLoopCountSpan(WebpInputReader.ReadFileWithDefaultLimit(path), loopCount);
    }

    /// <summary>Changes an animation's background color.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationBackgroundColor(byte[] data, uint backgroundColorBgra)
    {
        return WebpMux.SetAnimationBackgroundColor(data, backgroundColorBgra);
    }

    /// <summary>Changes the background color of an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationBackgroundColorSpan(ReadOnlySpan<byte> data, uint backgroundColorBgra)
    {
        return WebpMux.SetAnimationBackgroundColorSpan(data, backgroundColorBgra);
    }

    /// <summary>Sets the animation background in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationBackgroundColorStream(Stream stream, uint backgroundColorBgra)
    {
        return WebpMux.SetAnimationBackgroundColorSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), backgroundColorBgra);
    }

    /// <summary>Sets the animation background in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationBackgroundColorFile(string path, uint backgroundColorBgra)
    {
        return WebpMux.SetAnimationBackgroundColorSpan(WebpInputReader.ReadFileWithDefaultLimit(path), backgroundColorBgra);
    }

    /// <summary>Changes an animation's loop count and background color.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationLoopAndBackground(byte[] data, ushort loopCount, uint backgroundColorBgra)
    {
        return WebpMux.SetAnimationLoopAndBackground(data, loopCount, backgroundColorBgra);
    }

    /// <summary>Changes the loop count and background color of an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationLoopAndBackgroundSpan(ReadOnlySpan<byte> data, ushort loopCount, uint backgroundColorBgra)
    {
        return WebpMux.SetAnimationLoopAndBackgroundSpan(data, loopCount, backgroundColorBgra);
    }

    /// <summary>Sets animation globals in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationLoopAndBackgroundStream(Stream stream, ushort loopCount, uint backgroundColorBgra)
    {
        return WebpMux.SetAnimationLoopAndBackgroundSpan(WebpInputReader.ReadStreamWithDefaultLimit(stream), loopCount, backgroundColorBgra);
    }

    /// <summary>Sets animation globals in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    public byte[] SetAnimationLoopAndBackgroundFile(string path, ushort loopCount, uint backgroundColorBgra)
    {
        return WebpMux.SetAnimationLoopAndBackgroundSpan(WebpInputReader.ReadFileWithDefaultLimit(path), loopCount, backgroundColorBgra);
    }
}
