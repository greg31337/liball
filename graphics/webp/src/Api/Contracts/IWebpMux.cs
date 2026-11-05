using System;
using System.Collections.Generic;
using System.IO;

namespace webp;

/// <summary>Reads and edits WebP metadata and animation container fields from memory, streams, or file paths without decoding pixels.</summary>
public interface IWebpMux
{
    /// <summary>Reads raw ICC, Exif, and XMP payloads from a WebP container.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Independent copies of the metadata payloads.</returns>
    WebpMetadata GetMetadata(byte[] data);

    /// <summary>Reads raw ICC, Exif, and XMP payloads from a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Independent copies of the metadata payloads.</returns>
    WebpMetadata GetMetadataSpan(ReadOnlySpan<byte> data);

    /// <summary>Reads metadata from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>Independent copies of the metadata payloads.</returns>
    WebpMetadata GetMetadataStream(Stream stream);

    /// <summary>Reads metadata from a WebP file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>Independent copies of the metadata payloads.</returns>
    WebpMetadata GetMetadataFile(string path);

    /// <summary>Replaces all supported metadata payloads.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="metadata">The replacement metadata.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] ReplaceMetadata(byte[] data, WebpMetadata metadata);

    /// <summary>Replaces all supported metadata payloads in a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="metadata">The replacement metadata.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] ReplaceMetadataSpan(ReadOnlySpan<byte> data, WebpMetadata metadata);

    /// <summary>Replaces metadata in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="metadata">The replacement metadata.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] ReplaceMetadataStream(Stream stream, WebpMetadata metadata);

    /// <summary>Replaces metadata in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="metadata">The replacement metadata.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] ReplaceMetadataFile(string path, WebpMetadata metadata);

    /// <summary>Removes all supported metadata payloads.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A newly allocated WebP file without ICC, Exif, or XMP payloads.</returns>
    byte[] StripMetadata(byte[] data);

    /// <summary>Removes all supported metadata payloads from a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A newly allocated WebP file without ICC, Exif, or XMP payloads.</returns>
    byte[] StripMetadataSpan(ReadOnlySpan<byte> data);

    /// <summary>Strips metadata from WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>A newly allocated WebP file without ICC, Exif, or XMP payloads.</returns>
    byte[] StripMetadataStream(Stream stream);

    /// <summary>Strips metadata from WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>A newly allocated WebP file without ICC, Exif, or XMP payloads.</returns>
    byte[] StripMetadataFile(string path);

    /// <summary>Replaces or removes the ICC color profile.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="iccProfile">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetIccProfile(byte[] data, byte[]? iccProfile);

    /// <summary>Replaces or removes the ICC color profile in a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="iccProfile">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetIccProfileSpan(ReadOnlySpan<byte> data, byte[]? iccProfile);

    /// <summary>Sets the ICC profile in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="iccProfile">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetIccProfileStream(Stream stream, byte[]? iccProfile);

    /// <summary>Sets the ICC profile in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="iccProfile">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetIccProfileFile(string path, byte[]? iccProfile);

    /// <summary>Replaces or removes the Exif payload.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="exif">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetExif(byte[] data, byte[]? exif);

    /// <summary>Replaces or removes the Exif payload in a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="exif">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetExifSpan(ReadOnlySpan<byte> data, byte[]? exif);

    /// <summary>Sets Exif data in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="exif">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetExifStream(Stream stream, byte[]? exif);

    /// <summary>Sets Exif data in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="exif">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetExifFile(string path, byte[]? exif);

    /// <summary>Replaces or removes the XMP payload.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="xmp">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetXmp(byte[] data, byte[]? xmp);

    /// <summary>Replaces or removes the XMP payload in a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="xmp">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetXmpSpan(ReadOnlySpan<byte> data, byte[]? xmp);

    /// <summary>Sets XMP data in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="xmp">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetXmpStream(Stream stream, byte[]? xmp);

    /// <summary>Sets XMP data in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="xmp">The replacement payload, or null to remove it.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetXmpFile(string path, byte[]? xmp);

    /// <summary>Enumerates top-level unrecognized chunks in source order.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Independent copies of the unknown chunks.</returns>
    List<WebpUnknownChunk> EnumerateUnknownChunks(byte[] data);

    /// <summary>Enumerates top-level unrecognized chunks from a read-only span in source order.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Independent copies of the unknown chunks.</returns>
    List<WebpUnknownChunk> EnumerateUnknownChunksSpan(ReadOnlySpan<byte> data);

    /// <summary>Enumerates top-level unrecognized chunks from the current position of a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>Independent copies of the unknown chunks.</returns>
    List<WebpUnknownChunk> EnumerateUnknownChunksStream(Stream stream);

    /// <summary>Enumerates top-level unrecognized chunks from a WebP file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>Independent copies of the unknown chunks.</returns>
    List<WebpUnknownChunk> EnumerateUnknownChunksFile(string path);

    /// <summary>Replaces every top-level unknown chunk while preserving replacement order.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="unknownChunks">The replacement unknown chunks in desired output order.</param>
    /// <param name="placement">Where replacements are positioned relative to recognized chunks.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] ReplaceUnknownChunks(byte[] data, List<WebpUnknownChunk> unknownChunks, WebpUnknownChunkPlacement placement);

    /// <summary>Replaces every top-level unknown chunk in a container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <param name="unknownChunks">The replacement unknown chunks in desired output order.</param>
    /// <param name="placement">Where replacements are positioned relative to recognized chunks.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] ReplaceUnknownChunksSpan(ReadOnlySpan<byte> data, List<WebpUnknownChunk> unknownChunks, WebpUnknownChunkPlacement placement);

    /// <summary>Replaces top-level unknown chunks in data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="unknownChunks">The replacement unknown chunks in desired output order.</param>
    /// <param name="placement">Where replacements are positioned relative to recognized chunks.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] ReplaceUnknownChunksStream(Stream stream, List<WebpUnknownChunk> unknownChunks, WebpUnknownChunkPlacement placement);

    /// <summary>Replaces top-level unknown chunks in a WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="unknownChunks">The replacement unknown chunks in desired output order.</param>
    /// <param name="placement">Where replacements are positioned relative to recognized chunks.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] ReplaceUnknownChunksFile(string path, List<WebpUnknownChunk> unknownChunks, WebpUnknownChunkPlacement placement);

    /// <summary>Removes every top-level unknown chunk.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A newly allocated WebP file containing only recognized top-level chunks.</returns>
    byte[] StripUnknownChunks(byte[] data);

    /// <summary>Removes every top-level unknown chunk from a container supplied as a read-only span.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>A newly allocated WebP file containing only recognized top-level chunks.</returns>
    byte[] StripUnknownChunksSpan(ReadOnlySpan<byte> data);

    /// <summary>Removes top-level unknown chunks from data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>A newly allocated WebP file containing only recognized top-level chunks.</returns>
    byte[] StripUnknownChunksStream(Stream stream);

    /// <summary>Removes top-level unknown chunks from a WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>A newly allocated WebP file containing only recognized top-level chunks.</returns>
    byte[] StripUnknownChunksFile(string path);

    /// <summary>Extracts independent copies of every encoded animation frame.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <returns>The encoded frames in display order.</returns>
    List<WebpFrame> ExtractFrames(byte[] data);

    /// <summary>Extracts independent copies of every encoded animation frame from a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <returns>The encoded frames in display order.</returns>
    List<WebpFrame> ExtractFramesSpan(ReadOnlySpan<byte> data);

    /// <summary>Extracts frames from WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>The encoded frames in display order.</returns>
    List<WebpFrame> ExtractFramesStream(Stream stream);

    /// <summary>Extracts frames from animated WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>The encoded frames in display order.</returns>
    List<WebpFrame> ExtractFramesFile(string path);

    /// <summary>Extracts one encoded animation frame.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>An independent copy of the selected frame.</returns>
    WebpFrame ExtractFrame(byte[] data, int frameIndex);

    /// <summary>Extracts one encoded animation frame from a WebP container supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>An independent copy of the selected frame.</returns>
    WebpFrame ExtractFrameSpan(ReadOnlySpan<byte> data, int frameIndex);

    /// <summary>Extracts one frame from WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>An independent copy of the selected frame.</returns>
    WebpFrame ExtractFrameStream(Stream stream, int frameIndex);

    /// <summary>Extracts one frame from animated WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="frameIndex">The zero-based frame index.</param>
    /// <returns>An independent copy of the selected frame.</returns>
    WebpFrame ExtractFrameFile(string path, int frameIndex);

    /// <summary>Inserts an encoded frame before a playback position without decoding image pixels.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The insertion index from zero through the current frame count.</param>
    /// <param name="frame">The encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] InsertAnimationFrame(byte[] data, int frameIndex, WebpFrame frame);

    /// <summary>Inserts an encoded frame into an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The insertion index from zero through the current frame count.</param>
    /// <param name="frame">The encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] InsertAnimationFrameSpan(ReadOnlySpan<byte> data, int frameIndex, WebpFrame frame);

    /// <summary>Inserts an encoded frame into animation data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="frameIndex">The insertion index from zero through the current frame count.</param>
    /// <param name="frame">The encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] InsertAnimationFrameStream(Stream stream, int frameIndex, WebpFrame frame);

    /// <summary>Inserts an encoded frame into an animated WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="frameIndex">The insertion index from zero through the current frame count.</param>
    /// <param name="frame">The encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] InsertAnimationFrameFile(string path, int frameIndex, WebpFrame frame);

    /// <summary>Replaces one encoded animation frame without decoding image pixels.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame to replace.</param>
    /// <param name="frame">The replacement encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] ReplaceAnimationFrame(byte[] data, int frameIndex, WebpFrame frame);

    /// <summary>Replaces one encoded frame in an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame to replace.</param>
    /// <param name="frame">The replacement encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] ReplaceAnimationFrameSpan(ReadOnlySpan<byte> data, int frameIndex, WebpFrame frame);

    /// <summary>Replaces one encoded frame in animation data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="frameIndex">The zero-based frame to replace.</param>
    /// <param name="frame">The replacement encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] ReplaceAnimationFrameStream(Stream stream, int frameIndex, WebpFrame frame);

    /// <summary>Replaces one encoded frame in an animated WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="frameIndex">The zero-based frame to replace.</param>
    /// <param name="frame">The replacement encoded frame and its animation settings.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] ReplaceAnimationFrameFile(string path, int frameIndex, WebpFrame frame);

    /// <summary>Removes one encoded animation frame without decoding any remaining frame.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame to remove.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] RemoveAnimationFrame(byte[] data, int frameIndex);

    /// <summary>Removes one frame from an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="frameIndex">The zero-based frame to remove.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] RemoveAnimationFrameSpan(ReadOnlySpan<byte> data, int frameIndex);

    /// <summary>Removes one frame from animation data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="frameIndex">The zero-based frame to remove.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] RemoveAnimationFrameStream(Stream stream, int frameIndex);

    /// <summary>Removes one frame from an animated WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="frameIndex">The zero-based frame to remove.</param>
    /// <returns>A newly allocated animated WebP file.</returns>
    byte[] RemoveAnimationFrameFile(string path, int frameIndex);

    /// <summary>Normalizes a WebP container without recompressing valid image bitstreams.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Canonical bytes when safe, or the original bytes and diagnostics when repair is ambiguous.</returns>
    WebpNormalizationResult NormalizeContainer(byte[] data);

    /// <summary>Normalizes a WebP container supplied as a read-only span without recompressing image data.</summary>
    /// <param name="data">The complete RIFF WebP file.</param>
    /// <returns>Canonical bytes when safe, or the original bytes and diagnostics when repair is ambiguous.</returns>
    WebpNormalizationResult NormalizeContainerSpan(ReadOnlySpan<byte> data);

    /// <summary>Normalizes WebP data read from a caller-owned stream.</summary>
    /// <remarks>The stream is advanced to its end and remains open.</remarks>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <returns>Canonical bytes when safe, or the original bytes and diagnostics when repair is ambiguous.</returns>
    WebpNormalizationResult NormalizeContainerStream(Stream stream);

    /// <summary>Normalizes a WebP file read from a path.</summary>
    /// <param name="path">The source file path.</param>
    /// <returns>Canonical bytes when safe, or the original bytes and diagnostics when repair is ambiguous.</returns>
    WebpNormalizationResult NormalizeContainerFile(string path);

    /// <summary>Changes an animation's loop count.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationLoopCount(byte[] data, ushort loopCount);

    /// <summary>Changes the loop count of an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationLoopCountSpan(ReadOnlySpan<byte> data, ushort loopCount);

    /// <summary>Sets the animation loop count in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationLoopCountStream(Stream stream, ushort loopCount);

    /// <summary>Sets the animation loop count in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationLoopCountFile(string path, ushort loopCount);

    /// <summary>Changes an animation's background color.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationBackgroundColor(byte[] data, uint backgroundColorBgra);

    /// <summary>Changes the background color of an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationBackgroundColorSpan(ReadOnlySpan<byte> data, uint backgroundColorBgra);

    /// <summary>Sets the animation background in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationBackgroundColorStream(Stream stream, uint backgroundColorBgra);

    /// <summary>Sets the animation background in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationBackgroundColorFile(string path, uint backgroundColorBgra);

    /// <summary>Changes an animation's loop count and background color.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationLoopAndBackground(byte[] data, ushort loopCount, uint backgroundColorBgra);

    /// <summary>Changes the loop count and background color of an animation supplied as a read-only span.</summary>
    /// <param name="data">The complete animated RIFF WebP file.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationLoopAndBackgroundSpan(ReadOnlySpan<byte> data, ushort loopCount, uint backgroundColorBgra);

    /// <summary>Sets animation globals in WebP data read from the current position of a caller-owned stream, advancing it to the end without closing it.</summary>
    /// <param name="stream">The readable seekable or non-seekable stream.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationLoopAndBackgroundStream(Stream stream, ushort loopCount, uint backgroundColorBgra);

    /// <summary>Sets animation globals in WebP data read from a file path.</summary>
    /// <param name="path">The source file path.</param>
    /// <param name="loopCount">The repetition count, where zero means repeat forever.</param>
    /// <param name="backgroundColorBgra">The background packed as blue, green, red, alpha bytes.</param>
    /// <returns>A newly allocated WebP file.</returns>
    byte[] SetAnimationLoopAndBackgroundFile(string path, ushort loopCount, uint backgroundColorBgra);
}
